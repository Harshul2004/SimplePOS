using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class EditDialog : Form
    {
        public string NameValue { get; private set; }
        public double PriceValue { get; private set; }
        public int SelectedCategoryId { get; private set; }
        public int SelectedSubCategoryId { get; private set; }

        // Helper for consistent font and size
        private static Font DialogFont => new Font("Segoe UI", 14F, FontStyle.Regular);
        private static Font DialogButtonFont => new Font("Segoe UI", 14F, FontStyle.Bold);
        private static Padding DialogPadding => new Padding(24);
        private static int DialogRowHeight => 48;
        private static int DialogButtonHeight => 48;
        private static int DialogButtonWidth => 120;
        private static int DialogInputWidth => 340;
        private static int DialogLabelWidth => 180;

        // --- 1. Main Category Add Dialog ---
        public static (bool ok, string name) ShowAddCategoryDialog()
        {
            using var dlg = new EditDialog();
            dlg.Text = "Add Main Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lbl = new Label { Text = "Category Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txt = new TextBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left };
            var btnOk = new Button { Text = "Add", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lbl, 0, 0);
            layout.Controls.Add(txt, 1, 0);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 1);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            btnOk.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    MessageBox.Show("Category name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                return (true, txt.Text.Trim());
            return (false, null);
        }

        // --- 2. Sub Category Add Dialog ---
        public static (bool ok, string name, int categoryId) ShowAddSubCategoryDialog(List<(int ID, string Name)> categories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Add Sub Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblName = new Label { Text = "SubCategory Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtName = new TextBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left };
            var lblCat = new Label { Text = "Main Category:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbCat = new ComboBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in categories) cmbCat.Items.Add(new ComboBoxItem(c.Name, c.ID));
            var btnOk = new Button { Text = "Add", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(txtName, 1, 0);
            layout.Controls.Add(lblCat, 0, 1);
            layout.Controls.Add(cmbCat, 1, 1);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 2);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            btnOk.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text) || cmbCat.SelectedItem == null)
                {
                    MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtName.Text) && cmbCat.SelectedItem is ComboBoxItem item)
                return (true, txtName.Text.Trim(), item.Value);
            return (false, null, 0);
        }

        // --- 3. Food Item Add Dialog ---
        public static (bool ok, string name, double price, int subCatId) ShowAddItemDialog(List<(int ID, string Name)> subCategories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Add Food Item";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblName = new Label { Text = "Item Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtName = new TextBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left };
            var lblPrice = new Label { Text = "Price:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtPrice = new TextBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left };
            var lblSubCat = new Label { Text = "SubCategory:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbSubCat = new ComboBox { Width = DialogInputWidth, Font = DialogFont, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var s in subCategories) cmbSubCat.Items.Add(new ComboBoxItem(s.Name, s.ID));
            var btnOk = new Button { Text = "Add", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(txtName, 1, 0);
            layout.Controls.Add(lblPrice, 0, 1);
            layout.Controls.Add(txtPrice, 1, 1);
            layout.Controls.Add(lblSubCat, 0, 2);
            layout.Controls.Add(cmbSubCat, 1, 2);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 3);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            btnOk.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text) || cmbSubCat.SelectedItem == null || !double.TryParse(txtPrice.Text, out _))
                {
                    MessageBox.Show("All fields are required and price must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtName.Text) && double.TryParse(txtPrice.Text, out double price) && cmbSubCat.SelectedItem is ComboBoxItem item)
                return (true, txtName.Text.Trim(), price, item.Value);
            return (false, null, 0, 0);
        }

        // --- 4. Main Category Edit Dialog ---
        public static (bool ok, int id, string name) ShowEditCategoryDialog(List<(int ID, string Name)> categories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Edit Main Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblCat = new Label { Text = "Select Category:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbCat = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var c in categories) cmbCat.Items.Add(new ComboBoxItem(c.Name, c.ID));
            var lblName = new Label { Text = "New Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtName = new TextBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, Font = DialogFont };
            var btnOk = new Button { Text = "Save", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblCat, 0, 0);
            layout.Controls.Add(cmbCat, 1, 0);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(txtName, 1, 1);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 2);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            cmbCat.SelectedIndexChanged += (s, e) =>
            {
                if (cmbCat.SelectedItem is ComboBoxItem item)
                    txtName.Text = item.Text;
            };
            if (categories.Count > 0) cmbCat.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbCat.SelectedItem == null || string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbCat.SelectedItem is ComboBoxItem sel && !string.IsNullOrWhiteSpace(txtName.Text))
                return (true, sel.Value, txtName.Text.Trim());
            return (false, 0, null);
        }

        // --- 5. Sub Category Edit Dialog ---
        public static (bool ok, int id, string name, int catId) ShowEditSubCategoryDialog(List<(int ID, string Name)> categories, List<(int ID, string Name, int CatID)> subCategories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Edit Sub Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblSub = new Label { Text = "Select SubCategory:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbSub = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var s in subCategories) cmbSub.Items.Add(new ComboBoxItem(s.Name, s.ID));
            var lblName = new Label { Text = "New Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtName = new TextBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, Font = DialogFont };
            var lblCat = new Label { Text = "Main Category:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbCat = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var c in categories) cmbCat.Items.Add(new ComboBoxItem(c.Name, c.ID));
            var btnOk = new Button { Text = "Save", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblSub, 0, 0);
            layout.Controls.Add(cmbSub, 1, 0);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(txtName, 1, 1);
            layout.Controls.Add(lblCat, 0, 2);
            layout.Controls.Add(cmbCat, 1, 2);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 3);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            cmbSub.SelectedIndexChanged += (s, e) =>
            {
                if (cmbSub.SelectedItem is ComboBoxItem item)
                {
                    txtName.Text = item.Text;
                    var sub = subCategories.FirstOrDefault(x => x.ID == item.Value);
                    cmbCat.SelectedItem = cmbCat.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == sub.CatID);
                }
            };
            if (subCategories.Count > 0) cmbSub.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbSub.SelectedItem == null || string.IsNullOrWhiteSpace(txtName.Text) || cmbCat.SelectedItem == null)
                {
                    MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbSub.SelectedItem is ComboBoxItem sel && !string.IsNullOrWhiteSpace(txtName.Text) && cmbCat.SelectedItem is ComboBoxItem catSel)
                return (true, sel.Value, txtName.Text.Trim(), catSel.Value);
            return (false, 0, null, 0);
        }

        // --- 6. Food Item Edit Dialog ---
        public static (bool ok, int id, string name, double price, int subCatId) ShowEditItemDialog(List<(int ID, string Name, double Price, int SubCatID)> items, List<(int ID, string Name)> subCategories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Edit Food Item";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblItem = new Label { Text = "Select Item:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbItem = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var i in items) cmbItem.Items.Add(new ComboBoxItem(i.Name + " - ₹" + i.Price, i.ID));
            var lblName = new Label { Text = "New Name:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtName = new TextBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, Font = DialogFont };
            var lblPrice = new Label { Text = "Price:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var txtPrice = new TextBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, Font = DialogFont };
            var lblSubCat = new Label { Text = "SubCategory:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbSubCat = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var s in subCategories) cmbSubCat.Items.Add(new ComboBoxItem(s.Name, s.ID));
            var btnOk = new Button { Text = "Save", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblItem, 0, 0);
            layout.Controls.Add(cmbItem, 1, 0);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(txtName, 1, 1);
            layout.Controls.Add(lblPrice, 0, 2);
            layout.Controls.Add(txtPrice, 1, 2);
            layout.Controls.Add(lblSubCat, 0, 3);
            layout.Controls.Add(cmbSubCat, 1, 3);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 4);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            cmbItem.SelectedIndexChanged += (s, e) =>
            {
                if (cmbItem.SelectedItem is ComboBoxItem item)
                {
                    var food = items.FirstOrDefault(x => x.ID == item.Value);
                    txtName.Text = food.Name;
                    txtPrice.Text = food.Price.ToString();
                    cmbSubCat.SelectedItem = cmbSubCat.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == food.SubCatID);
                }
            };
            if (items.Count > 0) cmbItem.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbItem.SelectedItem == null || string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text) || cmbSubCat.SelectedItem == null || !double.TryParse(txtPrice.Text, out _))
                {
                    MessageBox.Show("All fields are required and price must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbItem.SelectedItem is ComboBoxItem sel && !string.IsNullOrWhiteSpace(txtName.Text) && double.TryParse(txtPrice.Text, out double price) && cmbSubCat.SelectedItem is ComboBoxItem subSel)
                return (true, sel.Value, txtName.Text.Trim(), price, subSel.Value);
            return (false, 0, null, 0, 0);
        }

        // --- 7. Main Category Delete Dialog ---
        public static (bool ok, int id, string name) ShowDeleteCategoryDialog(List<(int ID, string Name)> categories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Delete Main Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblCat = new Label { Text = "Select Category:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbCat = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var c in categories) cmbCat.Items.Add(new ComboBoxItem(c.Name, c.ID));
            var btnOk = new Button { Text = "Delete", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblCat, 0, 0);
            layout.Controls.Add(cmbCat, 1, 0);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 1);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            if (categories.Count > 0) cmbCat.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbCat.SelectedItem == null)
                {
                    MessageBox.Show("Please select a category to delete.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbCat.SelectedItem is ComboBoxItem sel)
                return (true, sel.Value, sel.Text);
            return (false, 0, null);
        }

        // --- 8. Sub Category Delete Dialog ---
        public static (bool ok, int id, string name) ShowDeleteSubCategoryDialog(List<(int ID, string Name)> subCategories)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Delete Sub Category";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblSub = new Label { Text = "Select SubCategory:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbSub = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var s in subCategories) cmbSub.Items.Add(new ComboBoxItem(s.Name, s.ID));
            var btnOk = new Button { Text = "Delete", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblSub, 0, 0);
            layout.Controls.Add(cmbSub, 1, 0);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 1);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            if (subCategories.Count > 0) cmbSub.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbSub.SelectedItem == null)
                {
                    MessageBox.Show("Please select a subcategory to delete.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbSub.SelectedItem is ComboBoxItem sel)
                return (true, sel.Value, sel.Text);
            return (false, 0, null);
        }

        // --- 9. Food Item Delete Dialog ---
        public static (bool ok, int id, string name) ShowDeleteItemDialog(List<(int ID, string Name)> items)
        {
            using var dlg = new EditDialog();
            dlg.Text = "Delete Food Item";
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.Padding = DialogPadding;
            dlg.Font = DialogFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogLabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DialogInputWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogRowHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DialogButtonHeight + 16));

            var lblItem = new Label { Text = "Select Item:", Anchor = AnchorStyles.Left, AutoSize = true, Font = DialogFont };
            var cmbItem = new ComboBox { Width = DialogInputWidth, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList, Font = DialogFont };
            foreach (var i in items) cmbItem.Items.Add(new ComboBoxItem(i.Name, i.ID));
            var btnOk = new Button { Text = "Delete", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
            var btnCancel = new Button { Text = "Cancel", Width = DialogButtonWidth, Height = DialogButtonHeight, Font = DialogButtonFont, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.None, Padding = new Padding(0, 8, 0, 0) };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            layout.Controls.Add(lblItem, 0, 0);
            layout.Controls.Add(cmbItem, 1, 0);
            layout.SetColumnSpan(btnPanel, 2);
            layout.Controls.Add(btnPanel, 0, 1);

            dlg.Controls.Add(layout);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            if (items.Count > 0) cmbItem.SelectedIndex = 0;
            btnOk.Click += (s, e) => {
                if (cmbItem.SelectedItem == null)
                {
                    MessageBox.Show("Please select an item to delete.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dlg.DialogResult = DialogResult.None;
                }
            };
            if (dlg.ShowDialog() == DialogResult.OK && cmbItem.SelectedItem is ComboBoxItem sel)
                return (true, sel.Value, sel.Text);
            return (false, 0, null);
        }

        // --- ComboBoxItem helper ---
        private class ComboBoxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public ComboBoxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() { return Text; }
        }
    }
}
