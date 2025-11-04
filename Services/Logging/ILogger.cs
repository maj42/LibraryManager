using LibraryManager.Models;


namespace LibraryManager.Services.Logging
{
    public interface ILogger
    {
        void Log(string message, LogLevel level = LogLevel.Info);
    }
}
