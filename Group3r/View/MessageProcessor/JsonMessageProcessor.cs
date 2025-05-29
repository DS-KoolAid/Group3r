using Group3r.Options;
using LibSnaffle.Concurrency;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Group3r.View
{
    /**
     * Summary: Implementation of IMessageProcessor which outputs messages as JSON.
     */
    class JsonMessageProcessor : IMessageProcessor
    {
        private JsonSerializerSettings jSettings;
        private List<object> outputObjects;

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
            outputObjects = new List<object>();
        }

        /**
         * Summary: Implementation of ProcessMessage which collects messages for JSON output.
         * Arguments: QueueMessage containing the message from the queue, GrouperOptions for config options
         * Returns: bool indicating whether processing should stop
         */
        public bool ProcessMessage(QueueMessage message, GrouperOptions options)
        {
            var messageObject = new
            {
                Timestamp = message.MsgDateTime,
                Type = message.GetType().Name,
                Message = message.MessageString
            };

            if (message is GpoResultMessage gpoResultMessage)
            {
                // For GPO results, output immediately as JSON
                string jsonOutput = options.Printer.OutputGpoResult(gpoResultMessage.GpoResult);
                Console.WriteLine(jsonOutput);
            }
            else if (message is FileResultMessage fileResultMessage)
            {
                var fileResult = new
                {
                    Timestamp = message.MsgDateTime,
                    Type = "FileResult",
                    Message = message.MessageString,
                    FileResult = fileResultMessage.Result
                };
                Console.WriteLine(JsonConvert.SerializeObject(fileResult, jSettings));
            }
            else if (message is FatalMessage || message is FinishMessage)
            {
                // For fatal or finish messages, output any remaining data
                if (message is FatalMessage)
                {
                    var errorOutput = new
                    {
                        Error = "Fatal error occurred",
                        Message = message.MessageString,
                        Timestamp = message.MsgDateTime
                    };
                    Console.WriteLine(JsonConvert.SerializeObject(errorOutput, jSettings));
                }
                else
                {
                    var finishOutput = new
                    {
                        Status = "Completed",
                        Message = message.MessageString,
                        Timestamp = message.MsgDateTime
                    };
                    Console.WriteLine(JsonConvert.SerializeObject(finishOutput, jSettings));
                }
                return true;
            }
            else if (!(message is TraceMessage || message is DebugMessage))
            {
                // For other non-debug messages, output them
                Console.WriteLine(JsonConvert.SerializeObject(messageObject, jSettings));
            }

            return false;
        }
    }
} 