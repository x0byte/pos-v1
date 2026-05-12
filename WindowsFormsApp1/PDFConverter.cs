using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WindowsFormsApp1;

public class PDFConverter
{
    public void ConvertPrintDocumentToPdf(DataGridView dataGridView, string cashierName, decimal totalAmount, decimal discountAmount, string billCode, DateTime billDateTime)
    {
        // Define the dimensions of the PDF in points (DPI adjusted to 300 DPI)
        float pdfWidthInInches = 2.85f;  // 2.85 inches for width (approximately 285 points)
        int dpi = 300;  // High DPI for quality

        // Convert to pixels for the bitmap size
        int pdfWidthInPixels = (int)(pdfWidthInInches * dpi);
        int rowCount = 0;
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (!row.IsNewRow)
            {
                rowCount++;
            }
        }
        int pdfHeightInPixels = Math.Max(2400, 1750 + (rowCount * 220));

        // Create a high-resolution bitmap using the calculated dimensions
        Bitmap bmp = new Bitmap(pdfWidthInPixels, pdfHeightInPixels);
        bmp.SetResolution(dpi, dpi);  // Set 300 DPI resolution

        // Define margins
        int marginLeft = 10;
        int marginTop = 10;

        // Column widths (adjust to prevent overlap)
        int idColWidth = 30;
        int itemNameColWidth = 250;
        int rateColWidth = 130;
        int qtyColWidth = 100;

        // Create a Graphics object from the Bitmap
        using (Graphics graphics = Graphics.FromImage(bmp))
        {
            // Set up fonts and proper spacing
            Font companyFont = new Font("Arial", 18, FontStyle.Bold);
            Font detailsFont = new Font("Arial", 9);
            Font font = new Font("Arial", 8);
            float fontHeight = font.GetHeight(graphics);

            // Start positions (taking margins into account)
            int startX = marginLeft;
            int startY = marginTop;
            int offsetY = startY + 20;

            // Draw company name, address, and phone number (centered)
            graphics.DrawString("Saman Trade Center", companyFont, Brushes.Black, startX + 25, startY);
            offsetY += companyFont.Height + 50;
            graphics.DrawString("No.20, Matale road, Galewela", detailsFont, Brushes.Black, startX + 120, offsetY);
            offsetY += detailsFont.Height + 35;
            graphics.DrawString("066 22 89 468", detailsFont, Brushes.Black, startX + 250, offsetY);
            offsetY += detailsFont.Height + 60;

            // Draw date, time and cashier name  (bill code moved to barcode at the bottom)
            string date = billDateTime.ToShortDateString();
            string time = billDateTime.ToShortTimeString();
            graphics.DrawString($"Date: {date}  Time: {time}", font, Brushes.Black, startX + 25, offsetY);
            offsetY += (int)fontHeight + 5;
            graphics.DrawString($"Salesperson: {cashierName}", font, Brushes.Black, startX + 25, offsetY);
            offsetY += (int)fontHeight + 20;

            // Draw table headers with specific column widths
            graphics.DrawString("ID", font, Brushes.Black, startX + 20, offsetY);
            graphics.DrawString("Item Name", font, Brushes.Black, startX + idColWidth + 55, offsetY);
            graphics.DrawString("Rate", font, Brushes.Black, startX + idColWidth + itemNameColWidth + 90, offsetY);
            graphics.DrawString("(kg/pcs)", font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + 125, offsetY);
            graphics.DrawString("Price", font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + qtyColWidth + 195, offsetY);
            offsetY += (int)fontHeight + 5;

            // Print each row from DataGridView with fixed column widths and no overlap
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.IsNewRow) continue;

                string id = row.Cells[0].Value.ToString();
                string itemName = row.Cells[1].Value.ToString() + " (Rs." + row.Cells[2].Value.ToString() + ")";
                string quantity = row.Cells[3].Value.ToString();
                string price = row.Cells[4].Value.ToString();

                string[] itemNameLines = SplitText(itemName, graphics, font, itemNameColWidth);
                graphics.DrawString(id, font, Brushes.Black, startX + 25, offsetY);

                int maxOffsetY = offsetY + (itemNameLines.Length * ((int)fontHeight + 2));

                int itemNameOffsetY = offsetY;
                foreach (string line in itemNameLines)
                {
                    graphics.DrawString(line, font, Brushes.Black, startX + idColWidth + 50, itemNameOffsetY);
                    itemNameOffsetY += (int)fontHeight + 2;
                }

                maxOffsetY = Math.Max(maxOffsetY, offsetY);
                offsetY = maxOffsetY - (itemNameLines.Length * ((int)fontHeight + 2));

                decimal ourprice = decimal.Parse(price) / decimal.Parse(quantity);
                graphics.DrawString(ourprice.ToString("N2"), font, Brushes.Black, startX + idColWidth + itemNameColWidth + 80, offsetY);
                graphics.DrawString(quantity, font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + 165, offsetY);
                graphics.DrawString(price, font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + qtyColWidth + 200, offsetY);

                offsetY = maxOffsetY + 5;
            }

            // Print Total, Discount, and Grand Total
            offsetY += 60;
            graphics.DrawString($"Total Rs.: {totalAmount:N2}", font, Brushes.Black, startX + 25, offsetY);
            offsetY += (int)fontHeight + 5;

            Font discount_font = new Font("Arial", 9, FontStyle.Bold);

            if (discountAmount != 0)
            {
                graphics.DrawString($"Discount Rs.: {discountAmount:N2}", discount_font, Brushes.Black, startX + 25, offsetY);
                offsetY += (int)fontHeight + 5;
            }

            Font total_font = new Font("Arial", 11, FontStyle.Bold);
            graphics.DrawString($"Grand Total Rs.: {(totalAmount - discountAmount):N2}", total_font, Brushes.Black, startX + 25, offsetY);

            if (discountAmount > 100)
            {
                // Bigger gap after Grand Total
                offsetY += 70;

                System.Drawing.Rectangle savingsBox = new System.Drawing.Rectangle(
                    startX + 45,
                    offsetY,
                    pdfWidthInPixels - 110,
                    130
                );

                using (Font savingsLabelFont = new Font("Nirmala UI", 9, FontStyle.Bold))
                using (Font savingsAmountFont = new Font("Arial", 10, FontStyle.Bold))
                using (Pen savingsBorderPen = new Pen(Color.Black, 2))
                using (StringFormat centeredFormat = new StringFormat())
                {
                    centeredFormat.Alignment = StringAlignment.Center;
                    centeredFormat.LineAlignment = StringAlignment.Near;

                    graphics.DrawRectangle(savingsBorderPen, savingsBox);

                    graphics.DrawString(
                        "ඔබ අද ඉතිරි කරගත් මුදල:",
                        savingsLabelFont,
                        Brushes.Black,
                        new RectangleF(
                            savingsBox.Left,
                            savingsBox.Top + 20,
                            savingsBox.Width,
                            55
                        ),
                        centeredFormat
                    );

                    graphics.DrawString(
                        "Rs. " + discountAmount.ToString("N2"),
                        savingsAmountFont,
                        Brushes.Black,
                        new RectangleF(
                            savingsBox.Left,
                            savingsBox.Top + 78,
                            savingsBox.Width,
                            40
                        ),
                        centeredFormat
                    );
                }

                offsetY += savingsBox.Height + 10;
            }

            // Draw footnotes
            offsetY += 90;
            graphics.DrawString("Thank you for shopping with us!", discount_font, Brushes.Black, startX + 105, offsetY);
            offsetY += (int)fontHeight + 5;
            graphics.DrawString("Returns accepted within 7 days with receipt.", font, Brushes.Black, startX + 85, offsetY);

            offsetY += 100;
            graphics.DrawString("POS System by BlackBox Technologies", font, Brushes.Black, startX + 105, offsetY);

            // ── Barcode at the bottom ──────────────────────────────────────────────
            offsetY += 40;

            //// Thin separator line before barcode
            //using (Pen separatorPen = new Pen(Color.LightGray, 1))
            //    graphics.DrawLine(separatorPen, startX + 10, offsetY, pdfWidthInPixels - startX - 10, offsetY);
            //offsetY += 15;

            // Generate Code 39 barcode sized to fit the receipt width
            int usableWidth = pdfWidthInPixels - 2 * startX - 20;
            int barcodeHeight = 80;
            using (Bitmap barcodeBmp = GenerateCode39Barcode(billCode, usableWidth, barcodeHeight))
            {
                int barcodeX = startX + 10 + (usableWidth - barcodeBmp.Width) / 2;
                graphics.DrawImage(barcodeBmp, barcodeX, offsetY, barcodeBmp.Width, barcodeHeight);
                offsetY += barcodeHeight + 6;
            }

            // Human-readable bill code centered below the barcode
            Font barcodeTextFont = new Font("Arial", 7, FontStyle.Regular);
            SizeF codeSize = graphics.MeasureString(billCode, barcodeTextFont);
            float codeX = startX + 10 + (usableWidth - codeSize.Width) / 2f;
            graphics.DrawString(billCode, barcodeTextFont, Brushes.Black, codeX, offsetY);
        }

        decimal grand_total = totalAmount - discountAmount;
        print_window pw = new print_window(bmp, grand_total, cashierName);
        pw.ShowDialog();
    }

    // ── Code 39 barcode generator (pure System.Drawing, no dependencies) ─────────
    //
    // Each Code 39 character is 9 elements (5 bars + 4 spaces), alternating
    // bar/space starting with a bar. '0' = narrow, '1' = wide.
    // Wide-to-narrow ratio = 3. Start and stop character is '*' (added automatically).
    //
    private static Bitmap GenerateCode39Barcode(string code, int availableWidth, int barHeight)
    {
        var patterns = new Dictionary<char, string>
        {
            {'0',"000110100"}, {'1',"100100001"}, {'2',"001100001"}, {'3',"101100000"},
            {'4',"000110001"}, {'5',"100110000"}, {'6',"001110000"}, {'7',"000100101"},
            {'8',"100100100"}, {'9',"001100100"}, {'A',"100001001"}, {'B',"001001001"},
            {'C',"101001000"}, {'D',"000011001"}, {'E',"100011000"}, {'F',"001011000"},
            {'G',"000001101"}, {'H',"100001100"}, {'I',"001001100"}, {'J',"000011100"},
            {'K',"100000011"}, {'L',"001000011"}, {'M',"101000010"}, {'N',"000010011"},
            {'O',"100010010"}, {'P',"001010010"}, {'Q',"000000111"}, {'R',"100000110"},
            {'S',"001000110"}, {'T',"000010110"}, {'U',"110000001"}, {'V',"011000001"},
            {'W',"111000000"}, {'X',"010010001"}, {'Y',"110010000"}, {'Z',"011010000"},
            {'-',"010000101"}, {'.',"110000100"}, {' ',"011000100"}, {'*',"010010100"},
            {'$',"010101000"}, {'/',"010100010"}, {'+',"010001010"}, {'%',"000101010"}
        };

        // Collect valid characters (start/stop '*' are added around the payload)
        string raw = "*" + code.ToUpper() + "*";
        var chars = new List<char>();
        foreach (char c in raw)
            if (patterns.ContainsKey(c)) chars.Add(c);

        int n = Math.Max(chars.Count, 2);

        // Total width formula:
        //   Each char has 3 wide + 6 narrow elements  → width = 3*wideW + 6*narrowW = narrowW*(9r+6) where r=3 → 15*narrowW
        //   Inter-character gap = 1 narrow width
        //   Total = n*15*narrowW + (n-1)*narrowW = narrowW*(16n-1)
        // Solve for narrowW:
        int narrowWidth = Math.Max(1, availableWidth / (16 * n - 1));
        int wideWidth = narrowWidth * 3;

        // Compute exact pixel width
        int totalWidth = 0;
        foreach (char c in chars)
        {
            string pat = patterns[c];
            for (int i = 0; i < 9; i++)
                totalWidth += pat[i] == '1' ? wideWidth : narrowWidth;
        }
        totalWidth += (chars.Count - 1) * narrowWidth; // inter-character gaps

        Bitmap bmp = new Bitmap(totalWidth, barHeight);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            int x = 0;
            for (int ci = 0; ci < chars.Count; ci++)
            {
                string pat = patterns[chars[ci]];
                for (int i = 0; i < 9; i++)
                {
                    int w = pat[i] == '1' ? wideWidth : narrowWidth;
                    bool isBar = (i % 2 == 0); // positions 0,2,4,6,8 are bars
                    if (isBar)
                        g.FillRectangle(Brushes.Black, x, 0, w, barHeight);
                    x += w;
                }
                // Inter-character gap (narrow white space)
                if (ci < chars.Count - 1)
                    x += narrowWidth;
            }
        }
        return bmp;
    }

    // Helper method to split text into multiple lines
    private string[] SplitText(string text, Graphics graphics, Font font, int maxWidth)
    {
        List<string> lines = new List<string>();
        string[] words = text.Split(' ');
        StringBuilder currentLine = new StringBuilder();

        foreach (string word in words)
        {
            if (graphics.MeasureString(currentLine + word, font).Width > maxWidth)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }
            currentLine.Append(word + " ");
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines.ToArray();
    }
}
