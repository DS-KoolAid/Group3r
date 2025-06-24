using Group3r.Options;
using LibSnaffle.Concurrency;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;

namespace Group3r.View
{
    /**
     * Summary: Implementation of IMessageProcessor which outputs messages as JSON.
     */
    class JsonMessageProcessor : IMessageProcessor
    {
        private JsonSerializerSettings jSettings;
        private List<object> messages;
        private bool hasStarted;

        /**
         * Summary: constructor
         * Arguments: none
         * Returns: JsonMessageProcessor instance
         */
        public JsonMessageProcessor()
        {
            // Set up the Json serializer
            jSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>() { new StringEnumConverter() }
            };
            messages = new List<object>();
            hasStarted = false;
        }

        /**
         * Summary: Implementation of ProcessMessage which collects messages for JSON output.
         * Arguments: QueueMessage containing the message from the queue, GrouperOptions for config options
         * Returns: bool indicating whether processing should stop
         */
        public bool ProcessMessage(QueueMessage message, GrouperOptions options)
        {
            // Initialize JSON array output on first message
            if (!hasStarted)
            {
                WriteOutput("[", options);
                hasStarted = true;
            }

            object messageObject = null;
            bool isLastMessage = false;

            if (message is GpoResultMessage gpoResultMessage)
            {
                // Parse the GPO result JSON output and add it as an object
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
                isLastMessage = true;
            }
            else if (message is FinishMessage)
            {
                messageObject = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = "Finish",
                    Message = message.GetMessage()
                };
                isLastMessage = true;
            }

            // Add message to array if we have one
            if (messageObject != null)
            {
                // Add comma separator if not the first message
                if (messages.Count > 0)
                {
                    WriteOutput(",", options);
                }
                
                messages.Add(messageObject);
                WriteOutput(JsonConvert.SerializeObject(messageObject, jSettings), options);
            }

            // Close JSON array on final message
            if (isLastMessage)
            {
                WriteOutput("]", options);
                
                // Force clean exit when running with file output to prevent hanging
                if (options.LogToFile && !string.IsNullOrEmpty(options.LogFilePath))
                {
                    // Ensure all output is flushed
                    if (options.LogToConsole)
                    {
                        Console.Out.Flush();
                        Console.Error.Flush();
                    }
                    
                    // Exit cleanly
                    Environment.Exit(0);
                }
                
                return true;
            }

            return false;
        }

        /**
         * Summary: Writes output to console or file based on options
         * Arguments: output string to write, GrouperOptions for configuration
         * Returns: None
         */
        private void WriteOutput(string output, GrouperOptions options)
        {
            if (options.LogToFile && !string.IsNullOrEmpty(options.LogFilePath))
            {
                File.AppendAllText(options.LogFilePath, output + Environment.NewLine);
            }
            
            if (options.LogToConsole)
            {
                Console.WriteLine(output);
            }
        }
    }
} 