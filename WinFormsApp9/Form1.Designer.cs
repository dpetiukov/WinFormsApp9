using System.Drawing.Text;

namespace WinFormsApp9
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;

        private TextBox txtForbiddenWords = null!;
        private Button btnLoadWords = null!;
        private TextBox txtOutputFolder = null!;
        private Button btnSelectOutput = null!;
        private NumericUpDown numericThreads = null!;

        private Button btnStart = null!;
        private Button btnPause = null!;
        private Button btnResume = null!;
        private Button btnStop = null!;

        private ProgressBar progressFiles = null!;

        private Label lblStatus = null!;
        private Label lblScannedFiles = null!;
        private Label lblFoundFiles = null!;
        private Label lblReplacements = null!;
        private Label lblErrors = null!;

        private TextBox txtResults = null!;
        private TextBox txtTopWords = null!;
        private TextBox txtLog = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            txtForbiddenWords = new TextBox();
            btnLoadWords = new Button();

            txtOutputFolder = new TextBox();
            btnSelectOutput = new Button();

            numericThreads = new NumericUpDown();

            btnStart = new Button();
            btnPause = new Button();
            btnResume = new Button();
            btnStop = new Button();

            progressFiles = new ProgressBar();

            lblStatus = new Label();
            lblScannedFiles = new Label();
            lblFoundFiles = new Label();
            lblReplacements = new Label();
            lblErrors = new Label();

            txtResults = new TextBox();
            txtTopWords = new TextBox();
            txtLog = new TextBox();

            ((System.ComponentModel.ISupportInitialize)
                numericThreads).BeginInit();

            SuspendLayout();

            // -------------------------------
            // Forbidden words
            // -------------------------------

            var lblWords = new Label
            {
                Text = "Заборонені слова:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            txtForbiddenWords = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(500, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText =
                    "Вводьте слова через Enter або ;"
            };

            btnLoadWords = new Button
            {
                Text = "Завантажити",
                Location = new Point(535, 45),
                Size = new Size(120, 35)
            };

            btnLoadWords.Click +=
                btnLoadWords_Click;

            // -------------------------------
            // Output directory
            // -------------------------------

            var lblOutput = new Label
            {
                Text = "Папка результатів:",
                Location = new Point(20, 140),
                AutoSize = true
            };

            txtOutputFolder = new TextBox
            {
                Location = new Point(20, 165),
                Size = new Size(500, 30),
                Text = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "ForbiddenScannerResults")
            };

            btnSelectOutput = new Button
            {
                Text = "Вибрати...",
                Location = new Point(535, 165),
                Size = new Size(120, 30)
            };

            btnSelectOutput.Click +=
                btnSelectOutput_Click;

            // -------------------------------
            // Threads
            // -------------------------------

            var lblThreads = new Label
            {
                Text = "Потоків:",
                Location = new Point(680, 45),
                AutoSize = true
            };

            numericThreads = new NumericUpDown
            {
                Location = new Point(740, 40),
                Width = 70,
                Minimum = 1,
                Maximum = 32,
                Value = Math.Clamp(
                    Environment.ProcessorCount,
                    1,
                    32)
            };

            // -------------------------------
            // Buttons
            // -------------------------------

            btnStart = new Button
            {
                Text = "▶ Старт",
                Location = new Point(680, 100),
                Size = new Size(100, 40)
            };

            btnPause = new Button
            {
                Text = "Ⅱ Пауза",
                Location = new Point(790, 100),
                Size = new Size(100, 40)
            };

            btnResume = new Button
            {
                Text = "▶ Продовжити",
                Location = new Point(900, 100),
                Size = new Size(125, 40)
            };

            btnStop = new Button
            {
                Text = "■ Стоп",
                Location = new Point(1030, 100),
                Size = new Size(100, 40)
            };

            btnStart.Click +=
                btnStart_Click;

            btnPause.Click +=
                btnPause_Click;

            btnResume.Click +=
                btnResume_Click;

            btnStop.Click +=
                btnStop_Click;

            // -------------------------------
            // Progress
            // -------------------------------

            progressFiles = new ProgressBar
            {
                Location = new Point(20, 220),
                Size = new Size(1110, 25),
                Style = ProgressBarStyle.Blocks
            };

            // -------------------------------
            // Statistics
            // -------------------------------

            lblStatus = new Label
            {
                Text = "Готовий.",
                Location = new Point(20, 260),
                Size = new Size(1100, 25),
                Font = new System.Drawing.Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold)
            };

            lblScannedFiles = new Label
            {
                Text = "Перевірено файлів: 0",
                Location = new Point(20, 295),
                AutoSize = true
            };

            lblFoundFiles = new Label
            {
                Text = "Знайдено файлів: 0",
                Location = new Point(230, 295),
                AutoSize = true
            };

            lblReplacements = new Label
            {
                Text = "Кількість замін: 0",
                Location = new Point(440, 295),
                AutoSize = true
            };

            lblErrors = new Label
            {
                Text = "Помилок: 0",
                Location = new Point(650, 295),
                AutoSize = true
            };

            // -------------------------------
            // Results
            // -------------------------------

            var lblResults = new Label
            {
                Text = "Знайдені файли:",
                Location = new Point(20, 335),
                AutoSize = true
            };

            txtResults = new TextBox
            {
                Location = new Point(20, 360),
                Size = new Size(700, 300),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true
            };

            // -------------------------------
            // Top words
            // -------------------------------

            var lblTopWords = new Label
            {
                Text = "Топ-10 слів:",
                Location = new Point(740, 335),
                AutoSize = true
            };

            txtTopWords = new TextBox
            {
                Location = new Point(740, 360),
                Size = new Size(390, 140),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // -------------------------------
            // Log
            // -------------------------------

            var lblLog = new Label
            {
                Text = "Журнал:",
                Location = new Point(740, 515),
                AutoSize = true
            };

            txtLog = new TextBox
            {
                Location = new Point(740, 540),
                Size = new Size(390, 120),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both
            };

            // -------------------------------
            // Form
            // -------------------------------

            ClientSize = new Size(1150, 700);

            Controls.Add(lblWords);
            Controls.Add(txtForbiddenWords);
            Controls.Add(btnLoadWords);

            Controls.Add(lblOutput);
            Controls.Add(txtOutputFolder);
            Controls.Add(btnSelectOutput);

            Controls.Add(lblThreads);
            Controls.Add(numericThreads);

            Controls.Add(btnStart);
            Controls.Add(btnPause);
            Controls.Add(btnResume);
            Controls.Add(btnStop);

            Controls.Add(progressFiles);

            Controls.Add(lblStatus);
            Controls.Add(lblScannedFiles);
            Controls.Add(lblFoundFiles);
            Controls.Add(lblReplacements);
            Controls.Add(lblErrors);

            Controls.Add(lblResults);
            Controls.Add(txtResults);

            Controls.Add(lblTopWords);
            Controls.Add(txtTopWords);

            Controls.Add(lblLog);
            Controls.Add(txtLog);

            Text = "Forbidden Words Scanner";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 650);

            ((System.ComponentModel.ISupportInitialize)
                numericThreads).EndInit();

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
