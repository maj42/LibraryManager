using Microsoft.Extensions.Logging;
using System;

namespace LibraryManager.Models
{
    public enum LogLevel
    {
        Info, 
        Success,
        Error
    }

    public class LogEntry
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
