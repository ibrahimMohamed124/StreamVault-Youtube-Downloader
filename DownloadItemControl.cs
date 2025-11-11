using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace StreamVaultWinForms
{
    public partial class DownloadItemControl : UserControl
    {
        private readonly string _title;
        private readonly MuxedStreamInfo _streamInfo;
        private readonly string _destinationFolder;
        private readonly YoutubeClient _ytClient;
        private CancellationTokenSource? _cts;
        private bool _isPaused = false;

        private Label lblTitle;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnPauseResume;

        public DownloadItemControl(string title, MuxedStreamInfo streamInfo, string destinationFolder, YoutubeClient ytClient)
        {
            _title = title;
            _streamInfo = streamInfo;
            _destinationFolder = destinationFolder;
            _ytClient = ytClient;
            BuildUI();
        }

        private void BuildUI()
        {
            lblTitle = new Label
            {
                Text = _title.Length > 60 ? _title.Substring(0, 57) + "..." : _title,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(10, 10)
            };

            lblStatus = new Label
            {
                Text = "Queued...",
                ForeColor = System.Drawing.Color.Silver,
                AutoSize = true,
                Location = new System.Drawing.Point(10, 35)
            };

            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(10, 60),
                Size = new System.Drawing.Size(500, 12)
            };

            btnPauseResume = new Button
            {
                Text = "Pause",
                BackColor = System.Drawing.Color.MediumSlateBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new System.Drawing.Point(520, 25),
                Size = new System.Drawing.Size(75, 28)
            };
            btnPauseResume.FlatAppearance.BorderSize = 0;
            btnPauseResume.Click += BtnPauseResume_Click;

            BackColor = System.Drawing.Color.FromArgb(45, 45, 65);
            Size = new System.Drawing.Size(610, 90);

            Controls.Add(lblTitle);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnPauseResume);
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

                string fileName = GetSafeFilename($"{_title}.mp4");
                string outputPath = Path.Combine(_destinationFolder, fileName);

                await using var stream = await _ytClient.Videos.Streams.GetAsync(_streamInfo);
                await using var file = File.Create(outputPath);

                byte[] buffer = new byte[16 * 1024];
                long totalBytes = (long)_streamInfo.Size.Bytes;
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
