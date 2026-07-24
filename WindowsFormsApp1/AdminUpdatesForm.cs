using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class AdminUpdatesForm : Form
    {
        private readonly IUpdateService updateService;
        private Label lblCurrentVersion;
        private Label lblFeed;
        private Label lblLatestVersion;
        private Label lblStatus;
        private Button btnCheck;
        private Button btnDownload;
        private Button btnInstall;

        public AdminUpdatesForm()
            : this(CreateUpdateService())
        {
        }

        public AdminUpdatesForm(IUpdateService updateService)
        {
            this.updateService = updateService;
            InitializeUpdatesUi();
            RefreshHeader();
        }

        private static IUpdateService CreateUpdateService()
        {
            IUpdateSafetyService safetyService = new UpdateSafetyService(new DefaultUpdateSafetyStateProvider());
            return new VelopackUpdateService(UpdateConfiguration.Load(), safetyService);
        }

        private void InitializeUpdatesUi()
        {
            Text = "System Updates";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(720, 380);

            Label title = new Label
            {
                Text = "System Updates",
                Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold),
                Location = new Point(24, 20),
                Size = new Size(360, 32)
            };

            lblCurrentVersion = CreateValueLabel(24, 78);
            lblFeed = CreateValueLabel(24, 128);
            lblLatestVersion = CreateValueLabel(24, 178);
            lblStatus = CreateValueLabel(24, 228);
            lblStatus.Size = new Size(660, 52);

            btnCheck = CreateButton("Check for Updates", 24, 305, BtnCheck_Click);
            btnDownload = CreateButton("Download Update", 245, 305, BtnDownload_Click);
            btnInstall = CreateButton("Install and Restart", 466, 305, BtnInstall_Click);

            Controls.Add(title);
            Controls.Add(CreateCaption("Current app version", 24, 58));
            Controls.Add(lblCurrentVersion);
            Controls.Add(CreateCaption("Update feed", 24, 108));
            Controls.Add(lblFeed);
            Controls.Add(CreateCaption("Latest available version", 24, 158));
            Controls.Add(lblLatestVersion);
            Controls.Add(CreateCaption("Status", 24, 208));
            Controls.Add(lblStatus);
            Controls.Add(btnCheck);
            Controls.Add(btnDownload);
            Controls.Add(btnInstall);
        }

        private static Label CreateCaption(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                Location = new Point(x, y),
                Size = new Size(220, 18)
            };
        }

        private static Label CreateValueLabel(int x, int y)
        {
            return new Label
            {
                AutoEllipsis = true,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(x, y),
                Size = new Size(660, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Button CreateButton(string text, int x, int y, EventHandler handler)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(195, 44)
            };
            button.Click += handler;
            return button;
        }

        private void RefreshHeader()
        {
            lblCurrentVersion.Text = updateService.CurrentVersion;
            lblFeed.Text = updateService.IsFeedConfigured ? updateService.FeedUrl : "Update feed is not configured.";
            lblLatestVersion.Text = "Not checked";
            lblStatus.Text = updateService.IsFeedConfigured ? "Ready." : "Update feed is not configured.";
            btnDownload.Enabled = false;
        }

        private async void BtnCheck_Click(object sender, EventArgs e)
        {
            await RunUpdateActionAsync("Checking for updates...", async () =>
            {
                UpdateOperationResult result = await updateService.CheckForUpdatesAsync();
                ApplyResult(result);
                btnDownload.Enabled = result.Success && result.UpdateAvailable;
            });
        }

        private async void BtnDownload_Click(object sender, EventArgs e)
        {
            await RunUpdateActionAsync("Downloading update...", async () =>
            {
                UpdateOperationResult result = await updateService.DownloadUpdateAsync(progress =>
                {
                    BeginInvoke((Action)(() => lblStatus.Text = "Downloading update... " + progress + "%"));
                });
                ApplyResult(result);
            });
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            UpdateOperationResult result = updateService.InstallDownloadedUpdateAndRestart();
            ApplyResult(result);
        }

        private async Task RunUpdateActionAsync(string status, Func<Task> action)
        {
            SetBusy(true, status);
            try
            {
                await action();
            }
            finally
            {
                SetBusy(false, lblStatus.Text);
            }
        }

        private void ApplyResult(UpdateOperationResult result)
        {
            lblStatus.Text = result.Message;
            if (!string.IsNullOrWhiteSpace(result.LatestVersion))
            {
                lblLatestVersion.Text = result.LatestVersion;
            }
        }

        private void SetBusy(bool busy, string status)
        {
            lblStatus.Text = status;
            btnCheck.Enabled = !busy;
            btnDownload.Enabled = !busy && updateService.IsFeedConfigured;
            btnInstall.Enabled = !busy;
        }
    }
}
