using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LibraryManager.Services.FileStorage
{
    public class LocalFileStorageService : IFileStorageService
    {
        public Task<IEnumerable<string>> GetPdfFilesAsync(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*pdf");
            return Task.FromResult(files);
        }

        public Task MoveFileAsync(string source, string destination) 
        {
            File.Move(source, destination, overwrite: true);
            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

    }
}