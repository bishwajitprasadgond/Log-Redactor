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
        // Colors
        private readonly Color ColBg = Color.FromArgb(246, 248, 251);
        private readonly Color ColCard = Color.FromArgb(255, 255, 255);
        private readonly Color ColBorder = Color.FromArgb(218, 225, 235);
        private readonly Color ColTextPrimary = Color.FromArgb(24, 30, 42);
        private readonly Color ColTextSecondary = Color.FromArgb(100, 116, 139);
        private readonly Color ColPrimary = Color.FromArgb(16, 85, 154);

        // Core Controls
        private TextBox txtInputFile;
        private TextBox txtOutputFile;
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

        private Button btnExecute;
        private Button btnCancel;
        private Button btnPreview;

        private ProgressBar prgProgress;
        private Label lblStatus;
        private Label lblMetrics;
        private TextBox txtConsole;

        private CancellationTokenSource _cts;
        private bool _isProcessing;

        public MainForm()
        {
            this.Font = new Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point);
            this.AutoScaleMode = AutoScaleMode.Font;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Log Redactor - Enterprise Streaming Redaction Utility";
            this.ClientSize = new Size(960, 840);
            this.MinimumSize = new Size(880, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColBg;
            this.ForeColor = ColTextPrimary;

            // ==========================================
            // 1. TOP HEADER PANEL (Dock = Top)
            // ==========================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = ColCard
            };
            pnlHeader.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(ColBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = "Log Redactor",
                Font = new Font("Segoe UI", 13.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, 12),
                AutoSize = true,
                UseMnemonic = false
            };

            Label lblSubtitle = new Label
            {
                Text = "Deterministic, streaming PII and credential sanitization for enterprise logs",
                Font = new Font("Segoe UI", 9.0F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(25, 38),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // ==========================================
            // 2. MAIN SCROLLABLE/FLOW CONTAINER (Dock = Fill)
            // ==========================================
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColBg,
                Padding = new Padding(24, 16, 24, 16)
            };
            this.Controls.Add(pnlBody);

            int currentY = 16;

            // ==========================================
            // 3. DROP ZONE
            // ==========================================
            Panel pnlDropZone = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48, 86),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ColCard,
                AllowDrop = true
            };
            pnlDropZone.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(160, 185, 215), 1.5f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 1, 1, pnlDropZone.Width - 3, pnlDropZone.Height - 3);
                }
            };
            pnlDropZone.DragEnter += DropZone_DragEnter;
            pnlDropZone.DragDrop += DropZone_DragDrop;

            Label lblDropMain = new Label
            {
                Text = "Drag & Drop Log File Here (.log, .txt, .out, .csv, .json)",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 10),
                Size = new Size(pnlDropZone.Width, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                AllowDrop = true
            };
            lblDropMain.DragEnter += DropZone_DragEnter;
            lblDropMain.DragDrop += DropZone_DragDrop;

            Label lblDropSub = new Label
            {
                Text = "Supports multi-gigabyte files processed in constant ~5 MB memory",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 31),
                Size = new Size(pnlDropZone.Width, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                AllowDrop = true
            };
            lblDropSub.DragEnter += DropZone_DragEnter;
            lblDropSub.DragDrop += DropZone_DragDrop;

            Button btnBrowseInput = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 8.5F),
                Size = new Size(90, 26),
                Location = new Point((pnlDropZone.Width - 90) / 2, 52),
                Anchor = AnchorStyles.Top,
                BackColor = Color.FromArgb(245, 247, 250),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnBrowseInput.Click += delegate(object s, EventArgs e) { BrowseInputFile(); };

            pnlDropZone.Controls.Add(lblDropMain);
            pnlDropZone.Controls.Add(lblDropSub);
            pnlDropZone.Controls.Add(btnBrowseInput);
            pnlBody.Controls.Add(pnlDropZone);

            currentY += 98;

            // ==========================================
            // 4. FILE PATH INPUTS
            // ==========================================
            Label lblInput = new Label
            {
                Text = "Input File:",
                Font = new Font("Segoe UI Semibold", 9.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, currentY + 3),
                Size = new Size(80, 22),
                UseMnemonic = false
            };
            txtInputFile = new TextBox
            {
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(108, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48 - 188, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtInputFile.TextChanged += delegate(object s, EventArgs e) { AutoGenerateOutputPath(); };

            Button btnChooseInput = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(pnlBody.ClientSize.Width - 24 - 76, currentY - 1),
                Size = new Size(76, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnChooseInput.Click += delegate(object s, EventArgs e) { BrowseInputFile(); };

            pnlBody.Controls.Add(lblInput);
            pnlBody.Controls.Add(txtInputFile);
            pnlBody.Controls.Add(btnChooseInput);

            currentY += 34;

            Label lblOutput = new Label
            {
                Text = "Output File:",
                Font = new Font("Segoe UI Semibold", 9.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(24, currentY + 3),
                Size = new Size(80, 22),
                UseMnemonic = false
            };
            txtOutputFile = new TextBox
            {
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(108, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48 - 188, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Button btnChooseOutput = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(pnlBody.ClientSize.Width - 24 - 76, currentY - 1),
                Size = new Size(76, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnChooseOutput.Click += delegate(object s, EventArgs e) { BrowseOutputFile(); };

            pnlBody.Controls.Add(lblOutput);
            pnlBody.Controls.Add(txtOutputFile);
            pnlBody.Controls.Add(btnChooseOutput);

            currentY += 40;

            // ==========================================
            // 5. REDACTION RULES CARD
            // ==========================================
            Panel pnlRulesCard = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48, 126),
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

            Label lblRulesHeader = new Label
            {
                Text = "Active Redaction Rules",
                Font = new Font("Segoe UI Semibold", 9.0F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(16, 8),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlRulesCard.Controls.Add(lblRulesHeader);

            TableLayoutPanel tlpRules = new TableLayoutPanel
            {
                Location = new Point(16, 32),
                Size = new Size(pnlRulesCard.Width - 32, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            tlpRules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpRules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpRules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpRules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpRules.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tlpRules.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            chkIPv4 = new CheckBox { Text = "IPv4 Addresses", Checked = true, AutoSize = true, UseMnemonic = false };
            chkIPv6 = new CheckBox { Text = "IPv6 Addresses", Checked = true, AutoSize = true, UseMnemonic = false };
            chkEmail = new CheckBox { Text = "Email Addresses", Checked = true, AutoSize = true, UseMnemonic = false };
            chkPhone = new CheckBox { Text = "Phone Numbers", Checked = true, AutoSize = true, UseMnemonic = false };

            chkWindowsPath = new CheckBox { Text = "Windows Paths", Checked = true, AutoSize = true, UseMnemonic = false };
            chkJwt = new CheckBox { Text = "JWT Tokens", Checked = true, AutoSize = true, UseMnemonic = false };
            chkApiToken = new CheckBox { Text = "API Keys / Tokens", Checked = true, AutoSize = true, UseMnemonic = false };
            chkCreditCard = new CheckBox { Text = "Credit Card / PAN", Checked = true, AutoSize = true, UseMnemonic = false };

            tlpRules.Controls.Add(chkIPv4, 0, 0);
            tlpRules.Controls.Add(chkIPv6, 1, 0);
            tlpRules.Controls.Add(chkEmail, 2, 0);
            tlpRules.Controls.Add(chkPhone, 3, 0);

            tlpRules.Controls.Add(chkWindowsPath, 0, 1);
            tlpRules.Controls.Add(chkJwt, 1, 1);
            tlpRules.Controls.Add(chkApiToken, 2, 1);
            tlpRules.Controls.Add(chkCreditCard, 3, 1);

            chkAadhaar = new CheckBox { Text = "Aadhaar IDs", Checked = true, AutoSize = true, UseMnemonic = false, Visible = false };
            chkUrlSensitive = new CheckBox { Text = "URL Query Secrets", Checked = true, AutoSize = true, UseMnemonic = false, Visible = false };

            pnlRulesCard.Controls.Add(tlpRules);

            LinkLabel lnkSelectAll = new LinkLabel
            {
                Text = "Select All",
                Font = new Font("Segoe UI", 8.25F),
                LinkColor = ColPrimary,
                Location = new Point(16, 100),
                AutoSize = true
            };
            lnkSelectAll.LinkClicked += delegate(object s, LinkLabelLinkClickedEventArgs e) { SetAllOptions(true); };

            LinkLabel lnkClearAll = new LinkLabel
            {
                Text = "Clear All",
                Font = new Font("Segoe UI", 8.25F),
                LinkColor = ColTextSecondary,
                Location = new Point(84, 100),
                AutoSize = true
            };
            lnkClearAll.LinkClicked += delegate(object s, LinkLabelLinkClickedEventArgs e) { SetAllOptions(false); };

            pnlRulesCard.Controls.Add(lnkSelectAll);
            pnlRulesCard.Controls.Add(lnkClearAll);
            pnlBody.Controls.Add(pnlRulesCard);

            currentY += 138;

            // ==========================================
            // 6. ACTION BUTTONS ROW
            // ==========================================
            btnExecute = new Button
            {
                Text = "Redact Log",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Location = new Point(24, currentY),
                Size = new Size(130, 36),
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
                Location = new Point(164, currentY),
                Size = new Size(90, 36),
                Enabled = false,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += delegate(object s, EventArgs e) { CancelRedaction(); };

            btnPreview = new Button
            {
                Text = "Preview Sample (50 Lines)",
                Font = new Font("Segoe UI", 9.0F),
                Location = new Point(264, currentY),
                Size = new Size(190, 36),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnPreview.Click += delegate(object s, EventArgs e) { PreviewRedaction(); };

            pnlBody.Controls.Add(btnExecute);
            pnlBody.Controls.Add(btnCancel);
            pnlBody.Controls.Add(btnPreview);

            currentY += 48;

            // ==========================================
            // 7. PROGRESS & STATUS TELEMETRY
            // ==========================================
            prgProgress = new ProgressBar
            {
                Location = new Point(24, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Value = 0
            };
            pnlBody.Controls.Add(prgProgress);

            currentY += 14;

            lblStatus = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(24, currentY),
                Size = new Size(380, 18),
                UseMnemonic = false
            };

            lblMetrics = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = ColTextSecondary,
                Location = new Point(pnlBody.ClientSize.Width - 24 - 450, currentY),
                Size = new Size(450, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopRight,
                UseMnemonic = false
            };

            pnlBody.Controls.Add(lblStatus);
            pnlBody.Controls.Add(lblMetrics);

            currentY += 26;

            // ==========================================
            // 8. CONSOLE / ACTIVITY LOG CARD (Anchored to Bottom)
            // ==========================================
            Panel pnlConsoleCard = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(pnlBody.ClientSize.Width - 48, pnlBody.ClientSize.Height - currentY - 16),
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

            Label lblConsoleHeader = new Label
            {
                Text = "Activity Log & Sample Preview",
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                ForeColor = ColTextPrimary,
                Location = new Point(14, 8),
                Size = new Size(300, 18),
                UseMnemonic = false
            };

            txtConsole = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(2, 30),
                Size = new Size(pnlConsoleCard.Width - 4, pnlConsoleCard.Height - 32),
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
