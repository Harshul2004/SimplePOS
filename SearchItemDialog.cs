using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class SearchItemDialog : Form
    {
        private List<(int ID, string Name, double Price)> menuItems;
        public (int ID, string Name, double Price)? SelectedItem { get; private set; }

        private TextBox txtSearch;
        private ListBox lstResults;
        private Button btnOK;
        private Button btnCancel;
        private Button btnUp;
        private Button btnDown;

        public SearchItemDialog(List<(int ID, string Name, double Price)> allMenuItems)
        {
            menuItems = allMenuItems;
            //InitializeComponent();
            InitializeCustom();
        }

        // Add a constructor that accepts initial search text
        public SearchItemDialog(List<(int ID, string Name, double Price)> allMenuItems, string initialSearch)
        {
            menuItems = allMenuItems;
            InitializeCustom();
            txtSearch.Text = initialSearch ?? string.Empty;
            // Trigger filtering on load
            TxtSearch_TextChanged(txtSearch, EventArgs.Empty);
        }

        private void InitializeCustom()
        {
            int padding = 32;
            int btnCount = 4;
            int spacing = 8;
            int btnRowHeight = 80;
            int listWidth = 500, listHeight = 400;
            int txtWidth = listWidth, txtHeight = 60;
            int dialogWidth = listWidth + 2 * padding;
            int btnWidth = (listWidth - (spacing * (btnCount - 1))) / btnCount;
            int dialogHeight = padding + txtHeight + spacing + listHeight + spacing + btnRowHeight + padding;

            txtSearch = new TextBox {
                Left = padding,
                Top = padding,
                Width = txtWidth,
                Height = txtHeight,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold)
            };
            lstResults = new ListBox {
                Left = padding,
                Top = txtSearch.Bottom + spacing,
                Width = listWidth,
                Height = listHeight,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ItemHeight = 48
            };

            int btnRowY = lstResults.Bottom + spacing;
            int btnLeftStart = padding;

            btnUp = new Button {
                Text = "Up",
                Left = btnLeftStart,
                Top = btnRowY,
                Width = btnWidth,
                Height = btnRowHeight,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGray,
                ForeColor = System.Drawing.Color.Black
            };
            btnDown = new Button {
                Text = "Down",
                Left = btnUp.Right + spacing,
                Top = btnRowY,
                Width = btnWidth,
                Height = btnRowHeight,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGray,
                ForeColor = System.Drawing.Color.Black
            };
            btnCancel = new Button {
                Text = "Cancel",
                Left = btnDown.Right + spacing,
                Top = btnRowY,
                Width = btnWidth,
                Height = btnRowHeight,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(255, 204, 204), // Light red
                ForeColor = System.Drawing.Color.Black
            };
            btnOK = new Button {
                Text = "OK",
                Left = btnCancel.Right + spacing,
                Top = btnRowY,
                Width = btnWidth,
                Height = btnRowHeight,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(204, 255, 204), // Light green
                ForeColor = System.Drawing.Color.Black
            };

            txtSearch.TextChanged += TxtSearch_TextChanged;
            lstResults.DoubleClick += LstResults_DoubleClick;
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            btnUp.Click += BtnUp_Click;
            btnDown.Click += BtnDown_Click;

            this.Controls.Add(txtSearch);
            this.Controls.Add(lstResults);
            this.Controls.Add(btnUp);
            this.Controls.Add(btnDown);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOK);

            this.Text = "Search Item";
            this.ClientSize = new System.Drawing.Size(dialogWidth, dialogHeight);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string search = txtSearch.Text.Trim().ToLower();
            var results = menuItems
                .Where(item => item.Name.ToLower().Contains(search))
                .Select(item => $"{item.Name} - ₹{item.Price:0.##}")
                .ToList();

            lstResults.Items.Clear();
            lstResults.Items.AddRange(results.ToArray());
        }

        private void LstResults_DoubleClick(object sender, EventArgs e)
        {
            SelectItem();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SelectItem();
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0) return;
            if (lstResults.SelectedIndex < 0)
            {
                lstResults.SelectedIndex = lstResults.Items.Count - 1;
                lstResults.TopIndex = Math.Max(0, lstResults.SelectedIndex - 3);
                return;
            }
            int newIndex = Math.Max(0, lstResults.SelectedIndex - 4);
            lstResults.SelectedIndex = newIndex;
            lstResults.TopIndex = newIndex;
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0) return;
            if (lstResults.SelectedIndex < 0)
            {
                lstResults.SelectedIndex = 0;
                lstResults.TopIndex = 0;
                return;
            }
            int newIndex = Math.Min(lstResults.Items.Count - 1, lstResults.SelectedIndex + 4);
            lstResults.SelectedIndex = newIndex;
            lstResults.TopIndex = newIndex;
        }

        private void SelectItem()
        {
            if (lstResults.SelectedIndex >= 0)
            {
                string selectedText = lstResults.SelectedItem.ToString();
                var item = menuItems.FirstOrDefault(i => selectedText.StartsWith(i.Name));
                SelectedItem = item;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please select an item.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}