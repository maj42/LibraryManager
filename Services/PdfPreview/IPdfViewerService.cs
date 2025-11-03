using System;
using System.Windows.Media.Imaging;

namespace LibraryManager.Services.PdfPreview
{
    public interface IPdfViewerService : IDisposable
    {
        int PageCount { get; }
        BitmapImage RenderPage(int pageIndex);
        bool Load(string filePath);
    }
}
