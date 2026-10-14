using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp9.Models
{
    internal class ScanStatistics
    {
        private readonly object _sync = new();

        public long FilesScanned { get; private set; }

        public long FilesWithForbiddenWords { get; private set; }

        public long TotalReplacements { get; private set; }

        public long TotalBytes { get; private set; }

        public long Errors { get; private set; }

        private readonly Dictionary<string, long> _wordCounts =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<ScanResult> _results = new();

        public void FileScanned(long size)
        {
            lock (_sync)
            {
                FilesScanned++;
                TotalBytes += size;
            }
        }

        public void AddResult(ScanResult result)
        {
            lock (_sync)
            {
                FilesWithForbiddenWords++;
                TotalReplacements += result.ReplacementCount;

                _results.Add(result);

                foreach (var pair in result.WordCounts)
                {
                    if (!_wordCounts.ContainsKey(pair.Key))
                        _wordCounts[pair.Key] = 0;

                    _wordCounts[pair.Key] += pair.Value;
                }
            }
        }

        public void AddError()
        {
            lock (_sync)
            {
                Errors++;
            }
        }

        public List<ScanResult> GetResults()
        {
            lock (_sync)
            {
                return _results.ToList();
            }
        }

        public List<KeyValuePair<string, long>> GetTopWords(int count)
        {
            lock (_sync)
            {
                return _wordCounts
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Take(count)
                    .ToList();
            }
        }

        public (long scanned, long found, long replacements,
                long bytes, long errors) GetSnapshot()
        {
            lock (_sync)
            {
                return (
                    FilesScanned,
                    FilesWithForbiddenWords,
                    TotalReplacements,
                    TotalBytes,
                    Errors
                );
            }
        }
    }
}
