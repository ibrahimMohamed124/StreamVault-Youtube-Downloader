using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private MenuStrip menu;

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

            // MENU
            menu = CreateMenu();
            Controls.Add(menu);

            int topOffset = menu.Height + 10;

            var header = new Label
            {
                Text = "StreamVault",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 180, 255),
                AutoSize = true,
                Location = new Point(14, topOffset)
            };
            Controls.Add(header);

            txtUrl = new TextBox
            {
                PlaceholderText = "Paste video or playlist URL here...",
                Font = new Font("Segoe UI", 10),
                Width = 700,
                Location = new Point(14, topOffset + 40)
            };
            Controls.Add(txtUrl);

            btnAnalyze = new Button
            {
                Text = "Paste & Analyze",
                Location = new Point(730, topOffset + 38),
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
                Location = new Point(14, topOffset + 80),
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
                Location = new Point(170, topOffset + 85),
                AutoSize = true
            };
            Controls.Add(lblDestination);

            var queueLabel = new Label
            {
                Text = "Download Activity Queue",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(14, topOffset + 125),
                AutoSize = true
            };
            Controls.Add(queueLabel);

            flowPanel = new FlowLayoutPanel
            {
                Location = new Point(14, topOffset + 155),
                Size = new Size(940, 380),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5)
            };
            Controls.Add(flowPanel);

            folderBrowser = new FolderBrowserDialog();
        }

        private MenuStrip CreateMenu()
        {
            var menu = new MenuStrip()
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White,
                Renderer = new UIHelper.CustomMenuRenderer()
            };

            var mFile = new ToolStripMenuItem("File");
            mFile.DropDownItems.Add("New Download");
            mFile.DropDownItems.Add("Open Download Folder");
            mFile.DropDownItems.Add("Import URL List");
            mFile.DropDownItems.Add("Export URLs as Text");
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add("Exit");

            var mDownloads = new ToolStripMenuItem("Downloads");
            mDownloads.DropDownItems.Add("Start Download");
            mDownloads.DropDownItems.Add("Pause / Resume");
            mDownloads.DropDownItems.Add("Cancel Download");
            mDownloads.DropDownItems.Add("Clear Completed");
            mDownloads.DropDownItems.Add("Clear Failed");
            mDownloads.DropDownItems.Add("Download Queue Management (View Queue)");

            var mVideoOptions = new ToolStripMenuItem("Video Options");
            mVideoOptions.DropDownItems.Add("Select Video Quality");
            mVideoOptions.DropDownItems.Add("Download Audio Only");
            mVideoOptions.DropDownItems.Add("Convert to MP3");
            mVideoOptions.DropDownItems.Add("Convert to MP4");

            var mTools = new ToolStripMenuItem("Tools");
            mTools.DropDownItems.Add("Bulk Downloader");
            mTools.DropDownItems.Add("Playlist Downloader");

            var mSettings = new ToolStripMenuItem("Settings");
            mSettings.DropDownItems.Add("Download Path");
            mSettings.DropDownItems.Add("Dark / Light Mode");

            var mHelp = new ToolStripMenuItem("Help");
            mHelp.DropDownItems.Add("Documentation");
            mHelp.DropDownItems.Add("Shortcuts");
            mHelp.DropDownItems.Add("About");

            menu.Items.AddRange(new ToolStripItem[]
            {
                mFile, mDownloads, mVideoOptions, mTools, mSettings, mHelp
            });

            return menu;
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
                MessageBox.Show("Please paste a YouTube video URL.", "No URL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAnalyze.Enabled = false;

            var waitingLabel = new Label
            {
                Text = "Please wait... analyzing video.",
                ForeColor = Color.Yellow,
                Location = new Point(14, 550),
                AutoSize = true
            };
            Controls.Add(waitingLabel);
            waitingLabel.BringToFront();
            Application.DoEvents();

            try
            {
                var video = await ytClient.Videos.GetAsync(url);
                var streamManifest = await ytClient.Videos.Streams.GetManifestAsync(video.Id);
                var muxedStreams = streamManifest.GetMuxedStreams()
                    .OrderBy(s => s.VideoQuality.MaxHeight)
                    .ToList();

                if (!muxedStreams.Any())
                {
                    MessageBox.Show("No downloadable streams found.");
                    return;
                }

                // تحقق إذا الفيديو موجود بالفعل
                var existingItem = flowPanel!.Controls
                    .OfType<DownloadItemControl>()
                    .FirstOrDefault(x => x.Title == video.Title);

                if (existingItem != null)
                {
                    MessageBox.Show("This video is already in the download queue.", "Info",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var panel = new Panel
                {
                    Size = new Size(650, 70),
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

                qualityCombo.SelectedIndex = muxedStreams.Count - 1;
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
                    var selected = muxedStreams[qualityCombo.SelectedIndex];

                    var item = new DownloadItemControl(video.Title, selected,
                        destinationFolder, ytClient);

                    item.Size = new Size(600, 0); // start collapsed
                    flowPanel.Controls.Add(item);

                    AnimateItem(item); // animation

                    await item.StartAsync();
                    panel.Visible = false;
                    panel.Dispose();
                };

                panel.Controls.Add(btnDownload);
                flowPanel!.Controls.Add(panel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing video:\n{ex.Message}");
            }
            finally
            {
                btnAnalyze.Enabled = true;
                Controls.Remove(waitingLabel);
            }
        }

        // Animation
        private async void AnimateItem(Control ctrl)
        {
            int h = 0;
            while (h < 90)
            {
                h += 5;
                ctrl.Size = new Size(600, h);
                await Task.Delay(5);
            }
        }
    }
}
