using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text.Json;

namespace SimplePOS
{
    // Simple KOT (Kitchen Order Ticket) printer helper
    // This file provides a sample layout for KOT printing and a helper method
    // to print a KOT. The printing logic is intentionally simple; item and
    // KOT-number generation logic will be added later as you requested.
    public static class KoTPrinter
    {
        // Public method to print a KoT. Expects a KoT model (below).
        // Optional parameter forcedPrinterName allows callers to send the KOT to a specific printer
        // without changing the saved configuration file.
        public static void PrintKoT(KoT kot, string forcedPrinterName = null)
        {
            string configPath = "printer_config.json";
            string printerName = "";

            if (!string.IsNullOrEmpty(forcedPrinterName))
            {
                printerName = forcedPrinterName;
            }
            else
            {
                if (System.IO.File.Exists(configPath))
                {
                    try
                    {
                        var configJson = System.IO.File.ReadAllText(configPath);
                        var config = JsonSerializer.Deserialize<PrinterConfig>(configJson);
                        // Use KitchenPrinterName from config
                        printerName = config?.KitchenPrinterName ?? "";
                    }
                    catch
                    {
                        // ignore parse errors and leave printerName empty
                    }
                }
            }

            if (string.IsNullOrEmpty(printerName))
            {
                System.Windows.Forms.MessageBox.Show("No kitchen printer configured. Please set it in Printer Setup.",
                    "Printer Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;

                pd.PrintPage += (s, ev) =>
                {
                    var font = new Font("Consolas", 10F, FontStyle.Regular);
                    var fontBold = new Font("Consolas", 12F, FontStyle.Bold);
                    float left = 10; // left margin (small)
                    float y = 0;
                    float lineHeight = font.GetHeight(ev.Graphics) + 4;
                    float pageWidth = ev.PageBounds.Width;
                    float rightPadding = 10;
                    float printableWidth = pageWidth - left - rightPadding;

                    // measure an approximate character width and shift certain right-aligned fields to the left
                    float charWidth = ev.Graphics.MeasureString("W", font).Width;
                    float shiftLeft = charWidth * 4f; // move KOT#, Qty and quantities left by ~4 characters
                    float qtyWidth = 40f; // reserved width for quantity column

                    // Header: KOT centered
                    var headerSf = new StringFormat { Alignment = StringAlignment.Center };
                    RectangleF headerRect = new RectangleF(left, y, printableWidth, lineHeight + 4);
                    ev.Graphics.DrawString("KITCHEN ORDER TICKET", fontBold, Brushes.Black, headerRect, headerSf);
                    y += lineHeight + 6;

                    // Time and KOT No on one row
                    string timeText = $"Time: {kot.Time}";
                    string kotText = $"KOT#: {kot.KoTNumber}";

                    // Draw time at left and KOT# at right within printable area but shifted left slightly
                    var leftSf = new StringFormat { Alignment = StringAlignment.Near };
                    var rightSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                    RectangleF timeRect = new RectangleF(left, y, printableWidth - shiftLeft, lineHeight);
                    ev.Graphics.DrawString(timeText, font, Brushes.Black, timeRect, leftSf);
                    ev.Graphics.DrawString(kotText, font, Brushes.Black, timeRect, rightSf);
                    y += lineHeight;

                    // Small separator
                    ev.Graphics.DrawLine(Pens.Black, left, y + (lineHeight / 2), left + printableWidth, y + (lineHeight / 2));
                    y += lineHeight / 2 + 2;

                    // Items header
                    ev.Graphics.DrawString("Item", fontBold, Brushes.Black, left, y);
                    // Qty header near right but shifted left
                    RectangleF qtyHeaderRect = new RectangleF(left + printableWidth - qtyWidth - shiftLeft, y, qtyWidth, lineHeight);
                    ev.Graphics.DrawString("Qty", fontBold, Brushes.Black, qtyHeaderRect, rightSf);
                    y += lineHeight;

                    // Items list (expecting JSON array of objects with Name and Quantity)
                    try
                    {
                        var items = JsonSerializer.Deserialize<System.Collections.Generic.List<KoTItem>>(kot.Items);
                        if (items != null)
                        {
                            foreach (var it in items)
                            {
                                // Compute name area width allowing for qty column and shift
                                float nameAreaWidth = printableWidth - qtyWidth - shiftLeft;
                                RectangleF nameRect = new RectangleF(left, y, nameAreaWidth, lineHeight);

                                // Measure required height for the item name when wrapped within nameAreaWidth
                                var measured = ev.Graphics.MeasureString(it.Name ?? string.Empty, font, new SizeF(nameAreaWidth, float.MaxValue));
                                float nameHeight = Math.Max(lineHeight, measured.Height);

                                // Draw name (wrapped)
                                var wrapSf = new StringFormat { Alignment = StringAlignment.Near };
                                RectangleF nameDrawRect = new RectangleF(nameRect.X, nameRect.Y, nameRect.Width, nameHeight);
                                ev.Graphics.DrawString(it.Name ?? string.Empty, font, Brushes.Black, nameDrawRect, wrapSf);

                                // Draw qty in its column, vertically centered relative to name height
                                RectangleF qtyRect = new RectangleF(left + printableWidth - qtyWidth - shiftLeft, y, qtyWidth, nameHeight);
                                var qtySf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                                ev.Graphics.DrawString(it.Quantity.ToString(), font, Brushes.Black, qtyRect, qtySf);

                                // Advance y by the height used
                                y += nameHeight;
                            }
                        }
                        else
                        {
                            // If items are not present or empty, show the raw string
                            ev.Graphics.DrawString(kot.Items ?? string.Empty, font, Brushes.Black, left, y);
                            y += lineHeight;
                        }
                    }
                    catch
                    {
                        // On error, show raw items text
                        ev.Graphics.DrawString(kot.Items ?? string.Empty, font, Brushes.Black, left, y);
                        y += lineHeight;
                    }

                    // Separator before instructions
                    ev.Graphics.DrawLine(Pens.Black, left, y + (lineHeight / 2), left + printableWidth, y + (lineHeight / 2));
                    y += lineHeight / 2 + 4;

                    // Instructions
                    ev.Graphics.DrawString("Instructions:", fontBold, Brushes.Black, left, y);
                    y += lineHeight;
                    if (!string.IsNullOrWhiteSpace(kot.Instructions))
                    {
                        // Wrap instructions
                        var instrRect = new RectangleF(left, y, printableWidth, lineHeight * 4);
                        var wrapSf = new StringFormat { Alignment = StringAlignment.Near };
                        ev.Graphics.DrawString(kot.Instructions, font, Brushes.Black, instrRect, wrapSf);
                        y += lineHeight * 3;
                    }

                    // End
                    ev.HasMorePages = false;

                    // Optional: cut paper for ESC/POS compatible printers
                    RawPrinterHelper.SendStringToPrinter(printerName, "\x1B\x69");
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Failed to print KOT: " + ex.Message,
                    "Print Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        // Simple KoT model used by the printer; your application may create/populate this
        public class KoT
        {
            public string Time { get; set; }
            public string Items { get; set; } // JSON serialized list of KoTItem
            public string KoTNumber { get; set; }
            public string Instructions { get; set; }
        }

        // Simple item representation for KOT
        private class KoTItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
        }
    }
}
