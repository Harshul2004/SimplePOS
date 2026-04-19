using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SimplePOS
{
    public static class BillPrinter
    {
        public static void PrintBill(Bill bill)
        {
            string configPath = "printer_config.json";
            PrinterConfig config = null;
            string printerName = "";
            if (System.IO.File.Exists(configPath))
            {
                var configJson = System.IO.File.ReadAllText(configPath);
                try
                {
                    config = JsonSerializer.Deserialize<PrinterConfig>(configJson);
                }
                catch { }
                printerName = config?.CounterPrinterName ?? "";
            }

            if (string.IsNullOrEmpty(printerName))
            {
                System.Windows.Forms.MessageBox.Show("No printer configured. Please set up a printer in Printer Setup.",
                    "Printer Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;

                pd.PrintPage += (s, ev) =>
                {
                    var font = new Font("Consolas", 9F, FontStyle.Regular);
                    // use same size for bold header to keep vertical alignment with item rows
                    var fontBold = new Font("Consolas", 9F, FontStyle.Bold);

                    // Left margin for the whole bill (change this value if you want a different left inset)
                    float left = 25; // <<-- LEFT_MARGIN (adjustable)
                    float y = 0;    // top margin
                    float lineSpacing = 10; // reduced for thermal paper
                    float pageWidth = ev.PageBounds.Width;
                    // Right padding to keep text away from the absolute paper edge
                    float rightPadding = 25; // <<-- RIGHT_MARGIN (adjustable)
                    float printableWidth = pageWidth - left - rightPadding;

                    // --- Header Logo (draw before any text) ---
                    // Image path is taken from the printer_config.json 'LogoPath' property saved via Printer Setup UI.
                    // Edit the saved path via the Printer Setup screen. Example file size: 576x200 pixels.
                    string logoPath = config?.LogoPath ?? "logo.png"; // <-- change default if needed
                    if (!string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath))
                    {
                        try
                        {
                            using (var img = Image.FromFile(logoPath))
                            {
                                // Preserve aspect ratio and scale to printable width while keeping clarity
                                float imgAspect = (float)img.Width / img.Height;
                                float targetWidth = printableWidth;
                                float targetHeight = targetWidth / imgAspect;

                                // Center horizontally within printable area
                                float imgLeft = left + (printableWidth - targetWidth) / 2f;

                                // Use high-quality settings for best raster scaling on Windows printer drivers
                                var oldInterpolation = ev.Graphics.InterpolationMode;
                                var oldSmoothing = ev.Graphics.SmoothingMode;
                                var oldPixel = ev.Graphics.PixelOffsetMode;
                                var oldCompositing = ev.Graphics.CompositingQuality;

                                ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                ev.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                ev.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                // Draw image as a graphic (avoids ESC/POS bitmap commands) so it prints as a photo
                                ev.Graphics.DrawImage(img, new RectangleF(imgLeft, y, targetWidth, targetHeight));

                                // Restore previous graphics settings
                                ev.Graphics.InterpolationMode = oldInterpolation;
                                ev.Graphics.SmoothingMode = oldSmoothing;
                                ev.Graphics.PixelOffsetMode = oldPixel;
                                ev.Graphics.CompositingQuality = oldCompositing;

                                // Advance y below image with small spacing
                                y += targetHeight + 8;
                            }
                        }
                        catch { /* ignore image load/draw errors and continue printing text */ }
                    }

                    // --- Bill Metadata ---
                    // Print Bill ID and KOT number on the same line
                    string billIdKotText = $"Bill ID : {bill.BillId}  KOT : {bill.KoTNumber}";
                    ev.Graphics.DrawString(billIdKotText, font, Brushes.Black, left, y);
                    y += lineSpacing;
                    // Print Date and Time on the same row
                    // Adjust spacing between Date and Time here (change value below to move Time closer/further)
                    int dateTimeSpacing = 3; // <<-- DATE_TIME_SPACING (adjustable)
                    string dateTimeRow = $"Date : {bill.Date}{new string(' ', dateTimeSpacing)}Time : {bill.Time}";
                    ev.Graphics.DrawString(dateTimeRow, font, Brushes.Black, left, y);
                    y += lineSpacing;

                    // --- Items Table ---
                    // Draw separator as a solid line across printable width so columns are visually bounded
                    ev.Graphics.DrawLine(Pens.Black, left, y + lineSpacing / 2, left + printableWidth, y + lineSpacing / 2);
                    y += lineSpacing;
                    // Calculate column widths: give more space to Description, and keep Qty and Amount close to the right edge
                    // Adjust columns: reduce description slightly and increase amount column so header fits
                    float descWidth = printableWidth * 0.64f; // description takes ~64%
                    float qtyWidth = printableWidth * 0.14f;  // qty takes ~14%
                    float amtWidth = printableWidth - descWidth - qtyWidth; // remaining for amount (~22%)

                    // Prepare formats
                    // For item names we don't want ellipsis; for headers use ellipsis to avoid clipping single letters
                    var leftSf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.None };
                    var headerSf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                    var centerSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    var rightSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };

                    // Header using same column rectangles
                    var descRectHeader = new RectangleF(left, y, descWidth, lineSpacing + 2);
                    var qtyRectHeader = new RectangleF(left + descWidth, y, qtyWidth, lineSpacing + 2);
                    var amtRectHeader = new RectangleF(left + descWidth + qtyWidth, y, amtWidth, lineSpacing + 2);

                    ev.Graphics.DrawString("Description", fontBold, Brushes.Black, descRectHeader, headerSf);
                    ev.Graphics.DrawString("Qty", fontBold, Brushes.Black, qtyRectHeader, centerSf);
                    ev.Graphics.DrawString("Amount", fontBold, Brushes.Black, amtRectHeader, rightSf);
                    y += lineSpacing;
                    // draw separator under header
                    ev.Graphics.DrawLine(Pens.Black, left, y + lineSpacing / 2, left + printableWidth, y + lineSpacing / 2);
                    y += lineSpacing;

                    var items = JsonSerializer.Deserialize<System.Collections.Generic.List<BillItem>>(bill.Items);
                    foreach (var item in items)
                    {
                        string amountStr = FormatAmount(item.Price * item.Quantity);

                        // Measure name with wrapping within descWidth
                        int measureWidth = Math.Max(1, (int)Math.Floor(descWidth));
                        var nameSize = ev.Graphics.MeasureString(item.Name, font, measureWidth);
                        float itemHeight = Math.Max(lineSpacing, nameSize.Height);

                        // Column rectangles with dynamic height to accommodate wrapped name
                        var descRect = new RectangleF(left, y, descWidth, itemHeight);
                        var qtyRect = new RectangleF(left + descWidth, y, qtyWidth, itemHeight);
                        var amtRect = new RectangleF(left + descWidth + qtyWidth, y, amtWidth, itemHeight);

                        // Draw wrapped name (DrawString will wrap inside the rectangle)
                        ev.Graphics.DrawString(item.Name, font, Brushes.Black, descRect, leftSf);

                        // Draw qty and amount aligned to top of the item's rect
                        ev.Graphics.DrawString(item.Quantity.ToString(), font, Brushes.Black, qtyRect, centerSf);
                        ev.Graphics.DrawString(amountStr, font, Brushes.Black, amtRect, rightSf);

                        // Advance by the measured item height (no extra padding) to avoid large gaps between wrapped lines
                        y += itemHeight;
                    }

                    // --- Table Footer ---
                    // draw separator under footer
                    ev.Graphics.DrawLine(Pens.Black, left, y + lineSpacing / 2, left + printableWidth, y + lineSpacing / 2);
                    y += lineSpacing;

                    // --- Totals Section ---
                    string totalLabel = "Total";
                    string totalAmt = FormatAmount(bill.TotalAmount);
                    // Use a larger bold font for totals (increased by 2 points)
                    using (var fontTotal = new Font("Consolas", 16F, FontStyle.Bold))
                    {
                        // Determine height from font to avoid clipping
                        float totalFontHeight = fontTotal.GetHeight(ev.Graphics);

                        // Draw label on the left (respecting left margin)
                        ev.Graphics.DrawString(totalLabel, fontTotal, Brushes.Black, left, y);

                        // Draw amount right-aligned but stay inside printable area (not at the absolute edge)
                        var amtSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
                        RectangleF amtRect = new RectangleF(left, y, printableWidth, totalFontHeight + 4);
                        ev.Graphics.DrawString(totalAmt, fontTotal, Brushes.Black, amtRect, amtSf);

                        // Reduce extra gap: advance by font height plus a small padding
                        y += totalFontHeight + 2;
                    }
                    // draw separator after totals
                    ev.Graphics.DrawLine(Pens.Black, left, y + lineSpacing / 2, left + printableWidth, y + lineSpacing / 2);
                    y += lineSpacing;

                    // --- Footer ---
                    string footer = "Thanks For Visiting";
                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    RectangleF footerRect = new RectangleF(left, y, printableWidth, lineSpacing + 4);
                    ev.Graphics.DrawString(footer, fontBold, Brushes.Black, footerRect, sf);
                    y += lineSpacing + 4;

                    // Stop printing here
                    ev.HasMorePages = false;

                    // Optional: cut paper for ESC/POS compatible printers
                    RawPrinterHelper.SendStringToPrinter(printerName, "\x1B\x69"); // full cut command
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Failed to print bill: " + ex.Message,
                    "Print Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        // --- Helper methods ---
        private class BillItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public double Price { get; set; }
        }

        static string PadRight(string text, int width) => text.Length >= width ? text.Substring(0, width) : text.PadRight(width);
        static string PadLeft(object text, int width) => text.ToString().Length >= width ? text.ToString().Substring(0, width) : text.ToString().PadLeft(width);
        private static string FormatAmount(double amt)
        {
            // If the amount is effectively an integer, drop decimal places
            if (Math.Abs(amt - Math.Round(amt)) < 0.0001)
                return ((long)Math.Round(amt)).ToString();
            // Otherwise show up to two decimals without trailing zeros
            return amt.ToString("0.##");
        }
        static string PadCenter(string text, int width)
        {
            if (text.Length >= width) return text.Substring(0, width);
            int left = (width - text.Length) / 2;
            int right = width - text.Length - left;
            return new string(' ', left) + text + new string(' ', right);
        }
    }

    // --- Raw printer helper for sending ESC/POS commands ---
    public class RawPrinterHelper
    {
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter")]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA")]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, IntPtr pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter")]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter")]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter")]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter")]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string szPrinterName, byte[] bytes)
        {
            IntPtr hPrinter;
            if (!OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero)) return false;

            StartDocPrinter(hPrinter, 1, IntPtr.Zero);
            StartPagePrinter(hPrinter);

            IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);
            WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out int written);
            Marshal.FreeCoTaskMem(unmanagedBytes);

            EndPagePrinter(hPrinter); // Pass hPrinter as argument
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);

            return true;
        }

        public static void SendStringToPrinter(string szPrinterName, string szString)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(szString);
            SendBytesToPrinter(szPrinterName, bytes);
        }
    }
}
