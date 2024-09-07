using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.IO.Image;
using iText.Kernel.Geom;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

public class PDFConverter
{
    public void ConvertPrintDocumentToPdf(DataGridView dataGridView, string cashierName, decimal totalAmount, decimal discountedAmount)
    {
        // Define the dimensions of the PDF in points (DPI adjusted to 300 DPI)
        float pdfWidthInInches = 2.85f;  // 2.85 inches for width (approximately 285 points)
        float pdfHeightInInches = 50f;  // Height for standard A4 or adjusted size
        int dpi = 300;  // High DPI for quality

        // Convert to pixels for the bitmap size
        int pdfWidthInPixels = (int)(pdfWidthInInches * dpi);
        int pdfHeightInPixels = (int)(pdfHeightInInches * dpi);

        // Create a high-resolution bitmap using the calculated dimensions
        Bitmap bmp = new Bitmap(pdfWidthInPixels, pdfHeightInPixels);
        bmp.SetResolution(dpi, dpi);  // Set 300 DPI resolution

        // Define margins
        int marginLeft = 10;
        int marginTop = 10;

        // Column widths (adjust to prevent overlap)
        int idColWidth = 30;  // Column width for ID
        int itemNameColWidth = 250;  // Column width for Item Name
        int rateColWidth = 130;  // Column width for Rate
        int qtyColWidth = 100;  // Column width for Quantity
        //int priceColWidth = 50;  // Column width for Price

        // Create a Graphics object from the Bitmap
        using (Graphics graphics = Graphics.FromImage(bmp))
        {
            // Set up fonts and proper spacing
            Font companyFont = new Font("Arial", 18, FontStyle.Bold);
            Font detailsFont = new Font("Arial", 9);
            Font font = new Font("Arial", 8);  // Font for the item details
            float fontHeight = font.GetHeight(graphics);  // Correctly calculate font height for proper line spacing

            // Start positions (taking margins into account)
            int startX = marginLeft;  // Add margin on the left
            int startY = marginTop;
            int offsetY = startY + 20;

            // Draw company name, address, and phone number (centered)
            graphics.DrawString("Saman Trade Center", companyFont, Brushes.Black, startX + 25, startY);
            offsetY += companyFont.Height + 50;
            graphics.DrawString("No.20, Matale road, Galewela", detailsFont, Brushes.Black, startX + 120, offsetY);
            offsetY += detailsFont.Height + 35;
            graphics.DrawString("066 22 89 468", detailsFont, Brushes.Black, startX + 250, offsetY);
            offsetY += detailsFont.Height + 60;

            // Draw date, time, and cashier name (centered)
            string date = DateTime.Now.ToShortDateString();
            string time = DateTime.Now.ToShortTimeString();
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

                // Print item details with aligned columns and no overlap
                string[] itemNameLines = SplitText(itemName, graphics, font, itemNameColWidth);
                graphics.DrawString(id, font, Brushes.Black, startX + 25, offsetY);

                // Calculate the maximum height of the row (based on the item name's number of lines)
                int maxOffsetY = offsetY + (itemNameLines.Length * ((int)fontHeight + 2));

                // Draw the item name
                int itemNameOffsetY = offsetY;  // To keep track of the starting position for other columns
                foreach (string line in itemNameLines)
                {
                    graphics.DrawString(line, font, Brushes.Black, startX + idColWidth + 50, itemNameOffsetY);
                    itemNameOffsetY += (int)fontHeight + 2;
                }


                // After printing all lines of Item Name, set maxOffsetY for the other columns
                maxOffsetY = Math.Max(maxOffsetY, offsetY);

                // Print Rate, Quantity, and Price aligned with the first line of item name
                offsetY = maxOffsetY - (itemNameLines.Length * ((int)fontHeight + 2)); // Reset to align with the first line

                // Print Rate
                decimal ourprice = decimal.Parse(price) / decimal.Parse(quantity);

                graphics.DrawString(ourprice.ToString("N2"), font, Brushes.Black, startX + idColWidth + itemNameColWidth + 80, offsetY);
                graphics.DrawString(quantity, font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + 165, offsetY);
                graphics.DrawString(price, font, Brushes.Black, startX + idColWidth + itemNameColWidth + rateColWidth + qtyColWidth + 200, offsetY);

                // Move to the next row
                offsetY = maxOffsetY + 5;
            }

            // Print Total, Discount, and Grand Total
            offsetY += 60;
            graphics.DrawString($"Total Rs.: {totalAmount:N2}", font, Brushes.Black, startX + 25, offsetY);
            offsetY += (int)fontHeight + 5;

            Font discount_font = new Font("Arial", 9, FontStyle.Bold);

            if (discountedAmount != 0)
            {
                graphics.DrawString($"Discount Rs.: {discountedAmount:N2}", discount_font, Brushes.Black, startX + 25, offsetY);
                offsetY += (int)fontHeight + 5;
            }

            Font total_font = new Font("Arial", 11, FontStyle.Bold);
            graphics.DrawString($"Grand Total Rs.: {(totalAmount - discountedAmount):N2}", total_font, Brushes.Black, startX + 25, offsetY);

            // Draw footnotes
            offsetY += 90;
            graphics.DrawString("Thank you for shopping with us!", discount_font, Brushes.Black, startX + 105, offsetY);
            offsetY += (int)fontHeight + 5;
            graphics.DrawString("Returns accepted within 7 days with receipt.", font, Brushes.Black, startX + 85, offsetY);

            offsetY += 100;
            string software_name = "POS System by BlackBox Technologies";
            string software_contact = "070 1371 880";

            graphics.DrawString(software_name, font, Brushes.Black, startX + 105, offsetY);
            offsetY += (int)fontHeight + 5;
            graphics.DrawString(software_contact, font, Brushes.Black, startX + 300, offsetY);


        }

        // Convert the bitmap to an iText ImageData object
        using (MemoryStream memoryStream = new MemoryStream())
        {
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            ImageData imageData = ImageDataFactory.Create(memoryStream.ToArray());


            // Now, send the bitmap to the printer
            PrintDocument printDocument = new PrintDocument();

            printDocument.PrintPage += (sender, e) =>
            {
                e.Graphics.DrawImage(bmp, 0, 0);  // Draw the bitmap to the printer graphics
            };

            // Print the document
            printDocument.Print();

            /*

            // Create a PDF document using iText
            using (PdfWriter writer = new PdfWriter(pdfFilePath))
            {
                PdfDocument pdfDoc = new PdfDocument(writer);
                Document document = new Document(pdfDoc, new PageSize(pdfWidthInInches * 72, pdfHeightInInches * 72));  // Correct size in points

                // Remove margins from the PDF document
                document.SetMargins(0, 0, 0, 0);

                // Create an iText Image object from the ImageData
                iText.Layout.Element.Image pdfImage = new iText.Layout.Element.Image(imageData);

                // Add the image to the PDF document
                document.Add(pdfImage);

                // Close the document
                document.Close();
            }*/
        }

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
