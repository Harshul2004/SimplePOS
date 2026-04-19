using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class AdminPanelForm : Form
    {
        private TabControl tabControl;
        private Panel pnlInventory;

        private Panel pnlCategories;
        private Panel pnlSubCategories;
        private Panel pnlItems;

        private int? selectedCategoryId = null;
        private int? selectedSubCategoryId = null;

        public AdminPanelForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Prevent resizing
            this.MaximizeBox = false; // Disable maximize/restore button
            this.MinimizeBox = true; // Allow minimizing
            this.ControlBox = true; // Show close/minimize
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized; // Normal maximized, taskbar visible
        }

        // Prevent moving and double-click restore by overriding WndProc
        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MOVE = 0xF010;
            if (m.Msg == WM_NCLBUTTONDBLCLK || (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MOVE))
            {
                // Block double-click and moving
                return;
            }
            base.WndProc(ref m);
        }

        private void AdminPanelForm_Load(object sender, EventArgs e)
        {
            this.Text = "Admin Panel";

            Label lblWelcome = new Label();
            lblWelcome.Text = "Welcome to Admin Panel";
            lblWelcome.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblWelcome.AutoSize = true;
            lblWelcome.Top = 30;
            lblWelcome.Left = 30;
            this.Controls.Add(lblWelcome);

            InitializeTabs();
            LoadInventoryPanels();
        }

        private void InitializeTabs()
        {
            tabControl = new TabControl();
            tabControl.Top = 100;
            tabControl.Left = 30;
            tabControl.Width = this.ClientSize.Width - 40;
            tabControl.Height = this.ClientSize.Height - 120;
            tabControl.Font = new Font("Segoe UI", 15F, FontStyle.Bold); // Match font size and weight
            tabControl.ItemSize = new Size(220, 50); // Increased width for full tab name
            tabControl.SizeMode = TabSizeMode.Fixed;

            TabPage inventoryTab = new TabPage("Inventory");
            inventoryTab.BackColor = Color.WhiteSmoke;

            pnlInventory = new Panel();
            pnlInventory.Dock = DockStyle.Fill;
            pnlInventory.BackColor = Color.White;
            inventoryTab.Controls.Add(pnlInventory);

            tabControl.TabPages.Add(inventoryTab);

            // --- Add Sales Tab ---
            TabPage salesTab = new TabPage("Sales");
            salesTab.BackColor = Color.WhiteSmoke;
            var salesTabControl = new SalesTabControl { Dock = DockStyle.Fill };
            salesTab.Controls.Add(salesTabControl);
            tabControl.TabPages.Add(salesTab);

            // --- Add Change Admin Pin Tab ---
            TabPage pinTab = new TabPage("Change Admin Pin");
            pinTab.BackColor = Color.WhiteSmoke;
            var changePinControl = new ChangeAdminPinControl { Dock = DockStyle.Fill };
            pinTab.Controls.Add(changePinControl);
            tabControl.TabPages.Add(pinTab);

            // --- Add Printer Setup Tab ---
            TabPage printerTab = new TabPage("Printer Setup");
            printerTab.BackColor = Color.WhiteSmoke;
            var printerSetupControl = new PrinterSetupControl { Dock = DockStyle.Fill };
            printerTab.Controls.Add(printerSetupControl);
            tabControl.TabPages.Add(printerTab);

            this.Controls.Add(tabControl);
        }

        private void LoadInventoryPanels()
        {
            int padding = 12;
            int panelWidth = (pnlInventory.Width - 4 * padding) / 3;
            int panelHeight = pnlInventory.Height - 2 * padding;

            // Categories Panel (light blue)
            pnlCategories = CreateCrudPanel("Categories", padding, padding, panelWidth, panelHeight,
                AddCategory_Click, EditCategory_Click, DeleteCategory_Click, Color.FromArgb(230, 240, 255));
            pnlCategories.AutoScroll = true; // Enable scrolling
            pnlInventory.Controls.Add(pnlCategories);

            // SubCategories Panel (light green)
            pnlSubCategories = CreateCrudPanel("SubCategories", pnlCategories.Right + padding, padding, panelWidth, panelHeight,
                AddSubCategory_Click, EditSubCategory_Click, DeleteSubCategory_Click, Color.FromArgb(240, 255, 240));
            pnlSubCategories.AutoScroll = true; // Enable scrolling
            pnlInventory.Controls.Add(pnlSubCategories);

            // Items Panel (light yellow)
            pnlItems = CreateCrudPanel("Items", pnlSubCategories.Right + padding, padding, panelWidth, panelHeight,
                AddItem_Click, EditItem_Click, DeleteItem_Click, Color.FromArgb(255, 255, 240));
            pnlItems.AutoScroll = true; // Enable scrolling
            pnlInventory.Controls.Add(pnlItems);

            LoadCategories();
        }

        private Panel CreateCrudPanel(string title, int left, int top, int width, int height,
            EventHandler addClick, EventHandler editClick, EventHandler deleteClick, Color? backColor = null)
        {
            Color panelColor = backColor ?? SystemColors.Control;
            Panel pnl = new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = panelColor
            };

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Top = 10,
                Left = 10
            };
            pnl.Controls.Add(lbl);

            int btnTop = lbl.Bottom + 10;
            int btnHeight = 44;
            int btnFontSize = 13;
            int btnSpacing = 10;
            int btnWidthAdd = 80;
            int btnWidthEdit = 100;
            int btnWidthDelete = 90;

            Button btnAdd = new Button
            {
                Text = "Add",
                Top = btnTop,
                Left = 10,
                Width = btnWidthAdd,
                Height = btnHeight,
                Font = new Font("Segoe UI", btnFontSize, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = panelColor // Match panel color
            };
            btnAdd.Click += addClick;
            pnl.Controls.Add(btnAdd);

            Button btnEdit = new Button
            {
                Text = "Edit",
                Top = btnTop,
                Left = btnAdd.Right + btnSpacing,
                Width = btnWidthEdit,
                Height = btnHeight,
                Font = new Font("Segoe UI", btnFontSize, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = panelColor // Match panel color
            };
            btnEdit.Click += editClick;
            pnl.Controls.Add(btnEdit);

            Button btnDelete = new Button
            {
                Text = "Delete",
                Top = btnTop,
                Left = btnEdit.Right + btnSpacing,
                Width = btnWidthDelete,
                Height = btnHeight,
                Font = new Font("Segoe UI", btnFontSize, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = panelColor // Match panel color
            };
            btnDelete.Click += deleteClick;
            pnl.Controls.Add(btnDelete);

            // Add scrollable content panel
            Panel contentPanel = new Panel
            {
                Name = "ContentPanel",
                Left = 0,
                Top = btnDelete.Bottom + 10,
                Width = pnl.Width - 2, // leave border
                Height = pnl.Height - (btnDelete.Bottom + 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(contentPanel);

            return pnl;
        }

        private void LoadCategories()
        {
            selectedCategoryId = null;
            // Find the content panel
            var contentPanel = pnlCategories.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ContentPanel");
            if (contentPanel == null) return;
            contentPanel.Controls.Clear();

            var categories = DBHelper.GetCategories();
            Color catPanelColor = Color.FromArgb(230, 240, 255);
            int y = 0;
            foreach (var cat in categories)
            {
                Button btn = new Button
                {
                    Text = cat.Name,
                    Tag = cat.ID,
                    Width = contentPanel.Width - 30,
                    Height = 44,
                    Top = y,
                    Left = 10,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = catPanelColor,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                btn.Click += (s, e) =>
                {
                    selectedCategoryId = cat.ID;
                    LoadSubCategories(cat.ID);
                };
                contentPanel.Controls.Add(btn);
                y += btn.Height + 8;
            }
        }

        private void LoadSubCategories(int categoryId)
        {
            selectedSubCategoryId = null;
            // Find the content panel
            var contentPanel = pnlSubCategories.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ContentPanel");
            if (contentPanel == null) return;
            contentPanel.Controls.Clear();

            var subs = DBHelper.GetSubCategories(categoryId);
            Color subPanelColor = Color.FromArgb(240, 255, 240);
            int y = 0;
            foreach (var sub in subs)
            {
                Button btn = new Button
                {
                    Text = sub.Name,
                    Tag = sub.ID,
                    Width = contentPanel.Width - 30,
                    Height = 44,
                    Top = y,
                    Left = 10,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = subPanelColor,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                btn.Click += (s, e) =>
                {
                    selectedSubCategoryId = sub.ID;
                    LoadItems(sub.ID);
                };
                contentPanel.Controls.Add(btn);
                y += btn.Height + 8;
            }

            // Always clear and reload items panel as well
            LoadItems(null);
        }

        private void LoadItems(int? subCategoryId)
        {
            // Find the content panel
            var contentPanel = pnlItems.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ContentPanel");
            if (contentPanel == null) return;
            contentPanel.Controls.Clear();

            if (subCategoryId == null)
                return;

            var items = DBHelper.GetMenuBySubCategory(subCategoryId.Value);
            int y = 0;
            foreach (var item in items)
            {
                Label lbl = new Label
                {
                    Text = $"{item.Name} - ₹{item.Price}",
                    AutoSize = true,
                    Top = y,
                    Left = 10,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Tag = item.ID,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                contentPanel.Controls.Add(lbl);
                y += lbl.Height + 8;
            }
        }

        // --------------------- CATEGORY CRUD ---------------------
        private void AddCategory_Click(object sender, EventArgs e)
        {
            var (ok, name) = EditDialog.ShowAddCategoryDialog();
            if (ok)
            {
                DBHelper.AddCategory(name);
                LoadCategories();
            }
        }

        private void EditCategory_Click(object sender, EventArgs e)
        {
            var categories = DBHelper.GetCategories();
            if (categories.Count == 0)
            {
                MessageBox.Show("No categories to edit!");
                return;
            }
            var (ok, id, name) = EditDialog.ShowEditCategoryDialog(categories);
            if (ok)
            {
                DBHelper.UpdateCategory(id, name);
                LoadCategories();
            }
        }

        private void DeleteCategory_Click(object sender, EventArgs e)
        {
            var categories = DBHelper.GetCategories();
            if (categories.Count == 0)
            {
                MessageBox.Show("No categories to delete!");
                return;
            }
            var (ok, id, name) = EditDialog.ShowDeleteCategoryDialog(categories);
            if (ok)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    DBHelper.DeleteCategory(id);
                    LoadCategories();
                }
            }
        }

        // --------------------- SUBCATEGORY CRUD ---------------------
        private void AddSubCategory_Click(object sender, EventArgs e)
        {
            var categories = DBHelper.GetCategories();
            if (categories.Count == 0)
            {
                MessageBox.Show("No categories available! Add a category first.");
                return;
            }
            List<(int, string)> catList;
            if (selectedCategoryId != null)
                catList = categories.Where(c => c.ID == selectedCategoryId.Value).ToList();
            else
                catList = categories;
            var (ok, name, catId) = EditDialog.ShowAddSubCategoryDialog(catList);
            if (ok)
            {
                DBHelper.AddSubCategory(name, catId);
                LoadSubCategories(catId);
            }
        }

        private void EditSubCategory_Click(object sender, EventArgs e)
        {
            if (selectedCategoryId == null)
            {
                MessageBox.Show("Please select a main category first.");
                return;
            }
            var catId = selectedCategoryId.Value;
            var categories = DBHelper.GetCategories();
            var subs = DBHelper.GetSubCategories(catId);
            if (subs.Count == 0)
            {
                MessageBox.Show("No subcategories to edit in this main category!");
                return;
            }
            var allSubs = subs.Select(x => (x.ID, x.Name, catId)).ToList();
            var (ok, id, name, newCatId) = EditDialog.ShowEditSubCategoryDialog(categories, allSubs);
            if (ok)
            {
                DBHelper.UpdateSubCategory(id, name, newCatId);
                LoadSubCategories(newCatId);
            }
        }

        private void DeleteSubCategory_Click(object sender, EventArgs e)
        {
            if (selectedCategoryId == null)
            {
                MessageBox.Show("Please select a main category first.");
                return;
            }
            var catId = selectedCategoryId.Value;
            var subs = DBHelper.GetSubCategories(catId);
            if (subs.Count == 0)
            {
                MessageBox.Show("No subcategories to delete in this main category!");
                return;
            }
            var subList = subs.Select(x => (x.ID, x.Name)).ToList();
            var (ok, id, name) = EditDialog.ShowDeleteSubCategoryDialog(subList);
            if (ok)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    DBHelper.DeleteSubCategory(id);
                    LoadSubCategories(catId);
                }
            }
        }

        // --------------------- ITEM CRUD ---------------------
        private void AddItem_Click(object sender, EventArgs e)
        {
            var categories = DBHelper.GetCategories();
            var allSubs = new List<(int, string)>();
            foreach (var cat in categories)
                allSubs.AddRange(DBHelper.GetSubCategories(cat.ID));
            if (allSubs.Count == 0)
            {
                MessageBox.Show("No subcategories available! Add a subcategory first.");
                return;
            }
            List<(int, string)> subList;
            if (selectedSubCategoryId != null)
                subList = allSubs.Where(s => s.Item1 == selectedSubCategoryId.Value).ToList();
            else
                subList = allSubs;
            var (ok, name, price, subCatId) = EditDialog.ShowAddItemDialog(subList);
            if (ok)
            {
                DBHelper.AddMenuItem(name, price, subCatId);
                LoadItems(subCatId);
            }
        }

        private void EditItem_Click(object sender, EventArgs e)
        {
            if (selectedSubCategoryId == null)
            {
                MessageBox.Show("Please select a subcategory first.");
                return;
            }
            var subCatId = selectedSubCategoryId.Value;
            // Get the subcategory name from the currently loaded subcategories
            var categories = DBHelper.GetCategories();
            string subCatName = null;
            foreach (var cat in categories)
            {
                var subs = DBHelper.GetSubCategories(cat.ID);
                var found = subs.FirstOrDefault(x => x.ID == subCatId);
                if (found.ID == subCatId)
                {
                    subCatName = found.Name;
                    break;
                }
            }
            if (string.IsNullOrEmpty(subCatName))
            {
                MessageBox.Show("Subcategory not found.");
                return;
            }
            var allSubs = new List<(int ID, string Name)> { (subCatId, subCatName) };
            var items = DBHelper.GetMenuBySubCategory(subCatId)
                .Select(i => (i.ID, i.Name, i.Price, subCatId)).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("No items to edit in this subcategory!");
                return;
            }
            var (ok, id, name, price, subCatId2) = EditDialog.ShowEditItemDialog(items, allSubs);
            if (ok)
            {
                DBHelper.UpdateMenuItem(id, name, price, subCatId2);
                LoadItems(subCatId2);
            }
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (selectedSubCategoryId == null)
            {
                MessageBox.Show("Please select a subcategory first.");
                return;
            }
            var subCatId = selectedSubCategoryId.Value;
            var items = DBHelper.GetMenuBySubCategory(subCatId);
            if (items.Count == 0)
            {
                MessageBox.Show("No items to delete in this subcategory!");
                return;
            }
            var itemList = items.Select(x => (x.ID, x.Name)).ToList();
            var (ok, id, name) = EditDialog.ShowDeleteItemDialog(itemList);
            if (ok)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    DBHelper.DeleteMenuItem(id);
                    LoadItems(subCatId);
                }
            }
        }

        // Helper for ComboBox items in CRUD dialogs
        private class ComboBoxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public ComboBoxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() { return Text; }
        }
    }

    // ---------------- Helper class for input popups ----------------
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label lblText = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 280 };
            Button confirmation = new Button() { Text = "OK", Left = 80, Width = 80, Top = 90, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 180, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { inputBox.Text = ""; prompt.Close(); };
            prompt.Controls.Add(lblText);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : "";
        }
    }
}
