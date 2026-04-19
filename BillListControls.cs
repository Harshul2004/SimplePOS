using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace SimplePOS
{
    public partial class BillListControls : Form
    {
        private TableLayoutPanel tableLayoutPanel;
        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private Button btnUp;
        private Button btnDown;
        private int billRowStartIndex = 0;
        private const int rowsPerPage = 8;
        private const int columns = 3;

        // Add this field to store the last selected bill
        private Bill selectedBill = null;
        // Add this field to track bill items scroll position
        private int billItemsStartIndex = 0;

        // Store reference to itemsTable and labels for smooth scrolling
        private TableLayoutPanel itemsTable = null;
        private List<Label[]> itemRowLabels = new List<Label[]>();
        private int lastRowsPerPage = 0;

        // Store reference to bill buttons for smooth scrolling
        private TableLayoutPanel billsGrid = null;
        private List<Button> billIdButtons = new List<Button>();
        private int lastVisibleRows = 0;

        // Cache for latest bills
        private List<Bill> billCache = new List<Bill>();
        private int cachePageIndex = 0;
        private const int cachePageSize = rowsPerPage * columns; // 24

        public BillListControls()
        {
            InitializeComponent();
            LoadBillCache();
            LoadBillsGrid();
        }

        private void LoadBillCache()
        {
            // Get all bills for today
            var todayBills = DBHelper.GetBillsByDate(DateTime.Now.Date)
                .OrderByDescending(b => GetBillIdPrefix(b.BillId))
                .ThenByDescending(b => GetBillIdNumber(b.BillId))
                .ToList();
            // Apply paging in memory
            billCache = todayBills.Skip(cachePageIndex * cachePageSize).Take(cachePageSize).ToList();
            // If cachePageIndex is out of range, reset to0
            if (billCache.Count ==0 && cachePageIndex >0)
            {
                cachePageIndex =0;
                billCache = todayBills.Take(cachePageSize).ToList();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Bill List";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 1400; // Increased width
            this.Height = 800; // Increased height
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 11F);

            // Create TableLayoutPanel
            tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
            };
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F)); // left grid
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F)); // white
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F)); // right grid
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F)); // rightmost
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Panels for each section
            panel1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.LightGray };
            var panelWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            panel2 = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            panel3 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Gainsboro };

            // Add buttons to rightmost column (panel3)
            var rightButtonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            rightButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var btnRightUp = new Button
            {
                Text = "Up",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BackColor = Color.LightGray
            };
            var btnRightDown = new Button
            {
                Text = "Down",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BackColor = Color.LightGray
            };
            var btnRightBack = new Button
            {
                Text = "Back",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 200, 200) // Light red
            };
            var btnRightOk = new Button
            {
                Text = "Ok",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 255, 200) // Light green
            };
            // Wire up new scroll handlers for right buttons
            btnRightUp.Click += BtnRightUp_Click;
            btnRightDown.Click += BtnRightDown_Click;
            btnRightBack.Click += BtnRightBack_Click;
            btnRightOk.Click += BtnRightOk_Click;
            rightButtonsPanel.Controls.Add(btnRightUp, 0, 0);
            rightButtonsPanel.Controls.Add(btnRightDown, 0, 1);
            rightButtonsPanel.Controls.Add(btnRightBack, 0, 2);
            rightButtonsPanel.Controls.Add(btnRightOk, 0, 3);
            panel3.Controls.Add(rightButtonsPanel);

            tableLayoutPanel.Controls.Add(panel1, 0, 0);
            tableLayoutPanel.Controls.Add(panelWhite, 1, 0);
            tableLayoutPanel.Controls.Add(panel2, 2, 0);
            tableLayoutPanel.Controls.Add(panel3, 3, 0);

            // Add Up and Down buttons to the white panel (second column)
            btnUp = new Button
            {
                Text = "Up",
                Height = 120, // double height
                Dock = DockStyle.Top,
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(20, 40, 20, 20)
            };
            btnDown = new Button
            {
                Text = "Down",
                Height = 120, // double height
                Dock = DockStyle.Top,
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(20, 20, 20, 40)
            };
            btnUp.Click += BtnUp_Click;
            btnDown.Click += BtnDown_Click;
            panelWhite.Controls.Add(btnDown); // Add Down first so Up is on top
            panelWhite.Controls.Add(btnUp);

            this.Controls.Add(tableLayoutPanel);
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            if (cachePageIndex >0)
            {
                cachePageIndex--;
                var todayBills = DBHelper.GetBillsByDate(DateTime.Now.Date)
                    .OrderByDescending(b => GetBillIdPrefix(b.BillId))
                    .ThenByDescending(b => GetBillIdNumber(b.BillId))
                    .ToList();
                billCache = todayBills.Skip(cachePageIndex * cachePageSize).Take(cachePageSize).ToList();
                billRowStartIndex =0;
                LoadBillsGrid();
            }
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            cachePageIndex++;
            var todayBills = DBHelper.GetBillsByDate(DateTime.Now.Date)
                .OrderByDescending(b => GetBillIdPrefix(b.BillId))
                .ThenByDescending(b => GetBillIdNumber(b.BillId))
                .ToList();
            var nextPage = todayBills.Skip(cachePageIndex * cachePageSize).Take(cachePageSize).ToList();
            if (nextPage.Count >0)
            {
                billCache = nextPage;
                billRowStartIndex =0;
                LoadBillsGrid();
            }
            else
            {
                cachePageIndex--; // No more pages
            }
        }

        private void LoadBillsGrid()
        {
            var bills = billCache;
            int btnWidth = 180, btnHeight = 80, spacing = 14;
            int totalRows = (int)Math.Ceiling((double)bills.Count / columns);
            int startIdx = billRowStartIndex * columns;
            int endIdx = Math.Min(startIdx + rowsPerPage * columns, bills.Count);
            int visibleRows = Math.Min(rowsPerPage, totalRows - billRowStartIndex);
            lastVisibleRows = visibleRows;

            // Only create grid and buttons once
            if (billsGrid == null || billIdButtons.Count != rowsPerPage * columns)
            {
                panel1.Controls.Clear();
                billsGrid = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = columns,
                    RowCount = rowsPerPage,
                    AutoScroll = false,
                    BackColor = Color.Transparent
                };
                billsGrid.ColumnStyles.Clear();
                billsGrid.RowStyles.Clear();
                for (int i = 0; i < columns; i++)
                    billsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                for (int i = 0; i < rowsPerPage; i++)
                    billsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, btnHeight + spacing));
                billIdButtons.Clear();
                for (int i = 0; i < rowsPerPage * columns; i++)
                {
                    var btn = new Button
                    {
                        Width = btnWidth,
                        Height = btnHeight,
                        Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                        BackColor = Color.White,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(8)
                    };
                    btn.Click += BillButton_Click;
                    billIdButtons.Add(btn);
                    billsGrid.Controls.Add(btn, i % columns, i / columns);
                }
                panel1.Controls.Add(billsGrid);
            }
            // Update button contents for current page
            for (int i = 0; i < billIdButtons.Count; i++)
            {
                int billIdx = startIdx + i;
                if (billIdx < bills.Count)
                {
                    billIdButtons[i].Text = bills[billIdx].BillId;
                    billIdButtons[i].Tag = bills[billIdx];
                    billIdButtons[i].Enabled = true;
                    billIdButtons[i].Visible = true;
                }
                else
                {
                    billIdButtons[i].Text = "";
                    billIdButtons[i].Tag = null;
                    billIdButtons[i].Enabled = false;
                    billIdButtons[i].Visible = false;
                }
            }
            // Enable/disable buttons
            btnUp.Enabled = cachePageIndex > 0;
            btnDown.Enabled = billCache.Count == cachePageSize;
        }

        // Add this event handler
        private void BillButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is Bill bill)
            {
                selectedBill = bill;
                billItemsStartIndex = 0; // Reset scroll position when selecting a new bill
                ShowBillItemsInPanel2(bill);
            }
        }

        // Add this method to display items in panel2
        private void ShowBillItemsInPanel2(Bill bill)
        {
            panel2.Controls.Clear();
            itemsTable = null;
            itemRowLabels.Clear();

            // Create a TableLayoutPanel to split panel2 horizontally
            var splitPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            splitPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 90F)); // Content area
            splitPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F)); // Total area
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Top section: bill items
            var itemsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                AutoScroll = false // No scroll, we handle paging
            };

            // Fixed header
            var headerPanel = new TableLayoutPanel
            {
                Height = 46,
                Dock = DockStyle.Top,
                BackColor = Color.WhiteSmoke,
                ColumnCount = 3,
                RowCount = 1
            };
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F)); // Qty
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F)); // Item
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Price
            headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblQty = new Label
            {
                Text = "Qty",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblItem = new Label
            {
                Text = "Item",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblPrice = new Label
            {
                Text = "Price",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            headerPanel.Controls.Add(lblQty, 0, 0);
            headerPanel.Controls.Add(lblItem, 1, 0);
            headerPanel.Controls.Add(lblPrice, 2, 0);

            itemsPanel.Controls.Add(headerPanel);

            // Parse items JSON safely
            List<BillItem> items = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(bill.Items) && bill.Items.TrimStart().StartsWith("["))
                {
                    items = System.Text.Json.JsonSerializer.Deserialize<List<BillItem>>(bill.Items);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                items = null;
            }

            int rowHeight = 46;
            int availableHeight = panel2.Height - headerPanel.Height - 60; // 60px for total panel and padding
            int rowsPerPage = Math.Max(1, availableHeight / rowHeight);
            lastRowsPerPage = rowsPerPage;

            if (items == null)
            {
                var errorLabel = new Label
                {
                    Text = "Unable to load bill items. Data may be corrupted or missing.",
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 40
                };
                itemsPanel.Controls.Add(errorLabel);
                errorLabel.BringToFront();
            }
            else
            {
                int startIdx = billItemsStartIndex;
                int endIdx = Math.Min(startIdx + rowsPerPage, items.Count);
                int visibleRows = rowsPerPage;
                int itemRows = endIdx - startIdx;

                itemsTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = visibleRows,
                    BackColor = Color.White,
                    AutoScroll = false
                };

                itemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F)); // Qty
                itemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F)); // Item
                itemsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Price

                for (int i = 0; i < visibleRows; i++)
                {
                    itemsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
                    Label lblQtyVal = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16F), TextAlign = ContentAlignment.MiddleLeft };
                    Label lblItemVal = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16F), TextAlign = ContentAlignment.MiddleLeft };
                    Label lblPriceVal = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16F), TextAlign = ContentAlignment.MiddleRight };
                    itemsTable.Controls.Add(lblQtyVal, 0, i);
                    itemsTable.Controls.Add(lblItemVal, 1, i);
                    itemsTable.Controls.Add(lblPriceVal, 2, i);
                    itemRowLabels.Add(new[] { lblQtyVal, lblItemVal, lblPriceVal });
                }
                // Fill initial data
                UpdateBillItemsTable(items);
                itemsPanel.Controls.Add(itemsTable);
                itemsTable.BringToFront();
            }

            // Bottom panel for total amount
            var totalPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(0)
            };
            // Add a TableLayoutPanel for horizontal layout
            var totalTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            totalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Total

            totalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Bill Id
            var lblTotal = new Label
            {
                Text = $"Total: ₹{bill.TotalAmount:0.00}",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(40, 0, 0, 0),
                AutoSize = false
            };
            var lblBillId = new Label
            {
                Text = $"{bill.BillId}",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.Black,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 40, 0),
                AutoSize = false
            };
            totalTable.Controls.Add(lblTotal, 0, 0);
            totalTable.Controls.Add(lblBillId, 1, 0);
            totalPanel.Controls.Add(totalTable);

            splitPanel.Controls.Add(itemsPanel, 0, 0);
            splitPanel.Controls.Add(totalPanel, 0, 1);
            panel2.Controls.Add(splitPanel);
        }

        // Helper class for deserializing bill items
        private class BillItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public double Price { get; set; }
        }

        // Helper to update the items table labels for smooth scrolling
        private void UpdateBillItemsTable(List<BillItem> items)
        {
            int startIdx = billItemsStartIndex;
            int endIdx = Math.Min(startIdx + lastRowsPerPage, items.Count);
            int itemRows = endIdx - startIdx;
            for (int i = 0; i < lastRowsPerPage; i++)
            {
                if (i < itemRows)
                {
                    var item = items[startIdx + i];
                    itemRowLabels[i][0].Text = item.Quantity.ToString();
                    itemRowLabels[i][1].Text = item.Name;
                    itemRowLabels[i][2].Text = $"₹{item.Price:0.00}";
                }
                else
                {
                    itemRowLabels[i][0].Text = "";
                    itemRowLabels[i][1].Text = "";
                    itemRowLabels[i][2].Text = "";
                }
            }
        }

        private List<Bill> GetAllBillsOrdered()
        {
            var bills = new List<Bill>();
            using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={AppDomain.CurrentDomain.BaseDirectory}menu.db;Version=3;"))
            {
                conn.Open();
                using var cmd = new System.Data.SQLite.SQLiteCommand("SELECT * FROM Bills", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bills.Add(new Bill
                    {
                        BillId = reader.GetString(reader.GetOrdinal("BillId")),
                        Date = reader.GetString(reader.GetOrdinal("Date")),
                        Time = reader.GetString(reader.GetOrdinal("Time")),
                        Items = reader.GetString(reader.GetOrdinal("Items")),
                        TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                    });
                }
                conn.Close();
            }
            // Custom sort: prefix descending, then number descending
            bills = bills.OrderByDescending(b => GetBillIdPrefix(b.BillId))
                         .ThenByDescending(b => GetBillIdNumber(b.BillId))
                         .ToList();
            return bills;
        }

        private string GetBillIdPrefix(string billId)
        {
            // Example: BJFF-AA000001 -> AA
            if (billId.Length >= 7)
                return billId.Substring(5, 2);
            return "";
        }

        private int GetBillIdNumber(string billId)
        {
            // Example: BJFF-AA000001 -> 1
            if (billId.Length >= 13 && int.TryParse(billId.Substring(7, 6), out int num))
                return num;
            return 0;
        }

        // Scroll up by 1 row in the bill items list (right panel Up button)
        private void BtnRightUp_Click(object sender, EventArgs e)
        {
            int rowHeight = 46;
            int availableHeight = panel2.Height - 46 - 60;
            int rowsPerPage = Math.Max(1, availableHeight / rowHeight);
            if (selectedBill != null && billItemsStartIndex > 0)
            {
                billItemsStartIndex--;
                // Only update labels, not recreate table
                List<BillItem> items = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(selectedBill.Items) && selectedBill.Items.TrimStart().StartsWith("["))
                    {
                        items = System.Text.Json.JsonSerializer.Deserialize<List<BillItem>>(selectedBill.Items);
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    items = null;
                }
                if (items != null)
                    UpdateBillItemsTable(items);
            }
        }

        // Scroll down by 1 row in the bill items list (right panel Down button)
        private void BtnRightDown_Click(object sender, EventArgs e)
        {
            if (selectedBill != null)
            {
                List<BillItem> items = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(selectedBill.Items) && selectedBill.Items.TrimStart().StartsWith("["))
                    {
                        items = System.Text.Json.JsonSerializer.Deserialize<List<BillItem>>(selectedBill.Items);
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    items = null;
                }
                int rowHeight = 46;
                int availableHeight = panel2.Height - 46 - 60;
                int rowsPerPage = Math.Max(1, availableHeight / rowHeight);
                if (items != null && billItemsStartIndex + rowsPerPage < items.Count)
                {
                    billItemsStartIndex++;
                    UpdateBillItemsTable(items);
                }
            }
        }

        // Add this method to handle Back button click
        private void BtnRightBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public Bill SelectedBill { get; private set; }

        private void BtnRightOk_Click(object sender, EventArgs e)
        {
            if (selectedBill != null)
            {
                SelectedBill = selectedBill;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a bill first.", "No Bill Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}