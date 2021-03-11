using System;
using System.Diagnostics;

namespace ThreadedLogging
{
    /// <summary>
    /// Class representing an entry in a log with a message <see cref="String"/> and a <see cref="TraceLevel"/>
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// The message of this log entry
        /// </summary>
        public string Message
        {
            get;
            private set;
        } = String.Empty;

        /// <summary>
        /// The trace level of this log entry
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            private set;
        } = TraceLevel.Off;

        /// <summary>
        /// Constructs a new instance of this class with the supplied <paramref name="message"/> and <paramref name="traceLevel"/>
        /// </summary>
        /// <param name="message">The message of this log entry</param>
        /// <param name="traceLevel">The trace level of this log entry</param>
        public LogEntry(string message, TraceLevel traceLevel)
        {
            Message = message;
            TraceLevel = traceLevel;
        }
    }
}
