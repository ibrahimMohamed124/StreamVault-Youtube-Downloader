using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
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
                BackColor = Color.White,      // هنا خليته أبيض
                ForeColor = Color.Black,      // النص يبقى أسود
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
            try
            {
                var video = await ytClient.Videos.GetAsync(url);
                var streamManifest = await ytClient.Videos.Streams.GetManifestAsync(video.Id);
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                if (streamInfo == null)
                {
                    MessageBox.Show("No downloadable muxed stream found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var item = new DownloadItemControl(video.Title, (MuxedStreamInfo)streamInfo, destinationFolder, ytClient);
                flowPanel!.Controls.Add(item);
                _ = item.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to analyze URL:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAnalyze.Enabled = true;
            }
        }
    }

    // DownloadItemControl و DownloadTask نفس النسخ السابقة بدون تعديل كبير سوى FileSize و HasValue

    public record DownloadProgressReport(double Progress, string StatusText, bool IsCompleted = false, bool IsError = false);

    // يمكنك نسخ نفس DownloadItemControl و DownloadTask من الكود السابق
    // فقط غيّر سطر حجم الملف:
    // long totalSize = (long)streamInfo.Size.Bytes;
}
