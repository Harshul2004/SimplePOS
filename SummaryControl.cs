using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class SummaryControl : UserControl
    {
        private TableLayoutPanel mainTable;
        private TableLayoutPanel cardsTable;
        private Panel[] cardPanels = new Panel[4];
        private Label[] cardTitles = new Label[3];
        private Label[] cardValues = new Label[3];
        private Button printReportButton;
        private Panel topItemsPanel;
        private Label topItemsLabel;
        private TableLayoutPanel topItemsTable;
        private readonly CultureInfo inrCulture = new CultureInfo("en-IN");
        private List<Bill> bills = new List<Bill>();

        public SummaryControl()
        {
            InitializeSummaryUI();
            printReportButton.Click += PrintReportButton_Click;
        }

        public void SetBills(List<Bill> bills)
        {
            this.bills = bills ?? new List<Bill>();
            // Update summary cards
            double totalSales = this.bills.Sum(b => b.TotalAmount);
            int billCount = this.bills.Count;
            double avgBill = billCount > 0 ? totalSales / billCount : 0;
            cardValues[0].Text = string.Format(inrCulture, "{0:C0}", totalSales);
            cardValues[1].Text = billCount.ToString();
            cardValues[2].Text = string.Format(inrCulture, "{0:C0}", avgBill);

            // Update top items table
            var itemStats = new Dictionary<string, (int qty, double amt)>();
            foreach (var bill in this.bills)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(bill.Items) && bill.Items.TrimStart().StartsWith("["))
                    {
                        var items = System.Text.Json.JsonSerializer.Deserialize<List<BillListItem>>(bill.Items);
                        foreach (var item in items)
                        {
                            if (!itemStats.ContainsKey(item.Name))
                                itemStats[item.Name] = (0, 0);
                            var stat = itemStats[item.Name];
                            stat.qty += item.Quantity;
                            stat.amt += item.Quantity * item.Price;
                            itemStats[item.Name] = stat;
                        }
                    }
                }
                catch { }
            }
            var topItems = itemStats.OrderByDescending(x => x.Value.qty).ThenByDescending(x => x.Value.amt).Take(10).ToList();
            // Clear old rows except header
            for (int i = topItemsTable.RowCount - 1; i > 0; i--)
            {
                for (int j = 0; j < 3; j++)
                {
                    var ctrl = topItemsTable.GetControlFromPosition(j, i);
                    if (ctrl != null) topItemsTable.Controls.Remove(ctrl);
                }
                topItemsTable.RowStyles.RemoveAt(i);
            }
            topItemsTable.RowCount = 1;
            // Add new rows
            var rowFont = new Font("Segoe UI", 16F, FontStyle.Regular);
            for (int i = 0; i < topItems.Count; i++)
            {
                topItemsTable.RowCount++;
                topItemsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                var (name, stat) = topItems[i];
                topItemsTable.Controls.Add(CreateCellLabel(name, rowFont, Color.Black, Color.White), 0, i + 1);
                topItemsTable.Controls.Add(CreateCellLabel(stat.qty.ToString(), rowFont, Color.Black, Color.White), 1, i + 1);
                topItemsTable.Controls.Add(CreateCellLabel(string.Format(inrCulture, "{0:C0}", stat.amt), rowFont, Color.Black, Color.White), 2, i + 1);
            }
        }

        private void InitializeSummaryUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            // Font for all text
            var baseFont = new Font("Segoe UI", 16F, FontStyle.Bold);
            var valueFont = new Font("Segoe UI", 24F, FontStyle.Bold);
            var buttonFont = new Font("Segoe UI", 20F, FontStyle.Bold);
            var headerFont = new Font("Segoe UI", 17F, FontStyle.Bold);
            var rowFont = new Font("Segoe UI", 16F, FontStyle.Regular);
            var sectionFont = new Font("Segoe UI", 19F, FontStyle.Bold);

            // Main TableLayoutPanel
            mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(20),
            };
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainTable);

            // Cards Table (Row 1)
            cardsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10),
            };
            for (int i = 0; i < 4; i++)
                cardsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            mainTable.Controls.Add(cardsTable, 0, 0);

            // Card Panels
            string[] titles = { "Total Sales", "No. of Bills", "Average Bill Value" };
            Color cardColor = Color.Gainsboro;
            for (int i = 0; i < 4; i++)
            {
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = cardColor,
                    Margin = new Padding(10),
                    Padding = new Padding(15),
                };
                card.Paint += (s, e) => DrawRoundedRectangle(e, card.ClientRectangle, 18, cardColor);
                cardPanels[i] = card;
                cardsTable.Controls.Add(card, i, 0);
            }

            // Card 1-3: Metric Cards (no icons, no sample values)
            for (int i = 0; i < 3; i++)
            {
                var title = new Label
                {
                    Text = titles[i],
                    Dock = DockStyle.Top,
                    Font = baseFont,
                    ForeColor = Color.Black,
                    AutoSize = true,
                    Padding = new Padding(0, 0, 0, 6),
                };
                var value = new Label
                {
                    Text = "-",
                    Dock = DockStyle.Top,
                    Font = valueFont,
                    ForeColor = Color.Black,
                    AutoSize = true,
                };
                cardPanels[i].Controls.Add(value);
                cardPanels[i].Controls.Add(title);
                cardTitles[i] = title;
                cardValues[i] = value;
                title.BringToFront();
                value.BringToFront();
            }

            // Card 4: Print Report Button (even lighter green, black text, larger font)
            printReportButton = new Button
            {
                Text = "Print Report Summary",
                Dock = DockStyle.Fill,
                Font = buttonFont,
                BackColor = Color.FromArgb(200, 255, 200), // Even lighter green
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Height = 80,
                Margin = new Padding(0, 20, 0, 20),
            };
            printReportButton.FlatAppearance.BorderSize = 0;
            printReportButton.Cursor = Cursors.Hand;
            cardPanels[3].Controls.Add(printReportButton);
            printReportButton.BringToFront();

            // Row 2: Top Items Sold Panel
            topItemsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(20, 20, 20, 10),
                Margin = new Padding(0, 10, 0, 0),
                AutoScroll = true,
            };
            mainTable.Controls.Add(topItemsPanel, 0, 1);

            // Top Items Label
            topItemsLabel = new Label
            {
                Text = "Top Items Sold",
                Dock = DockStyle.Top,
                Font = sectionFont,
                ForeColor = Color.Black,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 10),
            };
            topItemsPanel.Controls.Add(topItemsLabel);

            // Table for Top Items (TableLayoutPanel)
            topItemsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1, // Only header row for now
                AutoSize = true,
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Padding = new Padding(0),
                Margin = new Padding(0, 10, 0, 0),
            };
            topItemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topItemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            topItemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Header row
            topItemsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            var headerBack = Color.Gainsboro;
            var headerFore = Color.Black;
            topItemsTable.Controls.Add(CreateCellLabel("Item Name", headerFont, headerFore, headerBack), 0, 0);
            topItemsTable.Controls.Add(CreateCellLabel("Quantity Sold", headerFont, headerFore, headerBack), 1, 0);
            topItemsTable.Controls.Add(CreateCellLabel("Amount", headerFont, headerFore, headerBack), 2, 0);
            // No sample data rows
            topItemsPanel.Controls.Add(topItemsTable);
            topItemsTable.BringToFront();
        }

        // Helper: Create a label for a table cell
        private Label CreateCellLabel(string text, Font font, Color fore, Color back)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = font,
                ForeColor = fore,
                BackColor = back,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Padding = new Padding(8, 0, 0, 0),
            };
        }

        // Helper: Draw rounded rectangle for card panels
        private void DrawRoundedRectangle(PaintEventArgs e, Rectangle bounds, int radius, Color fillColor)
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int d = radius * 2;
                path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
                path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                using (var brush = new SolidBrush(fillColor))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        private void PrintReportButton_Click(object sender, EventArgs e)
        {
            if (bills == null || bills.Count == 0)
            {
                MessageBox.Show("No bills available to print.", "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SummaryPrinter.PrintSummary(bills);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to print summary: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public class BillListItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public double Price { get; set; }
        }
    }
}