using LibraryManager.Helpers;
using LibraryManager.Models;
using LibraryManager.Services.FileManagement;
using LibraryManager.Services.PdfPreview;
using LibraryManager.Services.Logging;
using LibraryManager.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;


namespace LibraryManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public PreviewViewModel Preview { get; }

        public ObservableCollection<PdfFile> PdfFiles { get; set; } = new();
        public ObservableCollection<LogEntry> LogMessages { get; set; } = new();
        public ILogger _logger { get; }
        public ObservableCollection<InstrumentStatus> Instruments { get; set; } = new();
        
        public readonly Stack<AssignmentOperation> _undoStack = new();

        public ICommand LoadFilesCommand { get; }
        public ICommand MoveFilesCommand { get; }
        public ICommand CancelCommand => new RelayCommand(() => _cancellationTokenSource?.Cancel());
        public ICommand SetProgramCommand { get; }
        public ICommand ApplyProgramCommand { get; }
        public ICommand AssignMatchedFilesCommand { get; }
        public ICommand UndoCommand => _undoCommand;
        public ICommand ArchiveCommand { get; }
        public ICommand CreateProgramFoldersCommand => _createProgramFoldersCommand;
        
        private readonly RelayCommand _undoCommand;
        private RelayCommand _setProgramCommand;
        private RelayCommand _createProgramFoldersCommand;

        public ICommand SetProgramFolderCommand => new RelayCommand(() =>
        {
            foreach (var instrument in Instruments.Where(i => i.IsSelected))
            {
                UpdateInstrumentPath(instrument, _rootFolderPath, ProgramName);
            }
            _logger.Log("Set program folders for selected instruments", LogLevel.Success);
        });

        public MainViewModel(IPdfFileManager pdfFileManager, IPdfViewerService pdfViewerService)
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel created");
            Preview = new PreviewViewModel(pdfViewerService, _logger);


            _pdfFileManager = pdfFileManager;
            _logger = new UiLogger(LogMessages);
            LoadFilesCommand = new AsyncRelayCommand(LoadFilesAsync);
            MoveFilesCommand = new AsyncRelayCommand(MoveFilesAsync);
            _setProgramCommand = new RelayCommand(SetProgramFolders, CanSetProgram);
            SetProgramCommand = _setProgramCommand;
            ApplyProgramCommand = new RelayCommand(ApplyProgram, () => CanApplyProgram);
            _createProgramFoldersCommand = new RelayCommand(CreateProgramFolders, CanCreateProgramFolders);
            AssignMatchedFilesCommand = new RelayCommand(AssignMatchedFiles);
            _undoCommand = new RelayCommand(UndoLastAssignment, CanUndo);
            ArchiveCommand = new RelayCommand(ArchiveExecute);
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

        private string _programName;
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

        private PdfFile _selectedPdf;
        public PdfFile SelectedPdf
        {
            get => _selectedPdf;
            set
            {
                if (_selectedPdf == value) return;
                _selectedPdf = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"SelectedPdf changed: {_selectedPdf?.FileName}");

                if (_selectedPdf != null)
                {
                    Preview.LoadPreview(_selectedPdf.FullPath);
                }
            }
        }

        private string _rootFolderPath;

        private bool _isProgramSet;
        public bool IsProgramSet
        {
            get => _isProgramSet;
            set
            {
                _isProgramSet = value;
                OnPropertyChanged();
            }
        }

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


        // --------------------------- Methods --------------------------------

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
                    _logger.Log("Selected folder contains no instrument folders.", LogLevel.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to get instrument folders: {ex.Message}", LogLevel.Error);
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

                _logger.Log($"Loaded {files.Count} PDF files and {Instruments.Count} instruments from {_rootFolderPath}",
                    LogLevel.Success);
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Loading Cancelled.", LogLevel.Error);
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
                    _logger.Log("No files assigned to instruments.", LogLevel.Error);
                    return;
                }

                var movedFilesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    foreach (var instrument in Instruments.Where(i => i.AssignedFiles.Any()))
                    {
                        string targetFolder = instrument.ProgramFolderPath;

                        if (!instrument.ProgramFolderExists || string.IsNullOrWhiteSpace(targetFolder))
                        {
                            _logger.Log($"Skipping '{instrument.Name}: Program folder path is not set or doesn't exist.'", LogLevel.Error);
                            continue;
                        }

                        foreach (var file in instrument.AssignedFiles.ToList())
                        {
                            if (file == null || string.IsNullOrWhiteSpace(file.FullPath))
                                continue;

                            string targetPath = Path.Combine(targetFolder, file.FileName);

                            try
                            {
                                if (!movedFilesMap.ContainsKey(file.FullPath))
                                {
                                    if (File.Exists(targetPath))
                                    {
                                        File.Delete(targetPath);
                                        _logger.Log($"Overwriting existing file at '{targetPath}'", LogLevel.Info);
                                    }

                                    File.Move(file.FullPath, targetPath);
                                    movedFilesMap[file.FullPath] = targetPath;
                                    movedFiles++;

                                    _logger.Log($"Moved '{file.FileName}' to '{instrument.Name}'", LogLevel.Info);
                                }
                                else
                                {
                                    string sourcePath = movedFilesMap[file.FullPath];

                                    if (!File.Exists(sourcePath))
                                    {
                                        _logger.Log($"Missing source for copy: {sourcePath}", LogLevel.Error);
                                        continue;
                                    }

                                    if (File.Exists(targetPath))
                                    {
                                        File.Delete(targetPath);
                                        _logger.Log($"Overwriting existing file at '{targetPath}'", LogLevel.Info);
                                    }

                                    File.Copy(sourcePath, targetPath, overwrite: true);

                                    _logger.Log($"Moved '{file.FileName}' to '{instrument.Name}'", LogLevel.Success);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Log($"Error moving '{file.FileName}' to '{instrument.Name}': {ex.Message}", LogLevel.Error);
                            }
                        }
                    }
                });
                
                if (movedFiles == 0)
                {
                    _logger.Log("No files were moved", LogLevel.Error);
                }
            }
            catch(OperationCanceledException)
            {
                _logger.Log("Moving Cancelled", LogLevel.Error);
            }
        }

        private void SetProgramFolders()
        {
            foreach (var instrument in Instruments.Where(i => i.IsSelected))
            {
                UpdateInstrumentPath(instrument, _rootFolderPath, ProgramName);
            }

            _logger.Log("Checked program folders for selected instruments");
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
                _logger.Log($"Program folder for '{instrument.Name}' found: {path}");
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
                    _logger.Log($"Created folder: {programPath}");
                }
                catch(Exception ex )
                {
                    _logger.Log($"Error creating folder '{programPath}': {ex.Message}", LogLevel.Error);
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

            _undoStack.Push(new AssignmentOperation
            {
                File = file,
                Instrument = instrument,
                WasCopy = copyInstead
            });

            _undoCommand.RaiseCanExecuteChanged();

            _logger.Log($"{(copyInstead ? "Copied" : "Assigned")} '{file.FileName}' to '{instrument.Name}'", LogLevel.Success);
        }

        private void UndoLastAssignment()
        {
            if (_undoStack.Count == 0) return;

            var op = _undoStack.Pop();

            if (op.Instrument.AssignedFiles.Contains(op.File))
            {
                op.Instrument.AssignedFiles.Remove(op.File);
            }

            if (!op.WasCopy && !PdfFiles.Contains(op.File))
            {
                PdfFiles.Add(op.File);
            }

            _undoCommand.RaiseCanExecuteChanged();

            _logger.Log($"Undid assignment of '{op.File.FileName}' from '{op.Instrument.Name}'", LogLevel.Success);
        }

        private bool CanUndo()
        {
            return _undoStack.Count > 0;
        }

        public void UnassignPdfFromInstrument(PdfFile file, InstrumentStatus instrument)
        {
            if (instrument.AssignedFiles.Contains(file))
            {
                instrument.AssignedFiles.Remove(file);

                _undoStack.Push(new AssignmentOperation
                {
                    File = file,
                    Instrument = instrument,
                    WasCopy = false
                });
            }

            if (!PdfFiles.Contains(file))
            {
                PdfFiles.Add(file);
            }

            _undoCommand.RaiseCanExecuteChanged();

            _logger.Log($"Unassigned '{file.FileName}' from '{instrument.Name}'", LogLevel.Success);
        }

        public void UnassignFile(PdfFile file)
        {
            var instrument = Instruments.FirstOrDefault(i => i.AssignedFiles.Contains(file));
            if (instrument != null)
            {
                UnassignPdfFromInstrument (file, instrument);
            }
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
            _logger.Log("All matched files assigned.", LogLevel.Success);
        }

        private void ArchiveExecute()
        {
            if (string.IsNullOrWhiteSpace(ProgramName))
            {
                _logger.Log("No program is currently set. Archive aborted.", LogLevel.Error);
                return;
            }

            foreach(var instrument in Instruments)
            {
                if (string.IsNullOrWhiteSpace(instrument.ProgramFolderPath))
                {
                    _logger.Log($"No program folder set for '{instrument.Name}. Skipping.'", LogLevel.Info);
                    continue;
                }

                string sourcePath = instrument.ProgramFolderPath;
                string archivePath = Path.Combine(_rootFolderPath, instrument.Name, "Архив");

                if (!Directory.Exists(sourcePath))
                {
                    _logger.Log($"Source folder does not exist for '{instrument.Name}'. Skipping.", LogLevel.Info);
                    continue;
                }

                if (!Directory.Exists(archivePath))
                {
                    _logger.Log($"Archive folder not found for '{instrument.Name}': {archivePath}", LogLevel.Error);
                    return;
                }

                try
                {
                    var files = Directory.GetFiles(sourcePath);
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string destinationFile = Path.Combine(archivePath, fileName);

                        if (File.Exists(destinationFile))
                        {
                            File.Delete(destinationFile);
                            _logger.Log($"Overwriting file in archive for '{instrument.Name}': {fileName}", LogLevel.Info);
                        }

                        File.Move(file, destinationFile);
                    }

                    if (Directory.GetFiles(sourcePath).Length == 0 && Directory.GetDirectories(sourcePath).Length == 0)
                    {
                        Directory.Delete(sourcePath);
                    }
                    instrument.AssignedFiles.Clear();

                    _logger.Log($"Archived files for '{instrument.Name}' to: {archivePath}", LogLevel.Success);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error archiving files for '{instrument.Name}': {ex.Message}", LogLevel.Error);
                }
            }

            RefreshProgramPaths();
            RefreshCommands();
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