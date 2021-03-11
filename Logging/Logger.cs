using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ThreadedLogging
{
    public class Logger : IDisposable
    {
        #region Properties and Fields

        /// <summary>
        /// Lazy instantiation of this class. We need an underlying instance since we implement <see cref="IDisposable"/>
        /// </summary>
        private static readonly Lazy<Logger> _Instance = new Lazy<Logger>(() => new Logger());

        /// <summary>
        /// Synchronization lock object for accessing <see cref="_EntryQueue"/>
        /// </summary>
        private readonly object _QueueLock = new object();

        /// <summary>
        /// The default trace listener that writes output to the log file and the output window
        /// </summary>
        private DefaultTraceListener _TraceListener = null;

        /// <summary>
        /// The queue of log entries that the producer thread(s) enqueue <see cref="LogEntry"/> instances in
        /// </summary>
        private readonly Queue<LogEntry> _EntryQueue = new Queue<LogEntry>();

        /// <summary>
        /// Thread which accesses the <see cref="_EntryQueue"/> and writes output to the log file and the output window
        /// </summary>
        private Thread _ConsumerThread = null;

        /// <summary>
        /// Boolean value indicating if the logger instance should be disposed at the next pass or not
        /// </summary>
        private bool _ShouldTerminate = false;

        #endregion

        #region Methods

        /// <summary>
        /// Internal implementation which initializes the logger with the supplied file path of the log; invoked from the static initialize method
        /// </summary>
        /// <param name="logFilePath">Full path to the log file the logger should use</param>
        private void InitializeInternal(string logFilePath)
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

            _ConsumerThread = new Thread(new ThreadStart(ConsumeLogEntries)) { Name = "Consumer thread" };
            _ConsumerThread.Start();
        }

        /// <summary>
        /// Initializes the logger instance with the supplied file path
        /// </summary>
        /// <param name="logFilePath">Full path to the log file the logger should use</param>
        public static void Initialize(string logFilePath)
        {
            _Instance.Value.InitializeInternal(logFilePath);
        }

        /// <summary>
        /// Enqueues a log message which is read later by the consuming thread
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="traceLevel">The trace level of the message</param>
        private void LogInternal(string message, TraceLevel traceLevel)
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

        /// <summary>
        /// Enqueues a message to the log
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="traceLevel">The trace level of the message</param>
        public static void Log(string message, TraceLevel traceLevel)
        {
            //TODO: Check if initialized, throw exception if not
            _Instance.Value.LogInternal(message, traceLevel);
        }

        /// <summary>
        /// Method which waits for log messages to be enqueued and then outputs them to the log via <see cref="_TraceListener"/><para/>
        /// Intended to be run on <see cref="_ConsumerThread"/>
        /// </summary>
        private void ConsumeLogEntries()
        {
            Queue<LogEntry> entryQueueCopy = null;

            while (true)
            {
                lock (_QueueLock)
                {
                    Monitor.Wait(_QueueLock);

                    //TODO: Is there a threshold where dequeuing is faster than copying the entire queue?
                    if (_EntryQueue.Count != 0)
                    {
                        entryQueueCopy = new Queue<LogEntry>(_EntryQueue);
                        _EntryQueue.Clear();
                    }
                }

                //Figure out if this can be done as a bulk operation - would be nice
                foreach (LogEntry logEntry in entryQueueCopy)
                {
                    _TraceListener.WriteLine($"{DateTime.Now:HH:mm:ss} {logEntry.TraceLevel.ToString().ToUpper()}: {logEntry.Message}");
                }

                if (_ShouldTerminate)
                {
                    entryQueueCopy = new Queue<LogEntry>(_EntryQueue);

                    foreach (LogEntry logEntry in entryQueueCopy)
                    {
                        _TraceListener.WriteLine($"{DateTime.Now:HH:mm:ss} {logEntry.TraceLevel.ToString().ToUpper()}: {logEntry.Message}");
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Disposes the logger instance and writes all remaining entries to the log
        /// </summary>
        public void Dispose()
        {
            _ShouldTerminate = true;

            lock (_QueueLock)
            {
                //Stop the consumer thread from waiting if it is
                Monitor.Pulse(_QueueLock);
            }

            //Wait for the consumer thread to terminate
            _ConsumerThread.Join();

            //Finally dispose of the tracelistener
            ((IDisposable)_TraceListener).Dispose();
        }

        /// <summary>
        /// Terminates the logger instance and writes all remaining entries to the log
        /// </summary>
        public static void Terminate()
        {
            _Instance.Value.Dispose();
        }

        #endregion
    }
}
