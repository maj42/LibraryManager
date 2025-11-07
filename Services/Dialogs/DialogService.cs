using System.Windows;

namespace LibraryManager.Services.Dialogs
{
    public class DialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
                ) == MessageBoxResult.Yes;
        }
    }
}
