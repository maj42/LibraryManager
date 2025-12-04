namespace LibraryManager.Services.Dialogs
{
    public interface IDialogService
    {
        bool Confirm(string message, string title);
        public string ShowInputDialog(string title, string message, string defaultValue);
    }
}
