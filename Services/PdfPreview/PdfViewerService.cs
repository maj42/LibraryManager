using LibraryManager.Helpers;
using PdfiumViewer;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace LibraryManager.Services.PdfPreview
{
    public class PdfViewerService : IPdfViewerService
    {
        private PdfDocument _document;
        private FileStream _currentFileStream;
        public int PageCount => _document?.PageCount ?? 0;

        public bool Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                _document?.Dispose();
                _currentFileStream?.Dispose();
                
                _currentFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _document = PdfDocument.Load(_currentFileStream);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pdf load error: {ex}");
                _document = null;
                return false;
            }
        }

        public BitmapImage RenderPage(int pageIndex)
        {
            if (_document == null || pageIndex < 0 || pageIndex >= PageCount)
                return null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Rendering page");
                using var image = _document.Render(pageIndex, 300, 300, true);
                var copy = new Bitmap(image);
                var bitmapImage = PdfPreviewHelper.ConvertBitmapToBitmapImage(copy);
                copy.Dispose();

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RenderPage error: {ex}");
                return null;
            }
        }

        public void Dispose()
        {
            _document?.Dispose();
            _currentFileStream?.Dispose();
        }
    }
}
