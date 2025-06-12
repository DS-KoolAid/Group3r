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
        }

        /**
         * Summary: Implementation of ProcessMessage which collects messages for JSON output.
         * Arguments: QueueMessage containing the message from the queue, GrouperOptions for config options
         * Returns: bool indicating whether processing should stop
         */
        public bool ProcessMessage(QueueMessage message, GrouperOptions options)
        {
            if (message is GpoResultMessage gpoResultMessage)
            {
                // For GPO results, use the dedicated printer for consistent output
                string jsonOutput = options.Printer.OutputGpoResult(gpoResultMessage.GpoResult);
                WriteOutput(jsonOutput, options);
            }
            else if (message is FileResultMessage fileResultMessage)
            {
                var fileResult = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = "FileResult",
                    Message = message.GetMessage(),
                    FileResult = fileResultMessage.Result
                };
                WriteOutput(JsonConvert.SerializeObject(fileResult, jSettings), options);
            }
            else if (message is InfoMessage || message is ErrorMessage)
            {
                var messageObject = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = message.GetType().Name.Replace("Message", ""),
                    Message = message.GetMessage()
                };
                WriteOutput(JsonConvert.SerializeObject(messageObject, jSettings), options);
            }
            else if (message is FatalMessage)
            {
                var errorOutput = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = "Fatal",
                    Message = message.GetMessage()
                };
                WriteOutput(JsonConvert.SerializeObject(errorOutput, jSettings), options);
                return true;
            }
            else if (message is FinishMessage)
            {
                var finishOutput = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = "Finish",
                    Message = message.GetMessage()
                };
                WriteOutput(JsonConvert.SerializeObject(finishOutput, jSettings), options);
                return true;
            }
            // Skip TraceMessage and DebugMessage for cleaner JSON output

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