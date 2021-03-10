using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ThreadedLogging
{
    public static class Logger
    {
        #region Properties and Fields

        private static readonly object _QueueLock = new object();

        private static DefaultTraceListener _TraceListener = null;

        private static readonly Queue<LogEntry> _EntryQueue = new Queue<LogEntry>();

        private static Thread _ConsumerThread = null;

        #endregion

        #region Methods

        public static void Initialize(string logFilePath)
        {
            if (String.IsNullOrEmpty(logFilePath))
            {
                throw new ArgumentException("logFilePath cannot be null or empty");
            }

            if (!File.Exists(logFilePath))
            {
                File.CreateText(logFilePath).Close();
            }

            //Remove the default listener
            Trace.Listeners.RemoveAt(0);
            Trace.AutoFlush = true;
            DefaultTraceListener defaultTraceListener = new DefaultTraceListener() { LogFileName = logFilePath };
            Trace.Listeners.Add(defaultTraceListener);

            _TraceListener = defaultTraceListener;

            //TODO: Figure out IsBackground or not 
            _ConsumerThread = new Thread(new ThreadStart(ConsumeLog)) { Name = "Logging thread" };
            _ConsumerThread.Start();
        }

        public static void Log(string message, TraceLevel traceLevel)
        {
            if (!String.IsNullOrEmpty(message))
            {
                lock (_QueueLock)
                {
                    _EntryQueue.Enqueue(new LogEntry(message, traceLevel));
                    Monitor.Pulse(_QueueLock);
                }
            }
        }

        private static void ConsumeLog()
        {
            while (true)
            {
                LogEntry logEntry = null;

                lock (_QueueLock)
                {
                    Monitor.Wait(_QueueLock);

                    if (_EntryQueue.Count != 0)
                    {
                        logEntry = _EntryQueue.Dequeue();
                    }
                }

                _TraceListener.WriteLine($"{DateTime.Now:HH:mm:ss} {logEntry.TraceLevel.ToString().ToUpper()}: {logEntry.Message}");

                //TODO: Implement abort at application shutdown
            }
        }

        #endregion
    }
}
