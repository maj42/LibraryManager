using LibraryManager.Helpers;
using LibraryManager.Models;
using System.Collections.ObjectModel;


namespace LibraryManager.Services.Logging
{
    public class UiLogger : ILogger
    {
        private readonly ObservableCollection<LogEntry> _logCollection;

        public UiLogger(ObservableCollection<LogEntry> logCollection)
        {
            _logCollection = logCollection;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            LogHelper.AddLog(_logCollection, message, level);
        }
    }
}
