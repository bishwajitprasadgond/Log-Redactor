using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogRedactor
{
    public class MainForm : Form
    {
        // Palette
        private readonly Color ColBg = Color.FromArgb(245, 247, 250);
        private readonly Color ColCard = Color.FromArgb(255, 255, 255);
        private readonly Color ColBorder = Color.FromArgb(218, 224, 233);
        private readonly Color ColTextPrimary = Color.FromArgb(26, 32, 44);
        private readonly Color ColTextSecondary = Color.FromArgb(100, 116, 139);
        private readonly Color ColPrimary = Color.FromArgb(15, 76, 129);
        private readonly Color ColPrimaryHover = Color.FromArgb(24, 95, 158);
        private readonly Color ColAccent = Color.FromArgb(16, 149, 106);
        private readonly Color ColDanger = Color.FromArgb(220, 38, 38);

        // Header controls
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;

        // Drop zone
        private Panel pnlDropZone;
        private Label lblDropMain;
        private Label lblDropSub;
        private Button btnBrowseInput;

        // Path inputs
        private Label lblInput;
        private TextBox txtInputFile;
        private Button btnChooseInput;
        private Label lblOutput;
        private TextBox txtOutputFile;
        private Button btnChooseOutput;

        // Redaction rules card
        private Panel pnlRulesCard;
        private Label lblRulesHeader;
        private CheckBox chkIPv4;
        private CheckBox chkIPv6;
        private CheckBox chkEmail;
        private CheckBox chkPhone;
        private CheckBox chkWindowsPath;
        private CheckBox chkJwt;
        private CheckBox chkApiToken;
        private CheckBox chkCreditCard;
        private CheckBox chkAadhaar;
        private CheckBox chkUrlSensitive;
        private LinkLabel lnkSelectAll;
        private LinkLabel lnkClearAll;

        // Action controls
        private Button btnExecute;
        private Button btnCancel;
        private Button btnPreview;

        // Progress controls
        private ProgressBar prgProgress;
        private Label lblStatus;
        private Label lblMetrics;

        // Log / Preview card
        private Panel pnlConsoleCard;
        private Label lblConsoleHeader;
        private TextBox txtConsole;

        private CancellationTokenSource _cts;
        private bool _isProcessing;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Log Redactor - Enterprise Streaming Redaction Utility";
            this.Size = new Size(860, 800);
            this.MinimumSize = new Size(820, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = ColBg;
            this.ForeColor = ColTextPrimary;
            this.DoubleBuffered = true;

            // 1. Header Panel
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = ColCard,
                Padding = new Padding(24, 12, 24, 10)
            };
            pnlHeader.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(ColBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            lblTitle = new Label
            {
                Text = "Log Redactor",
                Font = new Font("Segoe UI", 13.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, 10),
                AutoSize = true
            };

            lblSubtitle = new Label
            {
                Text = "Deterministic, streaming PII and credential sanitization for enterprise log files",
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(25, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // Container Panel with scrolling
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 20)
            };
            this.Controls.Add(pnlBody);

            int y = 14;

            // 2. Drop Zone Card
            pnlDropZone = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(794, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColCard,
                AllowDrop = true
            };
            pnlDropZone.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(180, 198, 220), 1.5f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 1, 1, pnlDropZone.Width - 3, pnlDropZone.Height - 3);
                }
            };
            pnlDropZone.DragEnter += DropZone_DragEnter;
            pnlDropZone.DragDrop += DropZone_DragDrop;

            lblDropMain = new Label
            {
                Text = "Drag and drop a log file here, or click Browse to select",
                Font = new Font("Segoe UI Semibold", 10.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 16),
                Size = new Size(794, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                AllowDrop = true
            };
            lblDropMain.DragEnter += DropZone_DragEnter;
            lblDropMain.DragDrop += DropZone_DragDrop;

            lblDropSub = new Label
            {
                Text = "Supports .log, .txt, .csv, .out, .json files. Multi-gigabyte files are processed in constant memory.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 40),
                Size = new Size(794, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                AllowDrop = true
            };
            lblDropSub.DragEnter += DropZone_DragEnter;
            lblDropSub.DragDrop += DropZone_DragDrop;

            btnBrowseInput = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Size = new Size(100, 26),
                Location = new Point((pnlDropZone.Width - 100) / 2, 64),
                Anchor = AnchorStyles.Top,
                BackColor = Color.FromArgb(240, 243, 246),
                ForeColor = ColTextPrimary,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnBrowseInput.Click += delegate(object s, EventArgs e) { BrowseInputFile(); };

            pnlDropZone.Controls.Add(lblDropMain);
            pnlDropZone.Controls.Add(lblDropSub);
            pnlDropZone.Controls.Add(btnBrowseInput);
            pnlBody.Controls.Add(pnlDropZone);

            y += 114;

            // 3. File Inputs Section
            lblInput = new Label
            {
                Text = "Input File:",
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, y + 4),
                Size = new Size(80, 20),
                UseMnemonic = false
            };
            txtInputFile = new TextBox
            {
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(106, y + 1),
                Size = new Size(642, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtInputFile.TextChanged += delegate(object s, EventArgs e) { AutoGenerateOutputPath(); };

            btnChooseInput = new Button
            {
                Text = "Select...",
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(756, y),
                Size = new Size(62, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.System
            };
            btnChooseInput.Click += delegate(object s, EventArgs e) { BrowseInputFile(); };

            pnlBody.Controls.Add(lblInput);
            pnlBody.Controls.Add(txtInputFile);
            pnlBody.Controls.Add(btnChooseInput);

            y += 34;

            lblOutput = new Label
            {
                Text = "Output File:",
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, y + 4),
                Size = new Size(80, 20),
                UseMnemonic = false
            };
            txtOutputFile = new TextBox
            {
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(106, y + 1),
                Size = new Size(642, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnChooseOutput = new Button
            {
                Text = "Select...",
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(756, y),
                Size = new Size(62, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.System
            };
            btnChooseOutput.Click += delegate(object s, EventArgs e) { BrowseOutputFile(); };

            pnlBody.Controls.Add(lblOutput);
            pnlBody.Controls.Add(txtOutputFile);
            pnlBody.Controls.Add(btnChooseOutput);

            y += 42;

            // 4. Redaction Rules Panel
            pnlRulesCard = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(794, 125),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColCard
            };
            pnlRulesCard.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(ColBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlRulesCard.Width - 1, pnlRulesCard.Height - 1);
                }
            };

            lblRulesHeader = new Label
            {
                Text = "Active Redaction Rules",
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(14, 10),
                AutoSize = true,
                UseMnemonic = false
            };

            // Two-row tidy grid with generous spacing
            int col1 = 16;
            int col2 = 175;
            int col3 = 330;
            int col4 = 490;
            int col5 = 650;

            int row1 = 38;
            int row2 = 68;

            chkIPv4 = new CheckBox { Text = "IPv4 Addresses", Checked = true, Location = new Point(col1, row1), AutoSize = true, UseMnemonic = false };
            chkIPv6 = new CheckBox { Text = "IPv6 Addresses", Checked = true, Location = new Point(col2, row1), AutoSize = true, UseMnemonic = false };
            chkEmail = new CheckBox { Text = "Email Addresses", Checked = true, Location = new Point(col3, row1), AutoSize = true, UseMnemonic = false };
            chkPhone = new CheckBox { Text = "Phone Numbers", Checked = true, Location = new Point(col4, row1), AutoSize = true, UseMnemonic = false };
            chkWindowsPath = new CheckBox { Text = "Windows Paths", Checked = true, Location = new Point(col5, row1), AutoSize = true, UseMnemonic = false };

            chkJwt = new CheckBox { Text = "JWT Tokens", Checked = true, Location = new Point(col1, row2), AutoSize = true, UseMnemonic = false };
            chkApiToken = new CheckBox { Text = "API Keys / Tokens", Checked = true, Location = new Point(col2, row2), AutoSize = true, UseMnemonic = false };
            chkCreditCard = new CheckBox { Text = "Credit Card / PAN", Checked = true, Location = new Point(col3, row2), AutoSize = true, UseMnemonic = false };
            chkAadhaar = new CheckBox { Text = "Aadhaar Numbers", Checked = true, Location = new Point(col4, row2), AutoSize = true, UseMnemonic = false };
            chkUrlSensitive = new CheckBox { Text = "URL Query Secrets", Checked = true, Location = new Point(col5, row2), AutoSize = true, UseMnemonic = false };

            lnkSelectAll = new LinkLabel
            {
                Text = "Select All",
                Font = new Font("Segoe UI", 8.25F),
                LinkColor = ColPrimary,
                Location = new Point(16, 98),
                AutoSize = true
            };
            lnkSelectAll.LinkClicked += delegate(object s, LinkLabelLinkClickedEventArgs e) { SetAllOptions(true); };

            lnkClearAll = new LinkLabel
            {
                Text = "Clear All",
                Font = new Font("Segoe UI", 8.25F),
                LinkColor = ColTextSecondary,
                Location = new Point(80, 98),
                AutoSize = true
            };
            lnkClearAll.LinkClicked += delegate(object s, LinkLabelLinkClickedEventArgs e) { SetAllOptions(false); };

            pnlRulesCard.Controls.Add(lblRulesHeader);
            pnlRulesCard.Controls.Add(chkIPv4);
            pnlRulesCard.Controls.Add(chkIPv6);
            pnlRulesCard.Controls.Add(chkEmail);
            pnlRulesCard.Controls.Add(chkPhone);
            pnlRulesCard.Controls.Add(chkWindowsPath);
            pnlRulesCard.Controls.Add(chkJwt);
            pnlRulesCard.Controls.Add(chkApiToken);
            pnlRulesCard.Controls.Add(chkCreditCard);
            pnlRulesCard.Controls.Add(chkAadhaar);
            pnlRulesCard.Controls.Add(chkUrlSensitive);
            pnlRulesCard.Controls.Add(lnkSelectAll);
            pnlRulesCard.Controls.Add(lnkClearAll);
            pnlBody.Controls.Add(pnlRulesCard);

            y += 138;

            // 5. Action Buttons Row
            btnExecute = new Button
            {
                Text = "Redact Log",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Location = new Point(24, y),
                Size = new Size(140, 36),
                BackColor = ColPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExecute.FlatAppearance.BorderSize = 0;
            btnExecute.Click += async delegate(object s, EventArgs e) { await StartRedactionAsync(); };

            btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(172, y),
                Size = new Size(90, 36),
                Enabled = false,
                FlatStyle = FlatStyle.System
            };
            btnCancel.Click += delegate(object s, EventArgs e) { CancelRedaction(); };

            btnPreview = new Button
            {
                Text = "Preview Sample (50 Lines)",
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(270, y),
                Size = new Size(185, 36),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnPreview.Click += delegate(object s, EventArgs e) { PreviewRedaction(); };

            pnlBody.Controls.Add(btnExecute);
            pnlBody.Controls.Add(btnCancel);
            pnlBody.Controls.Add(btnPreview);

            y += 46;

            // 6. Progress and Metrics
            prgProgress = new ProgressBar
            {
                Location = new Point(24, y),
                Size = new Size(794, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Value = 0
            };
            pnlBody.Controls.Add(prgProgress);

            y += 14;

            lblStatus = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(24, y),
                Size = new Size(420, 18),
                UseMnemonic = false
            };

            lblMetrics = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(450, y),
                Size = new Size(368, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopRight,
                UseMnemonic = false
            };

            pnlBody.Controls.Add(lblStatus);
            pnlBody.Controls.Add(lblMetrics);

            y += 26;

            // 7. Activity & Console Card
            pnlConsoleCard = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(794, 210),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColCard
            };
            pnlConsoleCard.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(ColBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlConsoleCard.Width - 1, pnlConsoleCard.Height - 1);
                }
            };

            lblConsoleHeader = new Label
            {
                Text = "Activity Log & Sample Preview",
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(12, 8),
                Size = new Size(300, 18),
                UseMnemonic = false
            };

            txtConsole = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(1, 30),
                Size = new Size(792, 178),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Consolas", 9.0F),
                BackColor = Color.FromArgb(250, 252, 255),
                ForeColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false
            };

            pnlConsoleCard.Controls.Add(lblConsoleHeader);
            pnlConsoleCard.Controls.Add(txtConsole);
            pnlBody.Controls.Add(pnlConsoleCard);
        }

        private void SetAllOptions(bool state)
        {
            chkIPv4.Checked = state;
            chkIPv6.Checked = state;
            chkEmail.Checked = state;
            chkPhone.Checked = state;
            chkCreditCard.Checked = state;
            chkAadhaar.Checked = state;
            chkJwt.Checked = state;
            chkApiToken.Checked = state;
            chkUrlSensitive.Checked = state;
            chkWindowsPath.Checked = state;
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void DropZone_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    txtInputFile.Text = files[0];
                }
            }
        }

        private void BrowseInputFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Log File to Redact";
                ofd.Filter = "Log and Text Files (*.log;*.txt;*.out;*.csv;*.json)|*.log;*.txt;*.out;*.csv;*.json|All Files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtInputFile.Text = ofd.FileName;
                }
            }
        }

        private void BrowseOutputFile()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Select Destination Output File";
                sfd.Filter = "Log and Text Files (*.log;*.txt;*.out;*.csv)|*.log;*.txt;*.out;*.csv|All Files (*.*)|*.*";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutputFile.Text = sfd.FileName;
                }
            }
        }

        private void AutoGenerateOutputPath()
        {
            if (string.IsNullOrEmpty(txtInputFile.Text) || string.IsNullOrEmpty(txtInputFile.Text.Trim())) return;
            try
            {
                string dir = Path.GetDirectoryName(txtInputFile.Text);
                if (dir == null) dir = "";
                string filename = Path.GetFileNameWithoutExtension(txtInputFile.Text);
                string ext = Path.GetExtension(txtInputFile.Text);
                txtOutputFile.Text = Path.Combine(dir, filename + "_redacted" + ext);
            }
            catch
            {
                // Ignore parse errors on manual typing
            }
        }

        private RedactionRuleOptions BuildOptionsFromUI()
        {
            var opts = new RedactionRuleOptions();
            opts.MaskIPv4 = chkIPv4.Checked;
            opts.MaskIPv6 = chkIPv6.Checked;
            opts.MaskEmail = chkEmail.Checked;
            opts.MaskPhone = chkPhone.Checked;
            opts.MaskCreditCard = chkCreditCard.Checked;
            opts.MaskAadhaar = chkAadhaar.Checked;
            opts.MaskJwt = chkJwt.Checked;
            opts.MaskApiToken = chkApiToken.Checked;
            opts.MaskUrlSensitive = chkUrlSensitive.Checked;
            opts.MaskWindowsUserPath = chkWindowsPath.Checked;
            return opts;
        }

        private async Task StartRedactionAsync()
        {
            string inFile = txtInputFile.Text != null ? txtInputFile.Text.Trim() : "";
            string outFile = txtOutputFile.Text != null ? txtOutputFile.Text.Trim() : "";

            if (string.IsNullOrEmpty(inFile) || !File.Exists(inFile))
            {
                MessageBox.Show(this, "The specified input file does not exist. Please select a valid file path.", "Input File Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(outFile))
            {
                MessageBox.Show(this, "Please specify a valid output file path.", "Output File Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.Equals(Path.GetFullPath(inFile), Path.GetFullPath(outFile), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "The output file path cannot match the input file path. Please specify a different output filename.", "Path Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isProcessing = true;
            _cts = new CancellationTokenSource();
            btnExecute.Enabled = false;
            btnCancel.Enabled = true;
            prgProgress.Value = 0;
            lblStatus.Text = "Initializing stream...";
            lblMetrics.Text = "";
            txtConsole.Text = string.Format("Starting redaction stream...\r\nInput:  {0}\r\nOutput: {1}\r\n\r\n", inFile, outFile);

            var options = BuildOptionsFromUI();
            var processor = new StreamingLogProcessor();

            processor.ProgressChanged += delegate(ProcessingProgressReport report)
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                this.BeginInvoke(new Action(delegate()
                {
                    int pct = Math.Min(100, Math.Max(0, (int)report.PercentComplete));
                    prgProgress.Value = pct;
                    lblStatus.Text = string.Format("Processing: {0}%", pct);
                    lblMetrics.Text = string.Format("{0:N0} lines | {1:N0} masks | {2:F1} MB/s", report.LinesProcessed, report.RedactionsApplied, report.MegaBytesPerSecond);
                }));
            };

            ProcessingSummary summary = null;
            try
            {
                summary = await Task.Factory.StartNew<ProcessingSummary>(delegate()
                {
                    return processor.ProcessFile(inFile, outFile, options, _cts.Token);
                });
            }
            catch (Exception ex)
            {
                summary = new ProcessingSummary();
                summary.Success = false;
                summary.ErrorMessage = ex.Message;
            }
            finally
            {
                _isProcessing = false;
                btnExecute.Enabled = true;
                btnCancel.Enabled = false;
            }

            if (summary.Cancelled)
            {
                lblStatus.Text = "Cancelled by user.";
                txtConsole.AppendText("[!] Processing stopped. Partial output saved.\r\n");
            }
            else if (summary.Success)
            {
                prgProgress.Value = 100;
                lblStatus.Text = "Completed successfully.";
                string log = string.Format(
                    "--------------------------------------------------\r\n" +
                    "Execution Summary:\r\n" +
                    "  Status:             Success\r\n" +
                    "  Total File Size:    {0:F2} MB\r\n" +
                    "  Total Lines:        {1:N0}\r\n" +
                    "  Redactions Applied: {2:N0}\r\n" +
                    "  Elapsed Time:       {3:F2} s\r\n" +
                    "  Throughput Rate:    {4:F2} MB/s\r\n" +
                    "  Output Location:    {5}\r\n" +
                    "--------------------------------------------------\r\n",
                    summary.TotalBytesProcessed / (1024.0 * 1024.0),
                    summary.TotalLinesProcessed,
                    summary.TotalRedactions,
                    summary.Duration.TotalSeconds,
                    (summary.TotalBytesProcessed / (1024.0 * 1024.0)) / Math.Max(0.001, summary.Duration.TotalSeconds),
                    summary.OutputFilePath);
                txtConsole.AppendText(log);

                MessageBox.Show(this, string.Format("Redaction completed successfully.\n\nLines processed: {0:N0}\nRedactions applied: {1:N0}\nDuration: {2:F2} seconds", summary.TotalLinesProcessed, summary.TotalRedactions, summary.Duration.TotalSeconds), "Process Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = "Execution failed.";
                txtConsole.AppendText(string.Format("[Error] {0}\r\n", summary.ErrorMessage));
                MessageBox.Show(this, "An error occurred during processing:\n" + summary.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelRedaction()
        {
            if (_isProcessing && _cts != null)
            {
                btnCancel.Enabled = false;
                lblStatus.Text = "Stopping stream...";
                _cts.Cancel();
            }
        }

        private void PreviewRedaction()
        {
            string inFile = txtInputFile.Text != null ? txtInputFile.Text.Trim() : "";
            if (string.IsNullOrEmpty(inFile) || !File.Exists(inFile))
            {
                MessageBox.Show(this, "Please select an existing input log file to preview.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var options = BuildOptionsFromUI();
                var engine = new RedactionEngine(options);

                txtConsole.Text = "--- Sample Redaction Preview (First 50 Lines) ---\r\n\r\n";
                using (var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    string line;
                    int lineCount = 0;
                    int totalRedactions = 0;
                    while ((line = reader.ReadLine()) != null && lineCount < 50)
                    {
                        lineCount++;
                        int count;
                        string redacted = engine.ProcessLine(line, out count);
                        totalRedactions += count;
                        txtConsole.AppendText(redacted + "\r\n");
                    }
                    txtConsole.AppendText(string.Format("\r\n--- End of Preview ({0} lines scanned, {1} redactions applied) ---\r\n", lineCount, totalRedactions));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to preview file:\n" + ex.Message, "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
