namespace WindowsFormsApp1
{
    partial class BillDetailView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblBillCodeLabel = new System.Windows.Forms.Label();
            this.lblBillCode = new System.Windows.Forms.Label();
            this.lblDateTimeLabel = new System.Windows.Forms.Label();
            this.lblDateTime = new System.Windows.Forms.Label();
            this.lblSalespersonLabel = new System.Windows.Forms.Label();
            this.lblSalesperson = new System.Windows.Forms.Label();
            this.dataGridItems = new System.Windows.Forms.DataGridView();
            this.lblTotalLabel = new System.Windows.Forms.Label();
            this.lblTotalRs = new System.Windows.Forms.Label();
            this.lblTotal = new System.Windows.Forms.Label();
            this.lblDiscountLabel = new System.Windows.Forms.Label();
            this.lblDiscountRs = new System.Windows.Forms.Label();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.lblGrandTotalLabel = new System.Windows.Forms.Label();
            this.lblGrandTotalRs = new System.Windows.Forms.Label();
            this.lblGrandTotal = new System.Windows.Forms.Label();
            this.btnReprint = new System.Windows.Forms.Button();
            this.btnDeleteBill = new System.Windows.Forms.Button();
            this.btnDeleteAndMoveToBilling = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridItems)).BeginInit();
            this.SuspendLayout();
            // 
            // lblBillCodeLabel
            // 
            this.lblBillCodeLabel.AutoSize = true;
            this.lblBillCodeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBillCodeLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblBillCodeLabel.Location = new System.Drawing.Point(25, 15);
            this.lblBillCodeLabel.Name = "lblBillCodeLabel";
            this.lblBillCodeLabel.Size = new System.Drawing.Size(51, 20);
            this.lblBillCodeLabel.TabIndex = 0;
            this.lblBillCodeLabel.Text = "Bill ID";
            // 
            // lblBillCode
            // 
            this.lblBillCode.AutoSize = true;
            this.lblBillCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBillCode.Location = new System.Drawing.Point(22, 38);
            this.lblBillCode.Name = "lblBillCode";
            this.lblBillCode.Size = new System.Drawing.Size(0, 42);
            this.lblBillCode.TabIndex = 1;
            // 
            // lblDateTimeLabel
            // 
            this.lblDateTimeLabel.AutoSize = true;
            this.lblDateTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTimeLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblDateTimeLabel.Location = new System.Drawing.Point(330, 15);
            this.lblDateTimeLabel.Name = "lblDateTimeLabel";
            this.lblDateTimeLabel.Size = new System.Drawing.Size(103, 20);
            this.lblDateTimeLabel.TabIndex = 2;
            this.lblDateTimeLabel.Text = "Date && Time";
            // 
            // lblDateTime
            // 
            this.lblDateTime.AutoSize = true;
            this.lblDateTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTime.Location = new System.Drawing.Point(328, 42);
            this.lblDateTime.Name = "lblDateTime";
            this.lblDateTime.Size = new System.Drawing.Size(0, 29);
            this.lblDateTime.TabIndex = 3;
            // 
            // lblSalespersonLabel
            // 
            this.lblSalespersonLabel.AutoSize = true;
            this.lblSalespersonLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSalespersonLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblSalespersonLabel.Location = new System.Drawing.Point(700, 15);
            this.lblSalespersonLabel.Name = "lblSalespersonLabel";
            this.lblSalespersonLabel.Size = new System.Drawing.Size(100, 20);
            this.lblSalespersonLabel.TabIndex = 4;
            this.lblSalespersonLabel.Text = "Salesperson";
            // 
            // lblSalesperson
            // 
            this.lblSalesperson.AutoSize = true;
            this.lblSalesperson.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSalesperson.Location = new System.Drawing.Point(698, 42);
            this.lblSalesperson.Name = "lblSalesperson";
            this.lblSalesperson.Size = new System.Drawing.Size(0, 29);
            this.lblSalesperson.TabIndex = 5;
            // 
            // dataGridItems
            // 
            this.dataGridItems.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridItems.Location = new System.Drawing.Point(25, 95);
            this.dataGridItems.Name = "dataGridItems";
            this.dataGridItems.RowHeadersWidth = 30;
            this.dataGridItems.RowTemplate.Height = 32;
            this.dataGridItems.Size = new System.Drawing.Size(940, 400);
            this.dataGridItems.TabIndex = 6;
            this.dataGridItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // lblTotalLabel
            // 
            this.lblTotalLabel.AutoSize = true;
            this.lblTotalLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalLabel.Location = new System.Drawing.Point(25, 510);
            this.lblTotalLabel.Name = "lblTotalLabel";
            this.lblTotalLabel.Size = new System.Drawing.Size(56, 25);
            this.lblTotalLabel.TabIndex = 7;
            this.lblTotalLabel.Text = "Total";
            this.lblTotalLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblTotalRs
            // 
            this.lblTotalRs.AutoSize = true;
            this.lblTotalRs.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalRs.Location = new System.Drawing.Point(25, 540);
            this.lblTotalRs.Name = "lblTotalRs";
            this.lblTotalRs.Size = new System.Drawing.Size(45, 25);
            this.lblTotalRs.TabIndex = 8;
            this.lblTotalRs.Text = "Rs.";
            this.lblTotalRs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblTotal
            // 
            this.lblTotal.AutoSize = true;
            this.lblTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotal.Location = new System.Drawing.Point(75, 535);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(0, 31);
            this.lblTotal.TabIndex = 9;
            this.lblTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblDiscountLabel
            // 
            this.lblDiscountLabel.AutoSize = true;
            this.lblDiscountLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDiscountLabel.Location = new System.Drawing.Point(310, 510);
            this.lblDiscountLabel.Name = "lblDiscountLabel";
            this.lblDiscountLabel.Size = new System.Drawing.Size(95, 25);
            this.lblDiscountLabel.TabIndex = 10;
            this.lblDiscountLabel.Text = "Discount";
            this.lblDiscountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblDiscountRs
            // 
            this.lblDiscountRs.AutoSize = true;
            this.lblDiscountRs.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDiscountRs.Location = new System.Drawing.Point(310, 540);
            this.lblDiscountRs.Name = "lblDiscountRs";
            this.lblDiscountRs.Size = new System.Drawing.Size(45, 25);
            this.lblDiscountRs.TabIndex = 11;
            this.lblDiscountRs.Text = "Rs.";
            this.lblDiscountRs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDiscount.Location = new System.Drawing.Point(360, 537);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(0, 29);
            this.lblDiscount.TabIndex = 12;
            this.lblDiscount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            // 
            // lblGrandTotalLabel
            // 
            this.lblGrandTotalLabel.AutoSize = true;
            this.lblGrandTotalLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGrandTotalLabel.Location = new System.Drawing.Point(600, 508);
            this.lblGrandTotalLabel.Name = "lblGrandTotalLabel";
            this.lblGrandTotalLabel.Size = new System.Drawing.Size(153, 29);
            this.lblGrandTotalLabel.TabIndex = 13;
            this.lblGrandTotalLabel.Text = "Grand Total";
            this.lblGrandTotalLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // lblGrandTotalRs
            // 
            this.lblGrandTotalRs.AutoSize = true;
            this.lblGrandTotalRs.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGrandTotalRs.Location = new System.Drawing.Point(600, 542);
            this.lblGrandTotalRs.Name = "lblGrandTotalRs";
            this.lblGrandTotalRs.Size = new System.Drawing.Size(48, 29);
            this.lblGrandTotalRs.TabIndex = 14;
            this.lblGrandTotalRs.Text = "Rs.";
            this.lblGrandTotalRs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // lblGrandTotal
            // 
            this.lblGrandTotal.AutoSize = true;
            this.lblGrandTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGrandTotal.Location = new System.Drawing.Point(655, 533);
            this.lblGrandTotal.Name = "lblGrandTotal";
            this.lblGrandTotal.Size = new System.Drawing.Size(0, 42);
            this.lblGrandTotal.TabIndex = 15;
            this.lblGrandTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // btnReprint
            // 
            this.btnReprint.BackColor = System.Drawing.Color.LimeGreen;
            this.btnReprint.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReprint.Location = new System.Drawing.Point(25, 600);
            this.btnReprint.Name = "btnReprint";
            this.btnReprint.Size = new System.Drawing.Size(190, 65);
            this.btnReprint.TabIndex = 16;
            this.btnReprint.Text = "Reprint Bill";
            this.btnReprint.UseVisualStyleBackColor = false;
            this.btnReprint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReprint.Click += new System.EventHandler(this.btnReprint_Click);
            // 
            // btnDeleteBill
            // 
            this.btnDeleteBill.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(42)))), ((int)(((byte)(42)))));
            this.btnDeleteBill.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeleteBill.ForeColor = System.Drawing.Color.White;
            this.btnDeleteBill.Location = new System.Drawing.Point(235, 600);
            this.btnDeleteBill.Name = "btnDeleteBill";
            this.btnDeleteBill.Size = new System.Drawing.Size(190, 65);
            this.btnDeleteBill.TabIndex = 18;
            this.btnDeleteBill.Text = "Delete Bill";
            this.btnDeleteBill.UseVisualStyleBackColor = false;
            this.btnDeleteBill.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteBill.Click += new System.EventHandler(this.btnDeleteBill_Click);
            // 
            // btnDeleteAndMoveToBilling
            // 
            this.btnDeleteAndMoveToBilling.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnDeleteAndMoveToBilling.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeleteAndMoveToBilling.ForeColor = System.Drawing.Color.White;
            this.btnDeleteAndMoveToBilling.Location = new System.Drawing.Point(445, 600);
            this.btnDeleteAndMoveToBilling.Name = "btnDeleteAndMoveToBilling";
            this.btnDeleteAndMoveToBilling.Size = new System.Drawing.Size(250, 65);
            this.btnDeleteAndMoveToBilling.TabIndex = 19;
            this.btnDeleteAndMoveToBilling.Text = "Delete && Move to Billing";
            this.btnDeleteAndMoveToBilling.UseVisualStyleBackColor = false;
            this.btnDeleteAndMoveToBilling.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeleteAndMoveToBilling.Click += new System.EventHandler(this.btnDeleteAndMoveToBilling_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(735, 600);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(230, 65);
            this.btnClose.TabIndex = 17;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // BillDetailView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 690);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnDeleteAndMoveToBilling);
            this.Controls.Add(this.btnDeleteBill);
            this.Controls.Add(this.btnReprint);
            this.Controls.Add(this.lblGrandTotal);
            this.Controls.Add(this.lblGrandTotalRs);
            this.Controls.Add(this.lblGrandTotalLabel);
            this.Controls.Add(this.lblDiscount);
            this.Controls.Add(this.lblDiscountRs);
            this.Controls.Add(this.lblDiscountLabel);
            this.Controls.Add(this.lblTotal);
            this.Controls.Add(this.lblTotalRs);
            this.Controls.Add(this.lblTotalLabel);
            this.Controls.Add(this.dataGridItems);
            this.Controls.Add(this.lblSalesperson);
            this.Controls.Add(this.lblSalespersonLabel);
            this.Controls.Add(this.lblDateTime);
            this.Controls.Add(this.lblDateTimeLabel);
            this.Controls.Add(this.lblBillCode);
            this.Controls.Add(this.lblBillCodeLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = true;
            this.MinimumSize = new System.Drawing.Size(860, 620);
            this.MinimizeBox = false;
            this.Name = "BillDetailView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bill Details";
            this.Load += new System.EventHandler(this.BillDetailView_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridItems)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblBillCodeLabel;
        private System.Windows.Forms.Label lblBillCode;
        private System.Windows.Forms.Label lblDateTimeLabel;
        private System.Windows.Forms.Label lblDateTime;
        private System.Windows.Forms.Label lblSalespersonLabel;
        private System.Windows.Forms.Label lblSalesperson;
        private System.Windows.Forms.DataGridView dataGridItems;
        private System.Windows.Forms.Label lblTotalLabel;
        private System.Windows.Forms.Label lblTotalRs;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label lblDiscountLabel;
        private System.Windows.Forms.Label lblDiscountRs;
        private System.Windows.Forms.Label lblDiscount;
        private System.Windows.Forms.Label lblGrandTotalLabel;
        private System.Windows.Forms.Label lblGrandTotalRs;
        private System.Windows.Forms.Label lblGrandTotal;
        private System.Windows.Forms.Button btnReprint;
        private System.Windows.Forms.Button btnDeleteBill;
        private System.Windows.Forms.Button btnDeleteAndMoveToBilling;
        private System.Windows.Forms.Button btnClose;
    }
}
