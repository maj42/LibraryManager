using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LibraryManager.Adorners;
using LibraryManager.Models;
using LibraryManager.ViewModels;

namespace LibraryManager
{
    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private object? _originalDragSource;
        private const double DragThreshold = 5;
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void PdfFilesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            if (sender is ListBoxItem item && item.DataContext is PdfFile)
            {
                _originalDragSource = item.DataContext;
                item.IsSelected = true;
            }
            else
            {
                _originalDragSource = null;
            }
        }

        private void PdfFilesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _originalDragSource is not PdfFile file)
            {
                return;
            }

            var currentPosition = e.GetPosition(null);
            if (Math.Abs(currentPosition.X - _dragStartPoint.X) > DragThreshold ||
                Math.Abs(currentPosition.Y - _dragStartPoint.Y) > DragThreshold)
            {
                _originalDragSource = null;
                DragDrop.DoDragDrop((DependencyObject)sender, file, DragDropEffects.Copy);
            }
            
        }

        private void AssignedFile_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var dataContext = ((FrameworkElement)sender).DataContext;
                if (dataContext is PdfFile file)
                {
                    DragDrop.DoDragDrop((DependencyObject)sender, file, DragDropEffects.Move);
                }
            }
        }

        private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((DataContext as MainViewModel)?.Preview is PreviewViewModel viewModel)
            {
                if (e.Delta > 0)
                {
                    if (viewModel.PreviousPageCommand.CanExecute(null))
                        viewModel.PreviousPageCommand.Execute(null);
                }
                else if (e.Delta < 0)
                {
                    if (viewModel.NextPageCommand.CanExecute(null))
                        viewModel.NextPageCommand.Execute(null);
                }
            }
        }

        private void ProgramTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;
            
            var tb = sender as TextBox;
            if (tb == null) return;

            if (!string.IsNullOrEmpty(vm.CurrentSuggestionDisplay) && (e.Key == Key.Tab || e.Key == Key.Enter))
            {
                tb.Text = tb.Text + vm.CurrentSuggestionDisplay;
                tb.CaretIndex = tb.Text.Length;

                vm.ProgramName = tb.Text;

                vm.ClearCurrentSuggestion();

                e.Handled |= true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                vm.ClearCurrentSuggestion();
                e.Handled = true;
            }
        }

        private void ProgramTextBox_TextChanged(object sender, EventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            var tb = sender as TextBox;
            if (tb == null) return;

            vm.UpdateCurrentSuggestion();

            if (!string.IsNullOrEmpty(vm.CurrentSuggestionDisplay))
            {
                var dpi = VisualTreeHelper.GetDpi(tb);

                var formatted = new FormattedText(
                    tb.Text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    tb.FlowDirection,
                    new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                    tb.FontSize,
                    Brushes.Black,
                    dpi.PixelsPerDip);

                vm.SuggestionMargin = new Thickness(
                    formatted.WidthIncludingTrailingWhitespace + tb.Padding.Left, 0, 0, 0);
            }
            else
            {
                vm.SuggestionMargin = new Thickness(0, 0, 0, 0);
            }
        }

        private void Instrument_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                e.Effects = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                            ? DragDropEffects.Copy : DragDropEffects.Move;

                if (sender is Expander expander)
                {
                    expander.Background = Brushes.LightGreen;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Instrument_DragLeave(object sender, EventArgs e)
        {
            if (sender is Expander expander)
            {
                expander.Background = Brushes.LightGreen;
            }
        }

        private void Instrument_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                if (sender is Expander expander)
                    expander.Background = Brushes.Transparent;

                if (!e.Data.GetDataPresent(typeof(PdfFile))) return;

                var file = e.Data.GetData(typeof(PdfFile)) as PdfFile;
                var instrument = (sender as Expander)?.Tag as InstrumentStatus;
                if (file != null && instrument != null)
                {
                    bool copy = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    (_viewModel ?? DataContext as MainViewModel)?.AssignPdfToInstrument(file, instrument, copy);
                }
            }
        }

        private void PdfFileListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                var file = e.Data.GetData(typeof(PdfFile)) as PdfFile;
                if (file != null)
                {
                    var vm = DataContext as MainViewModel;
                    vm?.UnassignFile(file);
                }
            }
        }

        private void ScrollViewer_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
