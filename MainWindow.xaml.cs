using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibraryManager.Models;
using LibraryManager.Services.FileManagement;
using LibraryManager.ViewModels;

namespace LibraryManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var pdfFileManager = new LocalPdfFileManager(); // or resolve via DI
            DataContext = new MainViewModel(pdfFileManager);
        }

        private void PdfFilesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var listBox = sender as ListBox;
                var pdf = listBox?.SelectedItem as PdfFile;

                if (pdf != null)
                {
                    DragDrop.DoDragDrop(listBox, pdf, DragDropEffects.Copy);
                }
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
