using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class LabelPrinter
    {
        private const int Dpi = 203;
        private const float LabelWidthMm = 80f;
        private const float LabelHeightMm = 55f;
        private const string ShopName = "Saman Trade Center";

        private static float LabelWidthInches => LabelWidthMm / 25.4f;
        private static float LabelHeightInches => LabelHeightMm / 25.4f;

        public static Bitmap BuildLabelBitmap(PackagingBatchResult batch)
        {
            int width = (int)(LabelWidthInches * Dpi);
            int height = (int)(LabelHeightInches * Dpi);
            Bitmap bitmap = new Bitmap(width, height);
            bitmap.SetResolution(Dpi, Dpi);

            using (Graphics g = Graphics.FromImage(bitmap))
            using (Font shopFont = new Font("Arial", 12, FontStyle.Bold))
            using (Font titleFont = new Font("Arial", 13, FontStyle.Bold))
            using (Font detailsFont = new Font("Arial", 10, FontStyle.Regular))
            using (Font barcodeTextFont = new Font("Arial", 8, FontStyle.Regular))
            using (Font regFont = new Font("Arial", 7, FontStyle.Regular))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.White);

                int margin = 16;
                int y = 10;
                DrawCentered(g, ShopName, shopFont, Brushes.Black, margin, y, width - margin * 2);
                y += 46;

                string title = FitText(g, batch.ProductName ?? string.Empty, titleFont, width - margin * 2);
                DrawCentered(g, title, titleFont, Brushes.Black, margin, y, width - margin * 2);
                y += 46;

                string qty = string.IsNullOrWhiteSpace(batch.PacketSizeLabel) ? "Qty" : batch.PacketSizeLabel;
                string exp = batch.ExpiryDate.HasValue ? batch.ExpiryDate.Value.ToString("yyyy-MM-dd") : "-";
                DrawCentered(g, qty + "    EXP: " + exp, detailsFont, Brushes.Black, margin, y, width - margin * 2);
                y += 34;

                int barcodeWidth = width - margin * 2;
                int barcodeHeight = 150;
                using (Bitmap barcode = BarcodeRenderer.GenerateCode39Barcode(batch.Barcode, barcodeWidth, barcodeHeight))
                {
                    int barcodeX = margin + (barcodeWidth - barcode.Width) / 2;
                    g.DrawImage(barcode, barcodeX, y, barcode.Width, barcodeHeight);
                }
                y += barcodeHeight + 6;

                DrawCentered(g, batch.Barcode ?? string.Empty, barcodeTextFont, Brushes.Black, margin, y, width - margin * 2);
                y += 24;
                DrawCentered(g, batch.BusinessRegistrationText ?? string.Empty, regFont, Brushes.Black, margin, y, width - margin * 2);
            }

            return bitmap;
        }

        public static void ShowPreviewAndPrint(PackagingBatchResult batch)
        {
            using (Bitmap preview = BuildLabelBitmap(batch))
            using (Form form = new Form())
            using (PictureBox picture = new PictureBox())
            using (Button printButton = new Button())
            using (Button closeButton = new Button())
            {
                form.Text = "Label Preview";
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(680, 430);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                picture.Image = new Bitmap(preview);
                picture.SizeMode = PictureBoxSizeMode.Zoom;
                picture.Location = new Point(20, 20);
                picture.Size = new Size(640, 285);
                picture.BorderStyle = BorderStyle.FixedSingle;

                printButton.Text = "Print " + batch.PacketsCreated + " Labels";
                printButton.Location = new Point(330, 330);
                printButton.Size = new Size(160, 48);
                printButton.Click += (sender, args) =>
                {
                    PrintLabels(batch);
                    MessageBox.Show("Labels sent to printer.", "Print Labels", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                closeButton.Text = "Close";
                closeButton.Location = new Point(500, 330);
                closeButton.Size = new Size(120, 48);
                closeButton.Click += (sender, args) => form.Close();

                form.Controls.Add(picture);
                form.Controls.Add(printButton);
                form.Controls.Add(closeButton);
                form.ShowDialog();
            }
        }

        public static void PrintLabels(PackagingBatchResult batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (batch.PacketsCreated <= 0) throw new InvalidOperationException("No labels to print.");

            int printed = 0;
            using (PrintDocument document = new PrintDocument())
            {
                document.DefaultPageSettings.PaperSize = new PaperSize("Packaging Label", ToHundredthsOfInch(LabelWidthMm), ToHundredthsOfInch(LabelHeightMm));
                document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                document.OriginAtMargins = false;
                document.PrintPage += (sender, ev) =>
                {
                    using (Bitmap label = BuildLabelBitmap(batch))
                    {
                        ev.Graphics.DrawImage(label, 0, 0, label.Width, label.Height);
                    }

                    printed++;
                    ev.HasMorePages = printed < batch.PacketsCreated;
                };

                document.Print();
            }
        }

        private static int ToHundredthsOfInch(float millimeters)
        {
            return (int)Math.Round((millimeters / 25.4f) * 100f);
        }

        private static void DrawCentered(Graphics g, string text, Font font, Brush brush, int x, int y, int width)
        {
            SizeF size = g.MeasureString(text, font);
            float drawX = x + Math.Max(0, (width - size.Width) / 2f);
            g.DrawString(text, font, brush, drawX, y);
        }

        private static string FitText(Graphics g, string text, Font font, int maxWidth)
        {
            if (g.MeasureString(text, font).Width <= maxWidth)
            {
                return text;
            }

            string trimmed = text;
            while (trimmed.Length > 3 && g.MeasureString(trimmed + "...", font).Width > maxWidth)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed.Length > 3 ? trimmed + "..." : trimmed;
        }
    }
}
