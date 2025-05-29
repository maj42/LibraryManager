using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;

#nullable enable

namespace LibraryManager.Services
{
    public class AliasEntry
    {
        public string? MainName { get; set; }
        public List<string> Aliases { get; set; } = new();
        public int Weight { get; set; }
    }

    public class AliasManager
    {
        private List<AliasEntry> _entries = new();

        public void LoadAliases(string path)
        {
            if (!File.Exists(path))
            {
                _entries.Clear();
            }

            var json = File.ReadAllText(path);
            _entries = JsonSerializer.Deserialize<List<AliasEntry>>(json) ?? new List<AliasEntry>();
        }


        public AliasEntry? Match(string input)
        {
            input = Normalize(input);
            return _entries.FirstOrDefault(e => e.Aliases.Any(a => Normalize(a) == input));
        }

        public static string Normalize(string input)
        {
            return input.ToLower().Trim();
        }

        public IEnumerable<AliasEntry> AllEntries => _entries;
    }
}
