using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LibraryManager.Resources.Converters
{
    public class SelectedTabToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!int.TryParse(value?.ToString(), out var selectedIndex))
                return Visibility.Collapsed;

            if (parameter == null)
                return Visibility.Collapsed;

            if (!int.TryParse(parameter.ToString(), out var targetIndex))
                return Visibility.Collapsed;

            return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
