using System.Windows;
using System.Windows.Controls;

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

        public string ShowInputDialog(string title, string message, string defaultValue = "")
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = Application.Current?.MainWindow
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 10)
            };

            panel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75
            };

            okButton.Click += (_, __) =>
            {
                window.DialogResult = true;
                window.Close();
            };

            cancelButton.Click += (_, __) =>
            {
                window.DialogResult = false;
                window.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);

            window.Content = panel;

            return window.ShowDialog() == true
                ? textBox.Text.Trim()
                : null;
        }
    }
}
