using LibraryManager.Models;
using LibraryManager.ViewModels;

namespace LibraryManager.Helpers
{
    public class AssignmentOperation
    {
        public PdfFile File { get; set; }
        public InstrumentStatus Instrument {  get; set; }
        public bool WasCopy { get; set; }
    }
}
