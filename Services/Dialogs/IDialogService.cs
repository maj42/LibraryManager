namespace LibraryManager.Services.Dialogs
{
    public interface IDialogService
    {
        bool Confirm(string message, string title);
    }
}
