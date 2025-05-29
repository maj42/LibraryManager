using LibraryManager.Helpers;
using LibraryManager.Models;
using LibraryManager.Services.FileManagement;
using LibraryManager.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows;
using System;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;

namespace LibraryManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<PdfFile> PdfFiles { get; set; } = new();
        public ObservableCollection<LogEntry> LogMessages { get; set; } = new();
        public ObservableCollection<InstrumentStatus> Instruments { get; set; } = new();

        public ICommand LoadFilesCommand { get; }
        public ICommand MoveFilesCommand { get; }
        public ICommand CancelCommand => new RelayCommand(() => _cancellationTokenSource?.Cancel());
        public ICommand SetProgramCommand { get; }
        public ICommand ApplyProgramCommand { get; }
        private RelayCommand _setProgramCommand;
        public ICommand AssignMatchedFilesCommand { get; }

        public ICommand CreateProgramFoldersCommand => _createProgramFoldersCommand;
        private RelayCommand _nextPageCommand;
        private RelayCommand _previousPageCommand;

        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand PreviousPageCommand => _previousPageCommand;
        private RelayCommand _createProgramFoldersCommand;

        public ICommand SetProgramFolderCommand => new RelayCommand(() =>
        {
            foreach (var instrument in Instruments.Where(i => i.IsSelected))
            {
                UpdateInstrumentPath(instrument, _rootFolderPath, ProgramName);
            }
            LogHelper.AddLog(LogMessages, "Set program folders for selected instruments", LogLevel.Success);
        });

        public MainViewModel(IPdfFileManager pdfFileManager)
        {
            _pdfFileManager = pdfFileManager;

            LoadFilesCommand = new AsyncRelayCommand(LoadFilesAsync);
            MoveFilesCommand = new AsyncRelayCommand(MoveFilesAsync);
            _setProgramCommand = new RelayCommand(SetProgramFolders, CanSetProgram);
            SetProgramCommand = _setProgramCommand;
            ApplyProgramCommand = new RelayCommand(ApplyProgram, () => CanApplyProgram);
            _createProgramFoldersCommand = new RelayCommand(CreateProgramFolders, CanCreateProgramFolders);
            AssignMatchedFilesCommand = new RelayCommand(AssignMatchedFiles);
            _nextPageCommand = new RelayCommand(() => PreviewPageIndex++, () => CanGoToNextPage);
            _previousPageCommand = new RelayCommand(() => PreviewPageIndex--, () => CanGoToPreviousPage);
        }

        private readonly AliasManager _aliasManager = new AliasManager();
        private readonly IPdfFileManager _pdfFileManager;
        private CancellationTokenSource _cancellationTokenSource;
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string ProgramName
        {
            get => _programName;
            set
            {
                if (_programName != value)
                {
                    _programName = value;
                    OnPropertyChanged();

                    CanApplyProgram = !string.IsNullOrEmpty(_programName);
                    RefreshProgramPaths();
                    RefreshCommands();
                }
            }
        }
        private string _programName;
        private string _rootFolderPath;
        public bool IsProgramSet
        {
            get => _isProgramSet;
            set
            {
                _isProgramSet = value;
                OnPropertyChanged();
            }
        }
        private bool _isProgramSet;
        private bool _programCheckedOnce;

        private bool _canApplyProgram;
        public bool CanApplyProgram
        {
            get => _canApplyProgram;
            set
            {
                _canApplyProgram = value;
                OnPropertyChanged();
            }
        }

        private PdfFile _selectedPdf;
        public PdfFile SelectedPdf
        {
            get => _selectedPdf;
            set
            {
                if (_selectedPdf != value)
                {
                    _selectedPdf = value;
                    OnPropertyChanged();
                    if (_selectedPdf != null)
                    {
                        LoadPdfPreview(_selectedPdf.FullPath);
                    }
                    else
                    {
                        PreviewImage = null;
                    }

                }
            }
        }

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

        private int _previewPageIndex;
        public int PreviewPageIndex
        {
            get => _previewPageIndex;
            set
            {
                _previewPageIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewPageDisplay));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));

                _nextPageCommand?.RaiseCanExecuteChanged();
                _previousPageCommand?.RaiseCanExecuteChanged();

                RenderPreviewPage();
            }
        }

        private int _previewPageCount;
        public int PreviewPageCount
        {
            get => _previewPageCount;
            set
            {
                _previewPageCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewPageDisplay));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
            }
        }

        public string PreviewPageDisplay => $"{PreviewPageIndex + 1} / {PreviewPageCount}";
        public bool CanGoToPreviousPage => PreviewPageIndex > 0;
        public bool CanGoToNextPage => PreviewPageIndex < PreviewPageCount - 1;

        private PdfDocument _currentPreviewDocument;

        private void LoadPdfPreview(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.AddLog(LogMessages, $"PDF file does not exist: {filePath}", LogLevel.Error);
                    PreviewImage = null;
                    PreviewPageCount = 0;
                    return;
                }
                _currentPreviewDocument?.Dispose();
                _currentPreviewDocument = PdfDocument.Load(filePath);
                
                PreviewPageCount = _currentPreviewDocument.PageCount;
                PreviewPageIndex = 0;
            }
            catch (Exception ex)
            {
                LogHelper.AddLog(LogMessages, $"Failed to preview PDF: {ex.Message}", LogLevel.Error);
                PreviewImage = null;
                PreviewPageCount = 0;
            }
        }

        private void RenderPreviewPage()
        {
            if (_currentPreviewDocument == null ||
                PreviewPageIndex < 0 || PreviewPageIndex >= PreviewPageCount)
            {
                LogHelper.AddLog(LogMessages, $"Render skipped", LogLevel.Error);
                return;
            }

            try
            {
                using var image = _currentPreviewDocument.Render(PreviewPageIndex, 300, 300, true);
                if (image == null)
                {
                    LogHelper.AddLog(LogMessages, $"Image is null", LogLevel.Error);
                    return;
                }
                var bitmap = (Bitmap)image;

                if (bitmap.Width == 0 || bitmap.Height == 0)
                {
                    LogHelper.AddLog(LogMessages, $"Image has zero dimentions", LogLevel.Error);
                    return;
                }
                PreviewImage = PdfPreviewHelper.ConvertBitmapToBitmapImage(bitmap);
            }
            catch (Exception ex)
            {
                LogHelper.AddLog(LogMessages, $"Failed to render page {PreviewPageIndex + 1}: {ex.Message}");
            }
        }

        private async Task LoadFilesAsync()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the root folder containig instrument folder and PDF's"
            };

            if (dialog?.ShowDialog() != DialogResult.OK) return;

            _rootFolderPath = dialog.SelectedPath;

            Instruments.Clear();
            PdfFiles.Clear();
            LogMessages.Clear();
            ProgressValue = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            var aliasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "aliases.json");
            _aliasManager.LoadAliases(aliasPath);

            string[] instrumentDirs;
            try
            {
                instrumentDirs = Directory.GetDirectories(_rootFolderPath);
                if (instrumentDirs.Length == 0)
                {
                    LogHelper.AddLog(LogMessages, "Selected folder contains no instrument folders.", LogLevel.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.AddLog(LogMessages, $"Failed to get instrument folders: {ex.Message}", LogLevel.Error);
                return;
            }
           
            var instrumentList = new List<InstrumentStatus>();

            foreach (var dir in instrumentDirs)
            {
                var instrumentName = Path.GetFileName(dir);
                var aliasEntry = _aliasManager.Match(instrumentName);

                string displayName = aliasEntry?.MainName ?? instrumentName;

                bool exists = !string.IsNullOrWhiteSpace(ProgramName) &&
                              Directory.Exists(Path.Combine(dir, ProgramName));

                instrumentList.Add(new InstrumentStatus
                {
                    Name = displayName,
                    IsSelected = true,
                    ProgramFolderExists = exists,
                    Weight = aliasEntry?.Weight ?? int.MaxValue,
                    Aliases = aliasEntry?.Aliases ?? new List<string>(),
                    IsAliasMatched = aliasEntry != null
                });
            }

            Instruments.Clear();
            foreach (var instrument in instrumentList.OrderBy(i => i.Weight))
            {
                Instruments.Add(instrument);
            }

            RefreshCommands();

            try
            {
                var files = await _pdfFileManager.LoadPdfsAsync(_rootFolderPath, _cancellationTokenSource.Token);

                foreach (var file in files)
                {
                    var matchedInstrument = Instruments.FirstOrDefault(instr => instr.Aliases.Any(
                        alias => file.FileName.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0));

                    if (matchedInstrument != null)
                    {
                        file.MatchWeight = matchedInstrument.Weight;
                        file.MatchedInstrumentName = matchedInstrument.Name;
                    }
                    else
                    {
                        file.MatchWeight = int.MaxValue;
                    }

                    file.IsAliasMatched = matchedInstrument != null;
                    PdfFiles.Add(file);
                }

                var sortedFiles = PdfFiles.OrderBy(f => f.MatchWeight).ThenBy(f => f.FileName).ToList();
                PdfFiles.Clear();

                foreach (var file in sortedFiles)
                {
                    PdfFiles.Add(file);
                }

                LogHelper.AddLog(LogMessages, $"Loaded {files.Count} PDF files and {Instruments.Count} instruments from {_rootFolderPath}",
                    LogLevel.Success);
            }
            catch (OperationCanceledException)
            {
                LogHelper.AddLog(LogMessages, "Loading Cancelled.", LogLevel.Error);
            }
        }

        private async Task MoveFilesAsync()
        {
            ProgressValue = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<int>(val => ProgressValue = val);

            try
            {
                int movedFiles = 0;
                var logs = new List<string>();
                var assignedPdfCount = Instruments
                    .SelectMany(i => i.AssignedFiles)
                    .Count();

                if (assignedPdfCount == 0)
                {
                    LogHelper.AddLog(LogMessages, "No files assigned to instruments.", LogLevel.Error);
                    return;
                }

                foreach (var instrument in Instruments.Where(i => i.ProgramFolderExists && i.AssignedFiles.Any()))
                {
                    string targetFolder = instrument.ProgramFolderPath;
                    if (string.IsNullOrWhiteSpace(targetFolder))
                    {
                        LogHelper.AddLog(LogMessages, $"Skipping '{instrument.Name}: Program folder path is not set'", LogLevel.Error);
                        continue;
                    }

                    var filesToMove = instrument.AssignedFiles.ToList();
                    var result = await _pdfFileManager.MovePdfsAsync(filesToMove, targetFolder, progress, _cancellationTokenSource.Token);

                    foreach (var file in filesToMove)
                    {
                        PdfFiles.Remove(file);
                    }

                    logs.AddRange(result);
                    movedFiles += filesToMove.Count;
                }

                if (movedFiles == 0)
                {
                    LogHelper.AddLog(LogMessages, "No files were moved", LogLevel.Error);
                }
            }
            catch(OperationCanceledException)
            {
                LogHelper.AddLog(LogMessages, "Moving Cancelled", LogLevel.Error);
            }
        }

        private void SetProgramFolders()
        {
            foreach (var instrument in Instruments.Where(i => i.IsSelected))
            {
                UpdateInstrumentPath(instrument, _rootFolderPath, ProgramName);
            }

            LogHelper.AddLog(LogMessages, "Checked program folders for selected instruments");
            RefreshCommands();
        }

        private void UpdateInstrumentPath(InstrumentStatus instrument, string rootFolder, string programName)
        {
            if (!instrument.IsSelected || string.IsNullOrWhiteSpace(rootFolder) || string.IsNullOrWhiteSpace(programName))
            {
                return;
            }

            var path = Path.Combine(rootFolder, instrument.Name, ProgramName);
            instrument.ProgramFolderPath = path;

            if (Directory.Exists(path))
            {
                instrument.ProgramFolderExists = true;
                LogHelper.AddLog(LogMessages, $"Program folder for '{instrument.Name}' found: {path}");
            }
            else
            {
                instrument.ProgramFolderExists = false;
            }
        }

        private bool CanSetProgram()
        {
            return !string.IsNullOrWhiteSpace(ProgramName);
        }

        private void ApplyProgram()
        {
            if (string.IsNullOrWhiteSpace(_programName) || string.IsNullOrWhiteSpace(_rootFolderPath)) return;

            foreach (var instrument in Instruments)
            {
                var path = Path.Combine(_rootFolderPath, instrument.Name, ProgramName);
                instrument.ProgramFolderExists = Directory.Exists(path);
            }

            CanApplyProgram = false;
        }

        private void CreateProgramFolders()
        {
            foreach (var instrument in Instruments.Where(i => i.IsSelected && !i.ProgramFolderExists))
            {
                var instrumentPath = Path.Combine(_rootFolderPath, instrument.Name);
                var programPath = Path.Combine(instrumentPath, ProgramName);

                try
                {
                    Directory.CreateDirectory(programPath);
                    instrument.ProgramFolderExists = true;
                    LogHelper.AddLog(LogMessages, $"Created folder: {programPath}");
                }
                catch(Exception ex )
                {
                    LogHelper.AddLog(LogMessages, $"Error creating folder '{programPath}': {ex.Message}", LogLevel.Error);
                }
            }

            RefreshCommands();
        }

        private bool CanCreateProgramFolders()
        {
            return !string.IsNullOrWhiteSpace(_rootFolderPath) &&
                   !string.IsNullOrWhiteSpace(ProgramName) &&
                   Instruments.Any(i => i.IsSelected && !i.ProgramFolderExists);
        }

        public void AssignPdfToInstrument(PdfFile file, InstrumentStatus instrument, bool copyInstead =  false)
        {
            if (!copyInstead && PdfFiles.Contains(file))
            {
                PdfFiles.Remove(file);
            }

            if(!instrument.AssignedFiles.Contains(file))
            {
                instrument.AssignedFiles.Add(file);
            }

            LogHelper.AddLog(LogMessages, $"{(copyInstead ? "Copied" : "Assigned")} '{file.FileName}' to '{instrument.Name}'", LogLevel.Success);
        }

        private void AssignMatchedFiles()
        {
            foreach(var file in PdfFiles.ToList())
            {
                var matchedInstrument = Instruments.FirstOrDefault(instr => instr.Aliases.Any(
                    alias => file.FileName.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0));
                if (matchedInstrument != null)
                {
                    AssignPdfToInstrument(file, matchedInstrument);
                }
            }
            LogHelper.AddLog(LogMessages, "All matched files assigned.", LogLevel.Success);
        }

        private void RefreshProgramPaths()
        {
            foreach (var instrument in Instruments)
            {
                UpdateInstrumentPath(instrument, _rootFolderPath, _programName);
            }
        }

        private void RefreshCommands()
        {
            _setProgramCommand?.RaiseCanExecuteChanged();
            _createProgramFoldersCommand?.RaiseCanExecuteChanged();
        }
    }
}