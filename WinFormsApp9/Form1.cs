using System.Text;
using WinFormsApp9.Models;
using WinFormsApp9.Services;

namespace WinFormsApp9
{
    public partial class Form1 : Form
    {
        private FileScanner? _scanner;

        private ScanStatistics? _statistics;

        private CancellationTokenSource? _uiCts;

        private bool _running;

        public Form1()
        {
            InitializeComponent();

            btnPause.Enabled = false;
            btnResume.Enabled = false;
            btnStop.Enabled = false;

            numericThreads.Value =
                Math.Clamp(Environment.ProcessorCount, 1, 32);
        }

        private void btnLoadWords_Click(
            object sender,
            EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Виберіть файл із забороненими словами"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                txtForbiddenWords.Text =
                    File.ReadAllText(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка читання файлу:\n{ex.Message}",
                    "Помилка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnSelectOutput_Click(
            object sender,
            EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description =
                    "Виберіть папку для результатів"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                txtOutputFolder.Text = dialog.SelectedPath;
        }

        private async void btnStart_Click(
            object sender,
            EventArgs e)
        {
            if (_running)
                return;

            var words = txtForbiddenWords.Text
                .Split(
                    new[] { '\r', '\n', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (words.Count == 0)
            {
                MessageBox.Show(
                    "Введіть хоча б одне заборонене слово.",
                    "Увага",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    txtOutputFolder.Text))
            {
                MessageBox.Show(
                    "Виберіть папку для результатів.",
                    "Увага",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Directory.CreateDirectory(
                txtOutputFolder.Text);

            _running = true;

            SetButtonsRunning();

            progressFiles.Value = 0;
            progressFiles.Style =
                ProgressBarStyle.Marquee;

            lblStatus.Text = "Сканування розпочато...";

            txtResults.Clear();
            txtLog.Clear();

            int parallelism =
                (int)numericThreads.Value;

            _scanner = new FileScanner(
                words,
                txtOutputFolder.Text,
                parallelism);

            _scanner.FileScanned +=
                OnFileScanned;

            _scanner.ForbiddenFileFound +=
                OnForbiddenFileFound;

            _scanner.Error +=
                OnError;

            _scanner.StatusChanged +=
                OnStatusChanged;

            try
            {
                _statistics =
                    await _scanner.StartAsync();

                ShowFinalStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Критична помилка:\n{ex.Message}",
                    "Помилка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _running = false;

                progressFiles.Style =
                    ProgressBarStyle.Blocks;

                progressFiles.Value = 100;

                SetButtonsStopped();

                _scanner = null;
            }
        }

        private void btnPause_Click(
            object sender,
            EventArgs e)
        {
            _scanner?.Pause();

            btnPause.Enabled = false;
            btnResume.Enabled = true;

            lblStatus.Text =
                "Стан: призупинено";
        }

        private void btnResume_Click(
            object sender,
            EventArgs e)
        {
            _scanner?.Resume();

            btnPause.Enabled = true;
            btnResume.Enabled = false;

            lblStatus.Text =
                "Стан: виконання";
        }

        private void btnStop_Click(
            object sender,
            EventArgs e)
        {
            _scanner?.Stop();

            lblStatus.Text =
                "Зупинка сканування...";
        }

        private void OnFileScanned(long count)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                    OnFileScanned(count));

                return;
            }

            lblScannedFiles.Text =
                $"Перевірено файлів: {count:N0}";
        }

        private void OnForbiddenFileFound(
            ScanResult result)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                    OnForbiddenFileFound(result));

                return;
            }

            string words = string.Join(
                ", ",
                result.WordCounts.Select(
                    x => $"{x.Key} ({x.Value})"));

            txtResults.AppendText(
                $"ЗНАЙДЕНО: {result.OriginalPath}\r\n" +
                $"Розмір: {FormatBytes(result.FileSize)}\r\n" +
                $"Заміни: {result.ReplacementCount}\r\n" +
                $"Слова: {words}\r\n" +
                $"Оригінал: {result.CopiedPath}\r\n" +
                $"Sanitized: {result.SanitizedPath}\r\n" +
                new string('-', 80) +
                "\r\n");

            lblFoundFiles.Text =
                $"Файлів із забороненими словами: " +
                $"{_statistics?.GetSnapshot().found ?? 0:N0}";
        }

        private void OnError(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                    OnError(message));

                return;
            }

            txtLog.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }

        private void OnStatusChanged(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                    OnStatusChanged(message));

                return;
            }

            lblStatus.Text = message;
        }

        private void ShowFinalStatistics()
        {
            if (_statistics == null)
                return;

            var snapshot =
                _statistics.GetSnapshot();

            lblScannedFiles.Text =
                $"Перевірено файлів: {snapshot.scanned:N0}";

            lblFoundFiles.Text =
                $"Знайдено файлів: {snapshot.found:N0}";

            lblReplacements.Text =
                $"Кількість замін: " +
                $"{snapshot.replacements:N0}";

            lblErrors.Text =
                $"Помилок: {snapshot.errors:N0}";

            lblStatus.Text =
                "Сканування завершено.";

            txtTopWords.Clear();

            txtTopWords.AppendText(
                "ТОП-10 ЗАБОРОНЕНИХ СЛІВ\r\n");
            txtTopWords.AppendText(
                "==========================\r\n");

            int position = 1;

            foreach (var word in
                     _statistics.GetTopWords(10))
            {
                txtTopWords.AppendText(
                    $"{position}. {word.Key} — " +
                    $"{word.Value} входжень\r\n");

                position++;
            }

            CreateReport(_statistics);
        }

        private void CreateReport(
            ScanStatistics statistics)
        {
            string reportPath =
                Path.Combine(
                    txtOutputFolder.Text,
                    $"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var snapshot =
                statistics.GetSnapshot();

            using var writer =
                new StreamWriter(
                    reportPath,
                    false,
                    new UTF8Encoding(false));

            writer.WriteLine(
                "ЗВІТ ПРО СКАНУВАННЯ ФАЙЛІВ");

            writer.WriteLine(
                "====================================");

            writer.WriteLine(
                $"Дата: {DateTime.Now}");

            writer.WriteLine(
                $"Перевірено файлів: {snapshot.scanned}");

            writer.WriteLine(
                $"Файлів із забороненими словами: " +
                $"{snapshot.found}");

            writer.WriteLine(
                $"Загальна кількість замін: " +
                $"{snapshot.replacements}");

            writer.WriteLine(
                $"Опрацьовано даних: " +
                $"{FormatBytes(snapshot.bytes)}");

            writer.WriteLine(
                $"Помилок: {snapshot.errors}");

            writer.WriteLine();

            writer.WriteLine(
                "ТОП-10 ЗАБОРОНЕНИХ СЛІВ");

            writer.WriteLine(
                "------------------------------------");

            int position = 1;

            foreach (var word in
                     statistics.GetTopWords(10))
            {
                writer.WriteLine(
                    $"{position}. {word.Key}: " +
                    $"{word.Value}");

                position++;
            }

            writer.WriteLine();

            writer.WriteLine(
                "ЗНАЙДЕНІ ФАЙЛИ");

            writer.WriteLine(
                "====================================");

            foreach (var result in
                     statistics.GetResults())
            {
                writer.WriteLine(
                    $"Файл: {result.OriginalPath}");

                writer.WriteLine(
                    $"Розмір: {result.FileSize} bytes");

                writer.WriteLine(
                    $"Кількість замін: " +
                    $"{result.ReplacementCount}");

                writer.WriteLine(
                    $"Слова: " +
                    $"{string.Join(", ",
                        result.WordCounts.Select(
                            x => $"{x.Key}={x.Value}"))}");

                writer.WriteLine(
                    $"Копія: {result.CopiedPath}");

                writer.WriteLine(
                    $"Sanitized: {result.SanitizedPath}");

                writer.WriteLine(
                    "------------------------------------");
            }

            txtLog.AppendText(
                $"Звіт створено: {reportPath}\r\n");
        }

        private void SetButtonsRunning()
        {
            btnStart.Enabled = false;
            btnPause.Enabled = true;
            btnResume.Enabled = false;
            btnStop.Enabled = true;

            txtForbiddenWords.Enabled = false;
            btnLoadWords.Enabled = false;
            txtOutputFolder.Enabled = false;
            btnSelectOutput.Enabled = false;
            numericThreads.Enabled = false;
        }

        private void SetButtonsStopped()
        {
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
            btnStop.Enabled = false;

            txtForbiddenWords.Enabled = true;
            btnLoadWords.Enabled = true;
            txtOutputFolder.Enabled = true;
            btnSelectOutput.Enabled = true;
            numericThreads.Enabled = true;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes =
            {
            "B", "KB", "MB", "GB", "TB"
        };

            double value = bytes;
            int index = 0;

            while (value >= 1024 &&
                   index < sizes.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return $"{value:0.##} {sizes[index]}";
        }
    }
}
