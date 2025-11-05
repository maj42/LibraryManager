using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibraryManager.Models;

namespace LibraryManager.Services.FileManagement
{
    public interface IPdfFileManager
    {
        Task<List<PdfFile>> LoadPdfsAsync(string folderPath, CancellationToken token = default);
        Task<List<string>> MovePdfsAsync(IEnumerable<PdfFile> files, string targetFolder, IProgress<int> progress = null, 
            CancellationToken token = default);
    }
}
