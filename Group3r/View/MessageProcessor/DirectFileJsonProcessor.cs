using Group3r.Options;
using LibSnaffle.Concurrency;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Group3r.View
{
    /**
     * Summary: Direct file writer for JSON output that doesn't block on message processing
     */
    public class DirectFileJsonProcessor : IMessageProcessor
    {
        private readonly string _filePath;
        private readonly JsonSerializerSettings _jSettings;
        private readonly object _fileLock = new object();
        private bool _hasStarted = false;
        private int _messageCount = 0;

        public DirectFileJsonProcessor(string filePath)
        {
            _filePath = filePath;
            _jSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>() { new StringEnumConverter() }
            };
            
            // Initialize the file with opening bracket
            lock (_fileLock)
            {
                File.WriteAllText(_filePath, "[" + Environment.NewLine);
            }
        }

        public bool ProcessMessage(QueueMessage message, GrouperOptions options)
        {
            // Process the message but don't block
            ThreadPool.QueueUserWorkItem(_ => ProcessMessageAsync(message, options));
            
            // Check if it's a finish message
            return message is FinishMessage || message is FatalMessage;
        }

        private void ProcessMessageAsync(QueueMessage message, GrouperOptions options)
        {
            try
            {
                object messageObject = null;

                if (message is GpoResultMessage gpoResultMessage)
                {
                    string gpoJson = options.Printer.OutputGpoResult(gpoResultMessage.GpoResult);
                    messageObject = JsonConvert.DeserializeObject(gpoJson);
                }
                else if (message is FileResultMessage fileResultMessage)
                {
                    messageObject = new
                    {
                        Timestamp = message.MsgDateTime,
                        Type = "FileResult",
                        Message = message.GetMessage(),
                        FileResult = fileResultMessage.Result
                    };
                }
                else if (message is InfoMessage || message is ErrorMessage)
                {
                    messageObject = new
                    {
                        Timestamp = message.MsgDateTime,
                        Type = message.GetType().Name.Replace("Message", ""),
                        Message = message.GetMessage()
                    };
                }
                else if (message is FatalMessage)
                {
                    messageObject = new
                    {
                        Timestamp = message.MsgDateTime,
                        Type = "Fatal",
                        Message = message.GetMessage()
                    };
                }
                else if (message is FinishMessage)
                {
                    // Close the JSON array when we get the finish message
                    lock (_fileLock)
                    {
                        File.AppendAllText(_filePath, Environment.NewLine + "]");
                    }
                    return;
                }

                if (messageObject != null)
                {
                    lock (_fileLock)
                    {
                        // Add comma if not first message
                        if (Interlocked.Increment(ref _messageCount) > 1)
                        {
                            File.AppendAllText(_filePath, "," + Environment.NewLine);
                        }
                        
                        // Write the JSON object
                        string json = JsonConvert.SerializeObject(messageObject, _jSettings);
                        File.AppendAllText(_filePath, json);
                        
                        // Flush to ensure data is written
                        using (var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            fs.Flush(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.Error.WriteLine($"Error writing to JSON file: {ex.Message}");
            }
        }

        public void Finalize()
        {
            // Ensure the JSON array is properly closed even if no FinishMessage was received
            try
            {
                lock (_fileLock)
                {
                    var content = File.ReadAllText(_filePath);
                    if (!content.TrimEnd().EndsWith("]"))
                    {
                        File.AppendAllText(_filePath, Environment.NewLine + "]");
                    }
                }
            }
            catch { }
        }
    }
} 