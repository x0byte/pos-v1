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
        private string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(validateLogin(txtName.Text, txtPass.Text))
            {
                Home home = new Home();
                home.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Username or Password is incorrect", "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        public bool validateLogin(string username, string password)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(1) FROM users WHERE username = @username AND password = @password";
                    MySqlCommand command = new MySqlCommand(query, conn);

                    command.Parameters.AddWithValue("@username", txtName.Text);
                    command.Parameters.AddWithValue("@password", txtPass.Text);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    return count == 1; //if count == 1 -> return true



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
