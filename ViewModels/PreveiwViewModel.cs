using LibraryManager.Helpers;
using LibraryManager.Services.PdfPreview;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LibraryManager.ViewModels
{
    public class PreviewViewModel : BaseViewModel
    {
        private readonly IPdfViewerService _pdfViewerService;

        private RelayCommand _nextPageCommand;
        private RelayCommand _previousPageCommand;

        private BitmapImage _previewImage;
        public BitmapImage PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged();
            }
        }

        public int PreviewPageIndex
        {
            get => _previewPageIndex;
            set
            {
                if (_previewPageIndex == value) return;
                _previewPageIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewPageDisplay));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));

                PreviewImage = _pdfViewerService.RenderPage(_previewPageIndex);

                _nextPageCommand?.RaiseCanExecuteChanged();
                _previousPageCommand?.RaiseCanExecuteChanged();
            }
        }
        private int _previewPageIndex;

        public string PreviewPageDisplay => $"{PreviewPageIndex + 1} / {_pdfViewerService.PageCount}";
        public bool CanGoToNextPage => PreviewPageIndex < _pdfViewerService.PageCount - 1;
        public bool CanGoToPreviousPage => PreviewPageIndex > 0;

        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand PreviousPageCommand => _previousPageCommand;
        public ICommand LoadPreviewCommand { get; }

        public PreviewViewModel(IPdfViewerService pdfViewerService)
        {
            _pdfViewerService = pdfViewerService;

            _nextPageCommand = new RelayCommand(() => PreviewPageIndex++, () => CanGoToNextPage);
            _previousPageCommand = new RelayCommand(() => PreviewPageIndex--, () => CanGoToPreviousPage);
            LoadPreviewCommand = new RelayCommand<string>(LoadPreview);
        }

        public void LoadPreview(string filePath)
        {
            if (_pdfViewerService.Load(filePath))
            {
                PreviewPageIndex = 0;
                PreviewImage = _pdfViewerService.RenderPage(0);

                OnPropertyChanged(nameof(PreviewPageDisplay));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));

                _nextPageCommand?.RaiseCanExecuteChanged();
                _previousPageCommand?.RaiseCanExecuteChanged();
            }
            else
            {
                PreviewImage = null;
                OnPropertyChanged(nameof(PreviewPageDisplay));
            }
        }
    }
}
