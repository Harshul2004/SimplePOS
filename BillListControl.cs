using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Text.Json;
using System.Linq;

namespace SimplePOS
{
    public partial class BillListControl : UserControl
    {
        private TableLayoutPanel mainTable;
        private ListView billListView;
        private ListView billDetailsView;
        private Label billDetailsLabel;
        private Label grandTotalLabel;
        private Button printButton;
        private Button editButton; // Add Edit Bill button
        private List<Bill> bills = new List<Bill>();
        private int sortColumn = 0; // 0: Bill ID, 1: Amount, 2: Date
        private bool sortAsc = true;

        public BillListControl()
        {
            InitializeBillListUI();
            printButton.Click += PrintButton_Click;
            editButton.Click += EditButton_Click; // Wire up event
        }

        public void SetBills(List<Bill> bills)
        {
            this.bills = bills ?? new List<Bill>();
            SortAndDisplayBills();
        }

        private void SortAndDisplayBills()
        {
            var inrCulture = new CultureInfo("en-IN");
            IEnumerable<Bill> sorted = bills;
            if (sortColumn == 0)
            {
                sorted = bills.OrderBy(b => GetBillIdSortKey(b.BillId));
            }
            else if (sortColumn == 1)
            {
                sorted = bills.OrderBy(b => b.TotalAmount);
            }
            else if (sortColumn == 2)
            {
                sorted = bills.OrderBy(b => ParseDate(b.Date));
            }
            if (!sortAsc)
                sorted = sorted.Reverse();
            billListView.Items.Clear();
            foreach (var bill in sorted)
            {
                billListView.Items.Add(new ListViewItem(new[]
                {
                    bill.BillId,
                    string.Format(inrCulture, "{0:C0}", bill.TotalAmount),
                    bill.Date,
                    bill.Time // Add time to the row
                }));
            }
            billDetailsLabel.Text = "Select a bill to view details";
            billDetailsView.Items.Clear();
            grandTotalLabel.Text = "";
        }

        private (string prefix, int number) GetBillIdSortKey(string billId)
        {
            // Example: BJFF-AA000001 -> (AA, 1)
            if (billId != null && billId.Length >= 13)
            {
                string prefix = billId.Substring(5, 2);
                int.TryParse(billId.Substring(7, 6), out int number);
                return (prefix, number);
            }
            return ("", 0);
        }

        private DateTime ParseDate(string dateStr)
        {
            // Try parse dd/MM/yyyy
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            // Fallback: try parse as-is
            DateTime.TryParse(dateStr, out dt);
            return dt;
        }

        private void InitializeBillListUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            var inrCulture = new CultureInfo("en-IN");

            // Main TableLayoutPanel: 2 columns, 2 rows (bottom row for button)
            mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(20),
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            this.Controls.Add(mainTable);

            // Left: Bill List
            billListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                Font = new Font("Segoe UI", 14F, FontStyle.Regular),
                HideSelection = false,
            };
            billListView.Columns.Add("Bill ID", 240);
            billListView.Columns.Add("Amount", 120);
            billListView.Columns.Add("Date", 180);
            billListView.Columns.Add("Time", 120); // Added Time column
            billListView.SelectedIndexChanged += BillListView_SelectedIndexChanged;
            billListView.ColumnClick += BillListView_ColumnClick;
            mainTable.Controls.Add(billListView, 0, 0);

            // Right: Bill Details
            var detailsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 3,
            };
            detailsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Grand total
            // Bill contents (fills space)
            detailsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detailsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Bill details label
            mainTable.Controls.Add(detailsPanel, 1, 0);

            grandTotalLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 10),
            };
            detailsPanel.Controls.Add(grandTotalLabel, 0, 0);

            billDetailsView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = new Font("Segoe UI", 14F, FontStyle.Regular),
                FullRowSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            billDetailsView.Columns.Add("Item Name", 220);
            billDetailsView.Columns.Add("Qty", 80);
            billDetailsView.Columns.Add("Price", 120);
            billDetailsView.Columns.Add("Total", 120);
            detailsPanel.Controls.Add(billDetailsView, 0, 1);

            billDetailsLabel = new Label
            {
                Text = "Select a bill to view details",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0),
            };
            detailsPanel.Controls.Add(billDetailsLabel, 0, 2);

            // Print Button (bottom, spans both columns)
            printButton = new Button
            {
                Text = "Print Selected Bill",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 255, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Height = 60,
                Margin = new Padding(0, 10, 0, 0),
            };
            printButton.FlatAppearance.BorderSize = 0;

            // Edit Bill Button (beside Print)
            editButton = new Button
            {
                Text = "Edit Bill",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 255, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Height = 60,
                Margin = new Padding(0, 10, 0, 0),
            };
            editButton.FlatAppearance.BorderSize = 0;

            // Add both buttons to the bottom row
            mainTable.Controls.Add(printButton, 0, 1);
            mainTable.Controls.Add(editButton, 1, 1);
            mainTable.SetColumnSpan(printButton, 1);
            mainTable.SetColumnSpan(editButton, 1);
        }

        private void BillListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == sortColumn)
            {
                sortAsc = !sortAsc;
            }
            else
            {
                sortColumn = e.Column;
                sortAsc = true;
            }
            SortAndDisplayBills();
        }

        private void BillListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (billListView.SelectedItems.Count == 0)
            {
                billDetailsLabel.Text = "Select a bill to view details";
                billDetailsView.Items.Clear();
                grandTotalLabel.Text = "";
                return;
            }
            var selected = billListView.SelectedItems[0];
            var bill = bills.Find(b => b.BillId == selected.SubItems[0].Text);
            if (bill == null)
            {
                billDetailsLabel.Text = "Bill not found";
                billDetailsView.Items.Clear();
                grandTotalLabel.Text = "";
                return;
            }
            billDetailsLabel.Text = $"Bill Details - {bill.BillId} ({bill.Date})";
            billDetailsView.Items.Clear();
            double grandTotal = 0;
            var inrCulture = new CultureInfo("en-IN");
            try
            {
                if (!string.IsNullOrWhiteSpace(bill.Items) && bill.Items.TrimStart().StartsWith("["))
                {
                    var items = JsonSerializer.Deserialize<List<BillItem>>(bill.Items);
                    foreach (var item in items)
                    {
                        double total = item.Quantity * item.Price;
                        grandTotal += total;
                        billDetailsView.Items.Add(new ListViewItem(new[]
                        {
                            item.Name,
                            item.Quantity.ToString(),
                            string.Format(inrCulture, "{0:C0}", item.Price),
                            string.Format(inrCulture, "{0:C0}", total)
                        }));
                    }
                }
            }
            catch { /* ignore parse errors */ }
            grandTotalLabel.Text = $"Grand Total: {string.Format(inrCulture, "{0:C0}", grandTotal)}";
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            if (billListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a bill to print.", "No Bill Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selected = billListView.SelectedItems[0];
            var bill = bills.Find(b => b.BillId == selected.SubItems[0].Text);
            if (bill == null)
            {
                MessageBox.Show("Selected bill not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            BillPrinter.PrintBill(bill);
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (billListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a bill to edit.", "No Bill Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selected = billListView.SelectedItems[0];
            var bill = bills.Find(b => b.BillId == selected.SubItems[0].Text);
            if (bill == null)
            {
                MessageBox.Show("Selected bill not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using var editor = new BillEditorForm(bill);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    BillUpdateService.UpdateBillItemsAndTotal(bill.BillId, editor.EditedItems, editor.EditedTotal);
                    MessageBox.Show("Bill updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Refresh bill list
                    bill.Items = JsonSerializer.Serialize(editor.EditedItems);
                    bill.TotalAmount = editor.EditedTotal;
                    SortAndDisplayBills();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to update bill: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    public class BillItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}