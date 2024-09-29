namespace WindowsFormsApp1
{
    partial class print_window
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTotalPrice = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblSalesperson = new System.Windows.Forms.Label();
            this.btnPrintAgain = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTotalPrice
            // 
            this.lblTotalPrice.AutoSize = true;
            this.lblTotalPrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalPrice.Location = new System.Drawing.Point(363, 124);
            this.lblTotalPrice.Name = "lblTotalPrice";
            this.lblTotalPrice.Size = new System.Drawing.Size(0, 69);
            this.lblTotalPrice.TabIndex = 25;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(297, 143);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(45, 25);
            this.label11.TabIndex = 23;
            this.label11.Text = "Rs. ";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(286, 91);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(56, 25);
            this.label12.TabIndex = 22;
            this.label12.Text = "Total";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(220, 273);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 25);
            this.label1.TabIndex = 26;
            this.label1.Text = "Salesperson";
            // 
            // lblSalesperson
            // 
            this.lblSalesperson.AutoSize = true;
            this.lblSalesperson.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSalesperson.Location = new System.Drawing.Point(379, 273);
            this.lblSalesperson.Name = "lblSalesperson";
            this.lblSalesperson.Size = new System.Drawing.Size(0, 49);
            this.lblSalesperson.TabIndex = 27;
            // 
            // btnPrintAgain
            // 
            this.btnPrintAgain.BackColor = System.Drawing.Color.LimeGreen;
            this.btnPrintAgain.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrintAgain.Location = new System.Drawing.Point(141, 395);
            this.btnPrintAgain.Name = "btnPrintAgain";
            this.btnPrintAgain.Size = new System.Drawing.Size(243, 88);
            this.btnPrintAgain.TabIndex = 28;
            this.btnPrintAgain.Text = "Print";
            this.btnPrintAgain.UseVisualStyleBackColor = false;
            this.btnPrintAgain.Click += new System.EventHandler(this.btnPrintAgain_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(506, 395);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(243, 88);
            this.btnClose.TabIndex = 28;
            this.btnClose.Text = "Exit";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // print_window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(912, 557);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnPrintAgain);
            this.Controls.Add(this.lblSalesperson);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblTotalPrice);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "print_window";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Print Confirmation";
            this.Load += new System.EventHandler(this.print_window_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.Label lblTotalPrice;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblSalesperson;
        private System.Windows.Forms.Button btnPrintAgain;
        private System.Windows.Forms.Button btnClose;
    }
}