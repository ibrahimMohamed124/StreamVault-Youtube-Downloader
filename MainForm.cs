using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace StreamVaultWinForms
{
    public partial class MainForm : Form
    {
        private TextBox? txtUrl;
        private Button? btnAnalyze;
        private FlowLayoutPanel? flowPanel;
        private Label? lblDestination;
        private FolderBrowserDialog? folderBrowser;
        private string destinationFolder = string.Empty;
        private readonly YoutubeClient ytClient = new YoutubeClient();

        public MainForm()
        {
            InitializeComponent();
            BuildUI();

            destinationFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "StreamVault");

            Directory.CreateDirectory(destinationFolder);
            lblDestination!.Text = $"Destination Folder: {destinationFolder}";
        }

        private void BuildUI()
        {
            Text = "StreamVault - WinForms (.NET 8)";
            Size = new Size(980, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(26, 24, 36);

            var header = new Label
            {
                Text = "StreamVault",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 180, 255),
                AutoSize = true,
                Location = new Point(14, 10)
            };
            Controls.Add(header);

            txtUrl = new TextBox
            {
                PlaceholderText = "Paste video or playlist URL here...",
                Font = new Font("Segoe UI", 10),
                Width = 700,
                Location = new Point(14, 50)
            };
            Controls.Add(txtUrl);

            btnAnalyze = new Button
            {
                Text = "Paste & Analyze",
                Location = new Point(730, 48),
                Width = 200,
                Height = 30,
                BackColor = Color.FromArgb(96, 75, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAnalyze.FlatAppearance.BorderSize = 0;
            btnAnalyze.Click += BtnAnalyze_Click;
            Controls.Add(btnAnalyze);

            var chooseFolderBtn = new Button
            {
                Text = "Choose Folder",
                ForeColor = Color.White,
                Location = new Point(14, 90),
                Width = 140,
                Height = 28
            };
            chooseFolderBtn.Click += ChooseFolderBtn_Click;
            Controls.Add(chooseFolderBtn);

            lblDestination = new Label
            {
                Text = "Destination Folder: ",
                BackColor = Color.White,
                ForeColor = Color.Black,
                Location = new Point(170, 95),
                AutoSize = true
            };
            Controls.Add(lblDestination);

            var queueLabel = new Label
            {
                Text = "Download Activity Queue",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(14, 130),
                AutoSize = true
            };
            Controls.Add(queueLabel);

            flowPanel = new FlowLayoutPanel
            {
                Location = new Point(14, 160),
                Size = new Size(940, 380),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            Controls.Add(flowPanel);

            folderBrowser = new FolderBrowserDialog();
        }

        private void ChooseFolderBtn_Click(object? sender, EventArgs e)
        {
            if (folderBrowser!.ShowDialog() == DialogResult.OK)
            {
                destinationFolder = folderBrowser.SelectedPath;
                lblDestination!.Text = $"Destination Folder: {destinationFolder}";
            }
        }

        private async void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            var url = txtUrl!.Text?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please paste a YouTube video URL.", "No URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAnalyze!.Enabled = false;

            // رسالة انتظار للمستخدم
            var waitingLabel = new Label
            {
                Text = "Please wait... analyzing video.",
                ForeColor = Color.Yellow,
                Location = new Point(14, 550),
                AutoSize = true
            };
            Controls.Add(waitingLabel);
            waitingLabel.BringToFront();

            try
            {
                // تحليل الفيديو
                var video = await ytClient.Videos.GetAsync(url);
                var streamManifest = await ytClient.Videos.Streams.GetManifestAsync(video.Id);
                var muxedStreams = streamManifest.GetMuxedStreams()
                                                 .Where(s => s.VideoQuality.Label != null)
                                                 .OrderBy(s => s.VideoQuality.MaxHeight)
                                                 .ToList();

                if (!muxedStreams.Any())
                {
                    MessageBox.Show("No downloadable streams found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // تصميم Panel اختيار الجودة والتحمبل
                var panel = new Panel
                {
                    Size = new Size(650, 70), // تم توسيعه
                    BackColor = Color.FromArgb(45, 45, 65),
                    Margin = new Padding(10, 5, 10, 5)
                };

                var lblTitle = new Label
                {
                    Text = video.Title.Length > 60 ? video.Title.Substring(0, 57) + "..." : video.Title,
                    ForeColor = Color.White,
                    Location = new Point(10, 10),
                    AutoSize = true
                };
                panel.Controls.Add(lblTitle);

                var qualityCombo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(10, 35),
                    Width = 120
                };
                foreach (var s in muxedStreams)
                    qualityCombo.Items.Add(s.VideoQuality.Label);

                qualityCombo.SelectedIndex = muxedStreams.Count - 1; // أعلى جودة افتراضيًا
                panel.Controls.Add(qualityCombo);

                var btnDownload = new Button
                {
                    Text = "Download",
                    Location = new Point(150, 33),
                    Size = new Size(100, 30),
                    BackColor = Color.MediumSlateBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnDownload.FlatAppearance.BorderSize = 0;

                btnDownload.Click += async (_, __) =>
                {
                    var selectedStream = muxedStreams[qualityCombo.SelectedIndex];
                    var item = new DownloadItemControl(video.Title, selectedStream, destinationFolder, ytClient);
                    flowPanel!.Controls.Add(item);
                    await item.StartAsync();
                    panel.Dispose(); // إزالة لوحة الاختيار بعد بدء التحميل
                };

                panel.Controls.Add(btnDownload);
                flowPanel!.Padding = new Padding(5); // منع تآكل الزرار
                flowPanel!.Controls.Add(panel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to analyze URL:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAnalyze.Enabled = true;
                Controls.Remove(waitingLabel); // إزالة رسالة الانتظار بعد الانتهاء
            }
        }

    }

    public partial class DownloadItemControlWithQuality : UserControl
    {
        private readonly string _title;
        private readonly List<MuxedStreamInfo> _streams;
        private readonly string _destinationFolder;
        private readonly YoutubeClient _ytClient;
        private readonly ComboBox _qualityCombo;
        private CancellationTokenSource? _cts;
        private bool _isPaused = false;

        private Label lblTitle;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnPauseResume;

        public DownloadItemControlWithQuality(string title, List<MuxedStreamInfo> streams, string destinationFolder, YoutubeClient ytClient, ComboBox qualityCombo)
        {
            _title = title;
            _streams = streams;
            _destinationFolder = destinationFolder;
            _ytClient = ytClient;
            _qualityCombo = qualityCombo;
            BuildUI();
        }

        private void BuildUI()
        {
            lblTitle = new Label
            {
                Text = _title.Length > 60 ? _title.Substring(0, 57) + "..." : _title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            lblStatus = new Label
            {
                Text = "Queued...",
                ForeColor = Color.Silver,
                AutoSize = true,
                Location = new Point(10, 35)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(10, 60),
                Size = new Size(500, 12)
            };

            btnPauseResume = new Button
            {
                Text = "Pause",
                BackColor = Color.MediumSlateBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(520, 25),
                Size = new Size(75, 28)
            };
            btnPauseResume.FlatAppearance.BorderSize = 0;
            btnPauseResume.Click += BtnPauseResume_Click;

            _qualityCombo.Location = new Point(520, 55);

            BackColor = Color.FromArgb(45, 45, 65);
            Size = new Size(610, 90);

            Controls.Add(lblTitle);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnPauseResume);
            Controls.Add(_qualityCombo);

            _ = StartAsync(); // نبدأ التحميل مباشرة بعد إضافته
        }

        private void BtnPauseResume_Click(object? sender, EventArgs e)
        {
            if (_isPaused)
            {
                _isPaused = false;
                lblStatus.Text = "Resuming...";
                btnPauseResume.Text = "Pause";
                _cts = new CancellationTokenSource();
                _ = StartAsync();
            }
            else
            {
                _isPaused = true;
                _cts?.Cancel();
                lblStatus.Text = "Paused";
                btnPauseResume.Text = "Resume";
            }
        }

        public async Task StartAsync()
        {
            try
            {
                lblStatus.Text = "Downloading...";
                _cts = new CancellationTokenSource();

                var selectedStream = _streams[_qualityCombo.SelectedIndex];
                string fileName = GetSafeFilename($"{_title}.mp4");
                string outputPath = Path.Combine(_destinationFolder, fileName);

                await using var stream = await _ytClient.Videos.Streams.GetAsync(selectedStream);
                await using var file = File.Create(outputPath);

                byte[] buffer = new byte[16 * 1024];
                long totalBytes = (long)selectedStream.Size.Bytes;
                long received = 0;

                while (true)
                {
                    int bytes = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytes == 0)
                        break;

                    await file.WriteAsync(buffer, 0, bytes, _cts.Token);
                    received += bytes;
                    double progress = (double)received / totalBytes * 100;
                    progressBar.Value = Math.Min(100, (int)progress);
                    lblStatus.Text = $"Downloading... {progressBar.Value}%";

                    if (_isPaused)
                        return;
                }

                lblStatus.Text = "Completed!";
                progressBar.Value = 100;
                btnPauseResume.Enabled = false;
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Paused";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private static string GetSafeFilename(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
