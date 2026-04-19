using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Text.Json;
using System.Linq;

namespace SimplePOS
{
    public static class SummaryPrinter
    {
        public static void PrintSummary(List<Bill> bills)
        {
            string configPath = "printer_config.json";
            string printerName = "";
            if (System.IO.File.Exists(configPath))
            {
                var configJson = System.IO.File.ReadAllText(configPath);
                try
                {
                    var config = JsonSerializer.Deserialize<PrinterConfig>(configJson);
                    printerName = config?.CounterPrinterName ?? "";
                }
                catch { }
            }

            if (string.IsNullOrEmpty(printerName))
            {
                System.Windows.Forms.MessageBox.Show("No printer configured. Please set up a printer in Printer Setup.",
                    "Printer Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            try
            {
                double totalSales = 0;
                foreach (var b in bills) totalSales += b.TotalAmount;
                int billCount = bills.Count;
                double avg = billCount > 0 ? totalSales / billCount : 0;

                // Compute top items across all bills
                var itemStats = new Dictionary<string, (int qty, double amt)>();
                foreach (var bill in bills)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(bill.Items) && bill.Items.TrimStart().StartsWith("["))
                        {
                            var items = JsonSerializer.Deserialize<List<BillItem>>(bill.Items);
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    if (!itemStats.ContainsKey(item.Name)) itemStats[item.Name] = (0, 0);
                                    var stat = itemStats[item.Name];
                                    stat.qty += item.Quantity;
                                    stat.amt += item.Quantity * item.Price;
                                    itemStats[item.Name] = stat;
                                }
                            }
                        }
                    }
                    catch { }
                }

                var topItems = itemStats.OrderByDescending(x => x.Value.qty).ThenByDescending(x => x.Value.amt).Take(10).ToList();

                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;

                pd.PrintPage += (s, ev) =>
                {
                    var titleFont = new Font("Consolas", 14F, FontStyle.Bold);
                    var font = new Font("Consolas", 10F, FontStyle.Regular);
                    var boldFont = new Font("Consolas", 10F, FontStyle.Bold);

                    float left = 25;
                    float y = 20;
                    float lineSpacing = font.GetHeight(ev.Graphics) + 4; // dynamic line spacing

                    float pageWidth = ev.PageBounds.Width;
                    float rightPadding = 25;
                    float printableWidth = pageWidth - left - rightPadding;

                    ev.Graphics.DrawString("Sales Report Summary", titleFont, Brushes.Black, left, y);
                    y += lineSpacing * 1.5f;

                    ev.Graphics.DrawString($"Total Sales : {FormatAmount(totalSales)}", font, Brushes.Black, left, y);
                    y += lineSpacing;
                    ev.Graphics.DrawString($"No. of Bills : {billCount}", font, Brushes.Black, left, y);
                    y += lineSpacing;
                    ev.Graphics.DrawString($"Average Bill Value : {FormatAmount(avg)}", font, Brushes.Black, left, y);
                    y += lineSpacing * 1.2f;

                    // --- Top Items ---
                    ev.Graphics.DrawString("Top Items Sold", boldFont, Brushes.Black, left, y);
                    y += lineSpacing * 1.2f;

                    // Column widths based on printable width
                    float descWidth = printableWidth * 0.62f;
                    float qtyWidth = printableWidth * 0.14f;
                    float amtWidth = printableWidth - descWidth - qtyWidth;

                    var leftSf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                    var centerSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    var rightSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };

                    // Header
                    var descRectHeader = new RectangleF(left, y, descWidth, lineSpacing);
                    var qtyRectHeader = new RectangleF(left + descWidth, y, qtyWidth, lineSpacing);
                    var amtRectHeader = new RectangleF(left + descWidth + qtyWidth, y, amtWidth, lineSpacing);

                    ev.Graphics.DrawString("Item Name", boldFont, Brushes.Black, descRectHeader, leftSf);
                    ev.Graphics.DrawString("Qty", boldFont, Brushes.Black, qtyRectHeader, centerSf);
                    ev.Graphics.DrawString("Amount", boldFont, Brushes.Black, amtRectHeader, rightSf);
                    y += lineSpacing;

                    // separator
                    ev.Graphics.DrawLine(Pens.Black, left, y + 2, left + printableWidth, y + 2);
                    y += 4;

                    foreach (var kv in topItems)
                    {
                        string itemName = kv.Key;
                        int qty = kv.Value.qty;
                        double amt = kv.Value.amt;

                        // Measure name height when wrapped
                        int measureWidth = Math.Max(1, (int)Math.Floor(descWidth));
                        var nameSize = ev.Graphics.MeasureString(itemName, font, measureWidth);
                        float itemHeight = Math.Max(lineSpacing, nameSize.Height);

                        var descRect = new RectangleF(left, y, descWidth, itemHeight);
                        var qtyRect = new RectangleF(left + descWidth, y, qtyWidth, itemHeight);
                        var amtRect = new RectangleF(left + descWidth + qtyWidth, y, amtWidth, itemHeight);

                        ev.Graphics.DrawString(itemName, font, Brushes.Black, descRect, leftSf);
                        ev.Graphics.DrawString(qty.ToString(), font, Brushes.Black, qtyRect, centerSf);
                        ev.Graphics.DrawString(FormatAmount(amt), font, Brushes.Black, amtRect, rightSf);

                        y += itemHeight;
                    }

                    y += lineSpacing * 0.5f;
                    ev.Graphics.DrawString("Generated on: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), font, Brushes.Black, left, y);

                    ev.HasMorePages = false;
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Failed to print summary: " + ex.Message,
                    "Print Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private static string FormatAmount(double amt)
        {
            if (Math.Abs(amt - Math.Round(amt)) < 0.0001) return ((long)Math.Round(amt)).ToString();
            return amt.ToString("0.##");
        }
    }
}
