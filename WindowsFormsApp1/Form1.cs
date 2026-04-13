using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            DatabaseConfig.Load();
            AppCache.Load();
            Task.Run(() =>
            {
                try
                {
                    int synced = FallbackBillLogger.RetryUnsynced();
                    if (synced > 0)
                    {
                        this.Invoke((Action)(() =>
                            MessageBox.Show($"{synced} offline bill(s) successfully synced to cloud.",
                                "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        ));
                    }
                }
                catch { }
            });
            InitializeComponent();
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
            try
            {
                using (MySqlConnection conn = new MySqlConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT isAdmin FROM users WHERE username = @username AND password = @password LIMIT 1";
                    MySqlCommand command = new MySqlCommand(query, conn);

                    command.Parameters.AddWithValue("@username", txtName.Text);
                    command.Parameters.AddWithValue("@password", txtPass.Text);

                    object result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        return false;
                    }
                    isAdmin = Convert.ToInt32(result) == 1;
                    return true;



                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured in the user authentication system. Check the database connection: " + ex.Message);

                return false;
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
