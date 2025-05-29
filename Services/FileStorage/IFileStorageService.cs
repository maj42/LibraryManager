using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibraryManager.Services.FileStorage
{
    public interface IFileStorageService
    {
        Task<IEnumerable<string>> GetPdfFilesAsync(string folder);
        Task MoveFileAsync(string source, string destination);
        Task<bool> FileExistsAsync(string path);
    }
}