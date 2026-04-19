using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class SalesTabControl : UserControl
    {
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Button btnSearch;
        private TabControl subTabControl;
        private SummaryControl summaryControl;
        private BillListControl billListControl;
        private List<Bill> currentBills = new List<Bill>();

        public SalesTabControl()
        {
            // No designer, so just call our own initializer
            InitializeSalesTabUI();
        }

        private void InitializeSalesTabUI()
        {
            this.Dock = DockStyle.Fill;

            // Date range selector
            Label lblDate = new Label
            {
                Text = "Date Range:",
                AutoSize = true,
                Left = 10,
                Top = 10,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold)
            };
            this.Controls.Add(lblDate);

            int spacing = 30;
            dtpFrom = new DateTimePicker
            {
                Left = lblDate.Left + lblDate.Width + spacing,
                Top = 7,
                Width = 180,
                Height = 38,
                Font = new Font("Segoe UI", 13F, FontStyle.Regular),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            dtpTo = new DateTimePicker
            {
                Left = dtpFrom.Left + dtpFrom.Width + spacing,
                Top = 7,
                Width = 180,
                Height = 38,
                Font = new Font("Segoe UI", 13F, FontStyle.Regular),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };

            btnSearch = new Button
            {
                Text = "Search",
                Left = dtpTo.Left + dtpTo.Width + spacing,
                Top = 7,
                Width = 120,
                Height = 38,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 255, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += SearchButton_Click;

            // Sub-tabs
            subTabControl = new TabControl
            {
                Left = 0,
                Top = 50,
                Width = this.Width,
                Height = this.Height - 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ItemSize = new Size(150, 40),
                SizeMode = TabSizeMode.Fixed
            };

            // Summary tab
            summaryControl = new SummaryControl { Dock = DockStyle.Fill };
            var summaryTab = new TabPage("Summary") { BackColor = Color.WhiteSmoke };
            summaryTab.Controls.Add(summaryControl);

            // Bill List tab
            billListControl = new BillListControl { Dock = DockStyle.Fill };
            var billListTab = new TabPage("Bill List") { BackColor = Color.WhiteSmoke };
            billListTab.Controls.Add(billListControl);

            subTabControl.TabPages.Add(summaryTab);
            subTabControl.TabPages.Add(billListTab);

            this.Controls.Add(dtpFrom);
            this.Controls.Add(dtpTo);
            this.Controls.Add(btnSearch);
            this.Controls.Add(subTabControl);

            // Set initial date range to today
            dtpFrom.Value = DateTime.Today;
            dtpTo.Value = DateTime.Today;
        }

        // Event handler for Search button click
        private void SearchButton_Click(object sender, EventArgs e)
        {
            // Ensure from <= to
            if (dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                MessageBox.Show("'From' date cannot be after 'To' date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FetchBillsForDateRange();
        }

        // Fetch bills for the selected date range (inclusive)
        private void FetchBillsForDateRange()
        {
            var bills = new List<Bill>();
            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date;
            for (DateTime date = from; date <= to; date = date.AddDays(1))
            {
                bills.AddRange(DBHelper.GetBillsByDate(date));
            }
            currentBills = bills;
            summaryControl.SetBills(currentBills);
            billListControl.SetBills(currentBills);
        }
    }
}
