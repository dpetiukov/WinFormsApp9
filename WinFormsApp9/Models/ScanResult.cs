using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp9.Models
{
    internal class ScanResult
    {
        public string OriginalPath { get; set; } = string.Empty;

        public string CopiedPath { get; set; } = string.Empty;

        public string SanitizedPath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public int ReplacementCount { get; set; }

        public Dictionary<string, int> WordCounts { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public DateTime FoundAt { get; set; } = DateTime.Now;
    }
}
