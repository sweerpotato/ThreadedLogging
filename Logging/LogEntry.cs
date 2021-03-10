using System;
using System.Diagnostics;

namespace ThreadedLogging
{
    public class LogEntry
    {
        public string Message
        {
            get;
            private set;
        } = String.Empty;

        public TraceLevel TraceLevel
        {
            get;
            private set;
        } = TraceLevel.Off;

        public LogEntry(string message, TraceLevel traceLevel)
        {
            Message = message;
            TraceLevel = traceLevel;
        }
    }
}
