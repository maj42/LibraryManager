using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LibraryManager.Models;
using LibraryManager.Services.FileManagement;
using LibraryManager.Services.PdfPreview;
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

            DataContext = new MainViewModel(
                new LocalPdfFileManager(),
                new PdfViewerService()
            );

            _viewModel = viewModel;
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

        private void Instrument_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                e.Effects = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                            ? DragDropEffects.Copy : DragDropEffects.Move;

                var border = sender as Border;
                if (border != null)
                {
                    border.Background = Brushes.LightGreen;
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
            var border = sender as Border;
            if (border != null)
            {
                border.Background = Brushes.Transparent;
            }
        }

        private void Instrument_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                var file = e.Data.GetData(typeof(PdfFile)) as PdfFile;
                var border = sender as Border;
                var instrument = border!.Tag as InstrumentStatus;

                if (border != null)
                {
                    border.Background = Brushes.Transparent;
                }

                if (file != null && instrument != null)
                {
                    var viewModel = DataContext as MainViewModel;
                    bool copy = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    viewModel?.AssignPdfToInstrument(file, instrument, copy);
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

        private void Border_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PdfFile)))
            {
                var border = sender as Border;
                if (border != null)
                {
                    border.Background = Brushes.LightGreen;
                }
            }
        }

        private void Border_PreviewDragLeave(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = Brushes.Transparent;
            }
        }

        private void Border_PreviewDrop(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = Brushes.Transparent;
            }

            if (e.Data.GetDataPresent(typeof (PdfFile)))
            {
                var file = e.Data.GetData(typeof(PdfFile)) as PdfFile;
                var instrument = border?.Tag as InstrumentStatus;

                if (file != null && instrument != null)
                {
                    var viewModel = DataContext as MainViewModel;
                    bool copy = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    viewModel?.AssignPdfToInstrument(file, instrument, copy);
                }
            }
        }
    }
}
