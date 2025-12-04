namespace LibraryManager.Models
{
    public class PdfFile
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool IsSelected { get; set; }
        public int MatchWeight { get; set; } = int.MaxValue;
        public bool IsAliasMatched { get; set; }
        public bool IsFromProgramFolder { get; set; }
        public string? MatchedInstrumentName { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PdfFile other && string.Equals(FullPath, other.FullPath, System.StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return FullPath?.ToLowerInvariant().GetHashCode() ?? 0;
        }
    }
}