using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryManager.Helpers;
using LibraryManager.Models;

namespace LibraryManager.ViewModels
{
    public class InstrumentStatus : BaseViewModel
    {
        private bool _programFolderExists;
        private bool _isSelected;

        public bool ProgramFolderExists
        {
            get => _programFolderExists;
            set { _programFolderExists = value; OnPropertyChanged(); }
        }
        public string Name { get; set; }
        public string ProgramFolderPath { get; set; }
        public int Weight { get; set; } = int.MaxValue;
        public bool IsAliasMatched { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PdfFile> AssignedFiles { get; set; } = new();
        public override string ToString()
        {
            return $"{Name} ({AssignedFiles.Count} files)";
        }
    }
}
