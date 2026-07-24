using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class ReturnReceiptPrinter
    {
        public static void ShowReceipt(string returnReference, ReturnRequest request)
        {
            using (Bitmap receipt = BuildReceipt(returnReference, request))
            using (Form form = new Form())
            using (PictureBox picture = new PictureBox())
            using (Button print = new Button())
            using (Button close = new Button())
            {
                form.Text = "Return Receipt - " + returnReference;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(500, 650);

                picture.Image = new Bitmap(receipt);
                picture.SizeMode = PictureBoxSizeMode.Zoom;
                picture.SetBounds(15, 15, 470, 540);

                print.Text = "Print";
                print.SetBounds(255, 575, 100, 40);
                print.Click += (s, e) => PrintReceipt(returnReference, request);

                close.Text = "Close";
                close.SetBounds(370, 575, 100, 40);
                close.Click += (s, e) => form.Close();

                form.Controls.AddRange(new Control[] { picture, print, close });
                form.ShowDialog();
            }
        }

        private static Bitmap BuildReceipt(string returnReference, ReturnRequest request)
        {
            Bitmap bitmap = new Bitmap(850, 1200);
            bitmap.SetResolution(300, 300);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Font title = new Font("Arial", 18, FontStyle.Bold))
            using (Font heading = new Font("Arial", 11, FontStyle.Bold))
            using (Font body = new Font("Arial", 9))
            {
                g.Clear(Color.White);
                int y = 20;
                g.DrawString("Saman Trade Center", title, Brushes.Black, 210, y);
                y += 60;
                g.DrawString("CUSTOMER RETURN RECEIPT", heading, Brushes.Black, 280, y);
                y += 45;
                g.DrawString("Return Ref: " + returnReference, body, Brushes.Black, 30, y);
                y += 30;
                g.DrawString("Linked Bill: " + (request.IsBillLinked ? request.OriginalBillId.ToString() : "No original bill linked"), body, Brushes.Black, 30, y);
                y += 30;
                g.DrawString("Cashier: " + request.Cashier + "  Approved: " + (request.ApprovedBy ?? "-"), body, Brushes.Black, 30, y);
                y += 30;
                g.DrawString("Refund: " + request.RefundMethod + "  Total: Rs. " + request.Items.Sum(x => x.RefundAmount).ToString("N2"), body, Brushes.Black, 30, y);
                y += 45;

                foreach (ReturnItemRequest item in request.Items)
                {
                    g.DrawString(item.ItemName, heading, Brushes.Black, 30, y);
                    y += 28;
                    g.DrawString("Qty: " + item.Quantity.ToString("N4") + "  Refund: Rs. " + item.RefundAmount.ToString("N2") +
                        "  Restocked: " + (item.Restock ? "Yes" : "No"), body, Brushes.Black, 50, y);
                    y += 25;
                    g.DrawString("Condition: " + item.Condition, body, Brushes.Black, 50, y);
                    y += 35;
                }

                y += 25;
                g.DrawString("Reason: " + request.Reason, body, Brushes.Black, 30, y);
            }
            return bitmap;
        }

        private static void PrintReceipt(string returnReference, ReturnRequest request)
        {
            using (Bitmap receipt = BuildReceipt(returnReference, request))
            using (PrintDocument document = new PrintDocument())
            {
                document.PrintPage += (sender, e) => e.Graphics.DrawImage(receipt, 0, 0);
                document.Print();
            }
        }
    }
}
