using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WinFormsApp9.Models;

namespace WinFormsApp9.Services
{
    internal class FileScanner
    {
        private readonly List<string> _forbiddenWords;
        private readonly string _outputDirectory;

        // Обмежує кількість одночасно оброблюваних файлів.
        private readonly SemaphoreSlim _semaphore;

        // Якщо Reset() — потоки працюють.
        // Якщо Reset() не встановлено — потоки очікують.
        private readonly ManualResetEventSlim _pauseEvent =
            new(true);

        private CancellationTokenSource? _cts;

        public event Action<long>? FileScanned;
        public event Action<ScanResult>? ForbiddenFileFound;
        public event Action<string>? Error;
        public event Action<string>? StatusChanged;

        public FileScanner(
            IEnumerable<string> forbiddenWords,
            string outputDirectory,
            int maxParallelism = 4)
        {
            _forbiddenWords = forbiddenWords
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Length)
                .ToList();

            _outputDirectory = outputDirectory;

            _semaphore = new SemaphoreSlim(
                Math.Max(1, maxParallelism));
        }

        public async Task<ScanStatistics> StartAsync()
        {
            if (_forbiddenWords.Count == 0)
                throw new InvalidOperationException(
                    "Не задано заборонених слів.");

            if (!Directory.Exists(_outputDirectory))
                Directory.CreateDirectory(_outputDirectory);

            _cts = new CancellationTokenSource();

            var statistics = new ScanStatistics();

            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d =>
                    {
                        try
                        {
                            return d.IsReady;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                StatusChanged?.Invoke(
                    $"Знайдено накопичувачів: {drives.Count}");

                var tasks = new List<Task>();

                foreach (var drive in drives)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;

                    tasks.Add(
                        ScanDirectoryAsync(
                            drive.RootDirectory.FullName,
                            statistics,
                            _cts.Token));
                }

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke("Сканування зупинено.");
            }
            finally
            {
                _pauseEvent.Set();
            }

            return statistics;
        }

        public void Pause()
        {
            _pauseEvent.Reset();
            StatusChanged?.Invoke("Сканування призупинено.");
        }

        public void Resume()
        {
            _pauseEvent.Set();
            StatusChanged?.Invoke("Сканування відновлено.");
        }

        public void Stop()
        {
            _cts?.Cancel();

            // Важливо: якщо потоки зараз перебувають у Wait(),
            // вони повинні прокинутися.
            _pauseEvent.Set();

            StatusChanged?.Invoke("Надіслано команду зупинки...");
        }

        private async Task ScanDirectoryAsync(
            string directory,
            ScanStatistics statistics,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            WaitWhilePaused(token);

            string[] files;
            string[] directories;

            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception ex)
            {
                statistics.AddError();
                Error?.Invoke(
                    $"Немає доступу до {directory}: {ex.Message}");

                return;
            }

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                WaitWhilePaused(token);

                await _semaphore.WaitAsync(token);

                try
                {
                    await ProcessFileAsync(
                        file,
                        statistics,
                        token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    statistics.AddError();
                    Error?.Invoke(
                        $"{file}: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            try
            {
                directories = Directory.GetDirectories(directory);
            }
            catch
            {
                return;
            }

            var directoryTasks = new List<Task>();

            foreach (var subDirectory in directories)
            {
                token.ThrowIfCancellationRequested();

                // Не скануємо нашу власну папку результатів.
                if (IsInsideOutputDirectory(subDirectory))
                    continue;

                directoryTasks.Add(
                    ScanDirectoryAsync(
                        subDirectory,
                        statistics,
                        token));
            }

            await Task.WhenAll(directoryTasks);
        }

        private async Task ProcessFileAsync(
            string file,
            ScanStatistics statistics,
            CancellationToken token)
        {
            WaitWhilePaused(token);

            FileInfo info;

            try
            {
                info = new FileInfo(file);

                if (!info.Exists)
                    return;
            }
            catch
            {
                return;
            }

            string text;

            try
            {
                text = await ReadTextFileAsync(file, token);
            }
            catch
            {
                // Наприклад, файл може бути двійковим.
                // Такі файли просто пропускаємо.
                return;
            }

            statistics.FileScanned(info.Length);
            FileScanned?.Invoke(statistics.GetSnapshot().scanned);

            token.ThrowIfCancellationRequested();

            var matches = FindForbiddenWords(text);

            if (matches.Count == 0)
                return;

            var result = new ScanResult
            {
                OriginalPath = file,
                FileSize = info.Length,
                ReplacementCount = matches.Sum(x => x.Value),
                WordCounts = matches
            };

            // Створюємо окрему директорію для результатів.
            string relativePath;

            try
            {
                relativePath = Path.GetRelativePath(
                    Path.GetPathRoot(file) ?? string.Empty,
                    file);
            }
            catch
            {
                relativePath = Path.GetFileName(file);
            }

            relativePath = SanitizePath(relativePath);

            string destination =
                Path.Combine(_outputDirectory, relativePath);

            string? destinationDirectory =
                Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            // 1. Копія оригіналу
            string copiedPath =
                destination + ".original";

            await CopyFileAsync(
                file,
                copiedPath,
                token);

            result.CopiedPath = copiedPath;

            // 2. Версія із заміненими словами
            string sanitizedPath =
                destination + ".sanitized.txt";

            string sanitizedText =
                ReplaceForbiddenWords(text);

            await File.WriteAllTextAsync(
                sanitizedPath,
                sanitizedText,
                new UTF8Encoding(false),
                token);

            result.SanitizedPath = sanitizedPath;

            statistics.AddResult(result);

            ForbiddenFileFound?.Invoke(result);
        }

        private Dictionary<string, int> FindForbiddenWords(
            string text)
        {
            var result = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var word in _forbiddenWords)
            {
                string pattern =
                    $@"(?<!\p{{L}}){Regex.Escape(word)}(?!\p{{L}})";

                int count = Regex.Matches(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant).Count;

                if (count > 0)
                    result[word] = count;
            }

            return result;
        }

        private string ReplaceForbiddenWords(string text)
        {
            string result = text;

            foreach (var word in _forbiddenWords)
            {
                string pattern =
                    $@"(?<!\p{{L}}){Regex.Escape(word)}(?!\p{{L}})";

                result = Regex.Replace(
                    result,
                    pattern,
                    "*******",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant);
            }

            return result;
        }

        private static async Task<string> ReadTextFileAsync(
            string path,
            CancellationToken token)
        {
            // StreamReader автоматично визначає UTF-8/UTF-16
            // за BOM. UTF-8 використовується як стандартне
            // кодування без BOM.
            using var reader = new StreamReader(
                path,
                new UTF8Encoding(false, true),
                detectEncodingFromByteOrderMarks: true);

            return await reader.ReadToEndAsync(token);
        }

        private static async Task CopyFileAsync(
            string source,
            string destination,
            CancellationToken token)
        {
            const int bufferSize = 1024 * 64;

            await using var sourceStream = new FileStream(
                source,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using var destinationStream = new FileStream(
                destination,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await sourceStream.CopyToAsync(
                destinationStream,
                bufferSize,
                token);
        }

        private void WaitWhilePaused(
            CancellationToken token)
        {
            _pauseEvent.Wait(token);
        }

        private bool IsInsideOutputDirectory(string directory)
        {
            try
            {
                string fullDirectory =
                    Path.GetFullPath(directory)
                        .TrimEnd(Path.DirectorySeparatorChar);

                string fullOutput =
                    Path.GetFullPath(_outputDirectory)
                        .TrimEnd(Path.DirectorySeparatorChar);

                return fullDirectory.Equals(
                           fullOutput,
                           StringComparison.OrdinalIgnoreCase)
                       ||
                       fullDirectory.StartsWith(
                           fullOutput + Path.DirectorySeparatorChar,
                           StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string SanitizePath(string path)
        {
            foreach (char c in Path.GetInvalidPathChars())
                path = path.Replace(c, '_');

            return path;
        }
    }
}
