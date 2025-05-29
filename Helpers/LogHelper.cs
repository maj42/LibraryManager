using System;
using System.Collections.ObjectModel;
using LibraryManager.Models;
using System.Windows;

namespace LibraryManager.Helpers
{
    public static class LogHelper
    {
        private const int MaxLogCount = 100;

        public static void AddLog(ObservableCollection<LogEntry> logCollection, string message, LogLevel level = LogLevel.Info)
        {
            var timestamp = DateTime.Now;

            var entry = new LogEntry
            {
                Message = $"{timestamp.ToString("HH:mm:ss")} - {message}",
                Level = level,
                Timestamp = timestamp,
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                logCollection.Add(entry);

                while (logCollection.Count > MaxLogCount)
                {
                    logCollection.RemoveAt(0);
                }
            });
        }
    }
}
