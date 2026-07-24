using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DatabaseConfig.Load();
            this.Shown += Form1_Shown;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            this.Shown -= Form1_Shown;
            if (!File.Exists(RuntimePathProvider.GetDataFilePath("config.json")))
            {
                MessageBox.Show(
                    "No database configuration found.\nPlease configure your database connection to continue.",
                    "First Time Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                using (AppSettings settings = new AppSettings(fromLogin: true))
                    settings.ShowDialog();
                DatabaseConfig.Load();
            }

            await Task.Run(() =>
            {
                try
                {
                    AppCache.Load();
                }
                catch (Exception ex)
                {
                    UpdateLogger.Error("Startup cache load failed", ex);
                    BeginInvoke((Action)(() =>
                        MessageBox.Show("The app opened, but inventory cache could not be loaded.\nCheck the database connection and try again.",
                            "Database Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                }
            });

            _ = CreditManager.WarmAccountCacheAsync();
            _ = Task.Run(() =>
            {
                try
                {
                    int synced = FallbackBillLogger.RetryUnsynced();
                    if (synced > 0 && !IsDisposed)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"{synced} offline bill(s) successfully synced to cloud.",
                                "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        ));
                    }
                }
                catch (Exception ex)
                {
                    UpdateLogger.Error("Startup fallback sync failed", ex);
                }
            });
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool isAdmin;
            if (validateLogin(txtName.Text, txtPass.Text, out isAdmin))
            {
                UserSession.IsAdmin = isAdmin;
                UserSession.Username = txtName.Text?.Trim();
                UserSession.IsCashierSessionActive = !isAdmin;
                Home home = new Home(isAdmin);
                home.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Username or Password is incorrect", "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool validateLogin(string username, string password, out bool isAdmin)
        {
            isAdmin = false;
            AuthenticationResult result = AuthenticationService.ValidateLogin(username, password);
            if (result.Success)
            {
                isAdmin = result.IsAdmin;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void BtnDbSettings_Click(object sender, EventArgs e)
        {
            string overridePassword = DatabaseConfig.OverridePassword;
            if (!string.IsNullOrWhiteSpace(overridePassword))
            {
                using (Form prompt = new Form())
                {
                    prompt.Text = "DB Settings";
                    prompt.StartPosition = FormStartPosition.CenterScreen;
                    prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                    prompt.ClientSize = new System.Drawing.Size(320, 130);
                    prompt.MaximizeBox = false;
                    prompt.MinimizeBox = false;

                    Label lbl = new Label { Text = "Enter override password:", AutoSize = true, Location = new System.Drawing.Point(20, 20) };
                    TextBox txt = new TextBox { UseSystemPasswordChar = true, Location = new System.Drawing.Point(20, 45), Width = 280 };
                    Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(130, 85), Width = 80 };
                    Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(220, 85), Width = 80 };

                    prompt.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                    prompt.AcceptButton = ok;
                    prompt.CancelButton = cancel;

                    if (prompt.ShowDialog() != DialogResult.OK || txt.Text != overridePassword)
                    {
                        if (prompt.DialogResult == DialogResult.OK)
                            MessageBox.Show("Incorrect password.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            AppSettings settings = new AppSettings(fromLogin: true);
            settings.Show();
            this.Hide();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
