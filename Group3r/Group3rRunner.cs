﻿using Group3r.Concurrency;
using Group3r.Options;
using Group3r.View;
using LibSnaffle.Concurrency;
using LibSnaffle.Logging;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Group3r
{
    /**
     * Summary: Responsible for setting up and running GroupCore.
     */
    public class Group3rRunner
    {
        /**
         * Summary: This method is the main controller of the program.
         *          It sets things up and kicks off the threads.
         * Imports: command line args
         * Exports: None
         */

        private static readonly object ConsoleWriterLock = new object();

        public void Run(string[] args)
        {
            // Check if JSON output is requested before printing banner
            bool isJsonOutput = false;
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "-p" || args[i] == "--printer") && i + 1 < args.Length)
                {
                    isJsonOutput = args[i + 1].ToLower() == "json";
                    break;
                }
            }

            if (!isJsonOutput)
            {
                lock (ConsoleWriterLock)
                {
                    Banner.PrintBanner();
                }
            }
            
            GrouperMq mq = new GrouperMq();

            try
            {
                GrouperOptions options = OptionsParser.Parse(args, mq);
                if (options == null)
                {
                    return;
                }
                SetupLogger(options.LogToFile, options.LogToConsole, mq, options.LogLevelString, options.LogFilePath);
                GroupCon controller = new GroupCon(options, mq);
                Task groupConThread = Task.Factory.StartNew(() => { controller.Execute(); });
                HandleForever(options, mq);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception in Group3rRunner. Please report the following error directly to l0ss or file an issue in GitHub:");
                Console.WriteLine(e.ToString());
                DumpQueue(mq);
                Environment.Exit(1);
            }
        }

        /**
         * Summary: Prints the Mq and exits on fatal error.
         * Imports: BlockingMq reference to the Mq
         * Exports: None
         */
        private void DumpQueue(BlockingMq Mq)
        {
            while (Mq.Q.TryTake(out QueueMessage message))
            {
                lock (ConsoleWriterLock)
                {
                    // emergency dump of queue contents to console
                    Console.WriteLine(message.GetMessage());
                }
            }
            if (Debugger.IsAttached)
            {
                lock (ConsoleWriterLock)
                {
                    Console.WriteLine("Emergency quit, dumped queue to console.");
                }
            }
            // TODO: exit nicely by returning to calling context.
        }

        /**
         * Summary: Infinite loop to handle Mq messages.
         * Imports: GroupCoreOptions object which gets passed to the processor.
         * Exports: None
         */
        private void HandleForever(GrouperOptions options, GrouperMq mq)
        {
            // Select the appropriate message processor based on output type
            IMessageProcessor processor;
            if (options.PrinterType?.ToLower() == "json")
            {
                // Use direct file processor if outputting JSON to a file
                if (options.LogToFile && !string.IsNullOrEmpty(options.LogFilePath))
                {
                    processor = new DirectFileJsonProcessor(options.LogFilePath);
                }
                else
                {
                    processor = new JsonMessageProcessor();
                }
            }
            else
            {
                processor = new CliMessageProcessor();
            }

            bool exit = false;
            
            while (exit == false)
            {
                // Use TryTake with a reasonable timeout instead of blocking forever
                // This allows for better process control without implementing a full timeout
                if (mq.Q.TryTake(out QueueMessage msg, 5000)) // 5 second wait
                {
                    lock (ConsoleWriterLock)
                    {
                        exit = processor.ProcessMessage(msg, options);
                    }
                }
            }
            
            // Ensure cleanup for DirectFileJsonProcessor
            if (processor is DirectFileJsonProcessor finalProcessor)
            {
                finalProcessor.Finalize();
            }
        }
        /// <summary>
        /// Used to initialise the logger with custom colours.
        /// </summary>
        /// <param name="logToFile"></param>
        /// <param name="mq"></param>
        /// <param name="logLevelString"></param>
        /// <param name="logFilePath"></param>
        private void SetupLogger(bool logToFile, bool logToConsole, BlockingMq mq, string logLevelString, string logFilePath)
        {
            ColoredConsoleTarget logconsole = new ColoredConsoleTarget("logconsole")
            {
                DetectOutputRedirected = true,
                UseDefaultRowHighlightingRules = false,
                WordHighlightingRules = {
                        new ConsoleWordHighlightingRule("[Trace]", ConsoleOutputColor.DarkGray, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("[Degub]", ConsoleOutputColor.Gray, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("[Info]", ConsoleOutputColor.White, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("[Error]", ConsoleOutputColor.Magenta, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("[Fatal]", ConsoleOutputColor.Red, ConsoleOutputColor.Black),
                        // File result prettifier for snaffler output.
                        new ConsoleWordHighlightingRule("[File]", ConsoleOutputColor.Green, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("[GPO]", ConsoleOutputColor.Cyan, ConsoleOutputColor.Black),
                        new ConsoleWordHighlightingRule("{Red}", ConsoleOutputColor.Red, ConsoleOutputColor.White),
                        new ConsoleWordHighlightingRule("{Black}", ConsoleOutputColor.DarkGray, ConsoleOutputColor.White),
                        new ConsoleWordHighlightingRule("{Yellow}", ConsoleOutputColor.Yellow, ConsoleOutputColor.White),
                        new ConsoleWordHighlightingRule("{Green}", ConsoleOutputColor.Yellow, ConsoleOutputColor.White),
                        /*
                        // TODO: these rules need to be removed and replaced with something better
                        new ConsoleWordHighlightingRule
                        {
                            CompileRegex = true,
                            Regex = @"^\d\d\d\d-\d\d\-\d\d \d\d:\d\d:\d\d [\+-]\d\d:\d\d ",
                            ForegroundColor = ConsoleOutputColor.DarkGray,
                            BackgroundColor = ConsoleOutputColor.Black
                        },
                        new ConsoleWordHighlightingRule
                        {
                            CompileRegex = true,
                            Regex = @"<.*\|.*\|.*\|.*?>",
                            ForegroundColor = ConsoleOutputColor.Cyan,
                            BackgroundColor = ConsoleOutputColor.Black
                        },
                        new ConsoleWordHighlightingRule
                        {
                            CompileRegex = true,
                            Regex = @"\((?:[^\)]*\)){1}",
                            ForegroundColor = ConsoleOutputColor.DarkMagenta,
                            BackgroundColor = ConsoleOutputColor.Black
                        }
                        */
                    }
            };
            Logging.SetupLogger(logToFile, logToConsole, mq, logLevelString, logFilePath, logconsole);
        }
    }
}
