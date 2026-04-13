using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class AppSettings : Form
    {
        private TextBox txtServer;
        private TextBox txtPort;
        private TextBox txtDatabase;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnTestConnection;
        private Button btnSave;
        private Button btnBack;

        public AppSettings()
        {
            InitializeSettingsUi();
            LoadCurrentValues();
        }

        private void InitializeSettingsUi()
        {
            this.Text = "Settings";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.ClientSize = new Size(700, 500);
            this.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);

            Label header = new Label
            {
                Text = "Database Settings",
                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold, GraphicsUnit.Point, 0),
                AutoSize = true,
                Location = new Point(240, 30)
            };
            this.Controls.Add(header);

            AddLabel("Server", 80);
            txtServer = AddTextBox(250, 80, false);

            AddLabel("Port", 130);
            txtPort = AddTextBox(250, 130, false);

            AddLabel("Database Name", 180);
            txtDatabase = AddTextBox(250, 180, false);

            AddLabel("Username", 230);
            txtUsername = AddTextBox(250, 230, false);

            AddLabel("Password", 280);
            txtPassword = AddTextBox(250, 280, true);

            btnTestConnection = new Button
            {
                Text = "Test Connection",
                Size = new Size(150, 45),
                Location = new Point(120, 360)
            };
            btnTestConnection.Click += BtnTestConnection_Click;

            btnSave = new Button
            {
                Text = "Save",
                Size = new Size(150, 45),
                Location = new Point(280, 360)
            };
            btnSave.Click += BtnSave_Click;

            btnBack = new Button
            {
                Text = "Back",
                Size = new Size(150, 45),
                Location = new Point(440, 360)
            };
            btnBack.Click += BtnBack_Click;

            this.Controls.Add(btnTestConnection);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnBack);
        }

        private void LoadCurrentValues()
        {
            var values = DatabaseConfig.ParseCurrent();
            txtServer.Text = values.ContainsKey("server") ? values["server"] : string.Empty;
            txtPort.Text = values.ContainsKey("port") ? values["port"] : string.Empty;
            txtDatabase.Text = values.ContainsKey("database") ? values["database"] : string.Empty;
            txtUsername.Text = values.ContainsKey("uid") ? values["uid"] : string.Empty;
            txtPassword.Text = values.ContainsKey("pwd") ? values["pwd"] : string.Empty;
        }

        private void BtnTestConnection_Click(object sender, EventArgs e)
        {
            string testConnectionString = BuildConnectionStringFromInputs();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(testConnectionString))
                {
                    connection.Open();
                }

                MessageBox.Show("Connection successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                DatabaseConfig.Save(
                    txtServer.Text,
                    txtPort.Text,
                    txtDatabase.Text,
                    txtUsername.Text,
                    txtPassword.Text
                );
                MessageBox.Show("Database settings saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            Home home = new Home(UserSession.IsAdmin);
            home.Show();
            this.Hide();
        }

        private string BuildConnectionStringFromInputs()
        {
            string server = string.IsNullOrWhiteSpace(txtServer.Text) ? "127.0.0.1" : txtServer.Text.Trim();
            string port = txtPort.Text.Trim();
            string database = string.IsNullOrWhiteSpace(txtDatabase.Text) ? "db_stc" : txtDatabase.Text.Trim();
            string uid = string.IsNullOrWhiteSpace(txtUsername.Text) ? "root" : txtUsername.Text.Trim();
            string pwd = txtPassword.Text ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(port))
            {
                return $"server={server};port={port};database={database};uid={uid};pwd={pwd};";
            }

            return $"server={server};database={database};uid={uid};pwd={pwd};";
        }

        private void AddLabel(string text, int y)
        {
            Label label = new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(120, y + 5)
            };
            this.Controls.Add(label);
        }

        private TextBox AddTextBox(int x, int y, bool masked)
        {
            TextBox textBox = new TextBox
            {
                Location = new Point(x, y),
                Width = 300,
                UseSystemPasswordChar = masked
            };
            this.Controls.Add(textBox);
            return textBox;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // AppSettings
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "AppSettings";
            this.ResumeLayout(false);

        }
    }
}
