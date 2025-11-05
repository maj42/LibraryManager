using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibraryManager.Models;
using LibraryManager.Services.FileStorage;

namespace LibraryManager.Services.FileManagement
{
    class LocalPdfFileManager : IPdfFileManager
    {
        private readonly IFileStorageService _fileStorage;

        public LocalPdfFileManager(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public async Task<List<PdfFile>> LoadPdfsAsync(string folderPath, CancellationToken token = default)
        {
            var files = await _fileStorage.GetPdfFilesAsync(folderPath);
            return files.Select(path => new PdfFile
            {
                FileName = Path.GetFileName(path),
                FullPath = path
            }).ToList();
            //    var files = Directory.GetFiles(folderPath, "*.pdf");
            //    var list = files.Select(path => new PdfFile
            //    {
            //        FileName = Path.GetFileName(path),
            //        FullPath = path
            //    }).ToList();
            //    return list;
            //}, token);
        }

        public async Task<List<string>> MovePdfsAsync(IEnumerable<PdfFile> files, string targetFolder, IProgress<int> progress = null, 
            CancellationToken token = default)
        {
            var logs = new List<string>();
            int count = 0;

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string targetPath = GetSafeDestinationPath(targetFolder, file.FileName);
                    await _fileStorage.MoveFileAsync(file.FullPath, targetPath);
                    logs.Add($"Moved: {file.FileName}");
                }
                catch (Exception ex)
                {
                    logs.Add($"Failed to move {file.FileName}: {ex.Message}");
                }

                progress?.Report(count++);
            }

            return logs;
            //return Task.Run(() =>
            //{
            //    List<string> logs = new();
            //    int count = 0;
            //    foreach (var file in files)
            //    {
            //        token.ThrowIfCancellationRequested();

            //        try
            //        {
            //            string targetPath = GetSafeDestinationPath(targetFolder, file.FileName);
            //            File.Move(file.FullPath, targetPath);
            //            logs.Add($"Moved: {file.FileName}");
            //        }
            //        catch (Exception ex)
            //        {
            //            logs.Add($"Failed to move {file.FileName}: {ex.Message}");
            //        }

            //        count++;
            //        progress?.Report(count);
            //    }
            //    return logs;
            //}, token);
        }

        private string GetSafeDestinationPath(string folder, string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            string path = Path.Combine(folder, fileName);
            int counter = 1;

            while (File.Exists(path))
            {
                path = Path.Combine(folder, $"{baseName}_{counter++}{ext}");
            }

            return path;
        }
    }
}
