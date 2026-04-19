using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using System.Linq;

namespace SimplePOS
{
 public class BillEditorForm : Form
 {
 private readonly Bill bill;
 private List<BillItem> items;
 private List<MenuItemModel> menuItems;
 private ListView itemsListView;
 private ListView menuListView;
 private Button btnSave;
 private Button btnCancel;
 private Button btnAdd;
 private Button btnDelete;
 private Label lblTotal;
 private TextBox txtSearch;
 private SplitContainer splitContainer;
 private Panel bottomPanel;

 public List<BillItem> EditedItems { get; private set; }
 public double EditedTotal { get; private set; }

 public BillEditorForm(Bill bill)
 {
 this.bill = bill;
 this.items = string.IsNullOrWhiteSpace(bill.Items) ? new List<BillItem>() :
 JsonSerializer.Deserialize<List<BillItem>>(bill.Items) ?? new List<BillItem>();
 this.menuItems = MenuDataHelper.LoadMenuItems();
 InitializeUI();
 RefreshMenu();
 RefreshItems();
 }

 private void InitializeUI()
 {
 this.Text = $"Edit Bill - {bill.BillId}";
 this.Size = new Size(1000,650);
 this.StartPosition = FormStartPosition.CenterScreen;
 this.FormBorderStyle = FormBorderStyle.Sizable;
 this.MaximizeBox = true;
 this.MinimizeBox = true;

 splitContainer = new SplitContainer
 {
 Dock = DockStyle.Fill,
 Orientation = Orientation.Vertical,
 // Do NOT set SplitterDistance, Panel1MinSize, or Panel2MinSize here
 BackColor = Color.White
 };
 this.Controls.Add(splitContainer);

 // --- Left: Menu Items ---
 var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
 txtSearch = new TextBox { PlaceholderText = "Search menu...", Dock = DockStyle.Top, Font = new Font("Segoe UI",12F), Margin = new Padding(8) };
 txtSearch.TextChanged += (s, e) => RefreshMenu();
 leftPanel.Controls.Add(txtSearch);
 menuListView = new ListView
 {
 View = View.Details,
 FullRowSelect = true,
 MultiSelect = false,
 Dock = DockStyle.Fill,
 Font = new Font("Segoe UI",12F),
 HideSelection = false
 };
 menuListView.Columns.Add("Item Name",200);
 menuListView.Columns.Add("Price",100);
 menuListView.DoubleClick += MenuListView_DoubleClick;
 leftPanel.Controls.Add(menuListView);
 splitContainer.Panel1.Controls.Add(leftPanel);

 // --- Right: Bill Items ---
 var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
 itemsListView = new ListView
 {
 View = View.Details,
 FullRowSelect = true,
 MultiSelect = false,
 Dock = DockStyle.Fill,
 Font = new Font("Segoe UI",12F),
 HideSelection = false
 };
 itemsListView.Columns.Add("Item Name",200);
 itemsListView.Columns.Add("Qty",80);
 itemsListView.Columns.Add("Price",100);
 itemsListView.Columns.Add("Total",100);
 itemsListView.DoubleClick += ItemsListView_DoubleClick;
 rightPanel.Controls.Add(itemsListView);
 splitContainer.Panel2.Controls.Add(rightPanel);

 // --- Bottom: Total + Add/Delete/Save/Cancel ---
 bottomPanel = new Panel { Dock = DockStyle.Bottom, Height =70, BackColor = Color.White };
 lblTotal = new Label { Text = "Total:0", Font = new Font("Segoe UI",16F, FontStyle.Bold), Left =20, Top =20, Width =300 };
 btnAdd = new Button { Text = "Add", Width =100, Height =40, Left =500, Top =15, Font = new Font("Segoe UI",14F, FontStyle.Bold), BackColor = Color.FromArgb(220,240,255) };
 btnDelete = new Button { Text = "Delete", Width =100, Height =40, Left =610, Top =15, Font = new Font("Segoe UI",14F, FontStyle.Bold), BackColor = Color.FromArgb(255,220,220) };
 btnSave = new Button { Text = "Save", Width =100, Height =40, Left =720, Top =15, Font = new Font("Segoe UI",14F, FontStyle.Bold), BackColor = Color.FromArgb(200,255,200) };
 btnCancel = new Button { Text = "Cancel", Width =100, Height =40, Left =830, Top =15, Font = new Font("Segoe UI",14F, FontStyle.Bold), BackColor = Color.FromArgb(255,220,220) };
 btnAdd.Click += BtnAdd_Click;
 btnDelete.Click += BtnDelete_Click;
 btnSave.Click += BtnSave_Click;
 btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
 bottomPanel.Controls.Add(lblTotal);
 bottomPanel.Controls.Add(btnAdd);
 bottomPanel.Controls.Add(btnDelete);
 bottomPanel.Controls.Add(btnSave);
 bottomPanel.Controls.Add(btnCancel);
 this.Controls.Add(bottomPanel);

 // At the end of InitializeUI or in the constructor:
 this.Load += BillEditorForm_Load;
 }

 private void RefreshMenu()
 {
 menuListView.Items.Clear();
 string search = txtSearch.Text.Trim().ToLower();
 var filtered = string.IsNullOrEmpty(search) ? menuItems : menuItems.Where(m => m.Name.ToLower().Contains(search));
 foreach (var item in filtered)
 {
 var lvi = new ListViewItem(new[] { item.Name, item.Price.ToString("0.##") });
 menuListView.Items.Add(lvi);
 }
 }

 private void MenuListView_DoubleClick(object sender, EventArgs e)
 {
 if (menuListView.SelectedItems.Count >0)
 {
 int idx = menuListView.SelectedIndices[0];
 var filtered = string.IsNullOrEmpty(txtSearch.Text.Trim()) ? menuItems : menuItems.Where(m => m.Name.ToLower().Contains(txtSearch.Text.Trim().ToLower())).ToList();
 var menuItem = filtered[idx];
 using var dlg = new QuantityDialog(menuItem.Name, menuItem.Price);
 if (dlg.ShowDialog(this) == DialogResult.OK)
 {
 var existing = items.FirstOrDefault(i => i.Name == menuItem.Name && Math.Abs(i.Price - menuItem.Price) <0.01);
 if (existing != null)
 existing.Quantity += dlg.Quantity;
 else
 items.Add(new BillItem { Name = menuItem.Name, Quantity = dlg.Quantity, Price = menuItem.Price });
 RefreshItems();
 }
 }
}

private void BtnDelete_Click(object sender, EventArgs e)
{
 if (itemsListView.SelectedIndices.Count >0)
 {
 int idx = itemsListView.SelectedIndices[0];
 items.RemoveAt(idx);
 RefreshItems();
 }
}

private void ItemsListView_DoubleClick(object sender, EventArgs e)
{
 if (itemsListView.SelectedIndices.Count >0)
 {
 int idx = itemsListView.SelectedIndices[0];
 var item = items[idx];
 using var dlg = new BillItemEditDialog(item);
 if (dlg.ShowDialog(this) == DialogResult.OK)
 {
 if (dlg.EditedItem.Quantity ==0)
 {
 // Logical removal
 items.RemoveAt(idx);
 }
 else
 {
 items[idx] = dlg.EditedItem;
 }
 RefreshItems();
 }
 }
}

private void RefreshItems()
{
 itemsListView.Items.Clear();
 double total =0;
 foreach (var item in items)
 {
 double itemTotal = item.Quantity * item.Price;
 total += itemTotal;
 itemsListView.Items.Add(new ListViewItem(new[]
 {
 item.Name,
 item.Quantity.ToString(),
 item.Price.ToString("0.##"),
 itemTotal.ToString("0.##")
 }));
 }
 lblTotal.Text = $"Total: {total:0.##}";
 }

 private void BtnAdd_Click(object sender, EventArgs e)
 {
 if (menuListView.SelectedItems.Count >0)
 {
 int idx = menuListView.SelectedIndices[0];
 var filtered = string.IsNullOrEmpty(txtSearch.Text.Trim()) ? menuItems : menuItems.Where(m => m.Name.ToLower().Contains(txtSearch.Text.Trim().ToLower())).ToList();
 var menuItem = filtered[idx];
 using var dlg = new QuantityDialog(menuItem.Name, menuItem.Price);
 if (dlg.ShowDialog(this) == DialogResult.OK)
 {
 var existing = items.FirstOrDefault(i => i.Name == menuItem.Name && Math.Abs(i.Price - menuItem.Price) <0.01);
 if (existing != null)
 existing.Quantity += dlg.Quantity;
 else
 items.Add(new BillItem { Name = menuItem.Name, Quantity = dlg.Quantity, Price = menuItem.Price });
 RefreshItems();
 }
 }
}

 private void BtnSave_Click(object sender, EventArgs e)
 {
 this.EditedItems = new List<BillItem>(items);
 this.EditedTotal =0;
 foreach (var item in items)
 this.EditedTotal += item.Quantity * item.Price;
 this.DialogResult = DialogResult.OK;
 }

 private void BillEditorForm_Load(object sender, EventArgs e)
 {
 // Set Splitter properties after layout is valid
 splitContainer.Panel1MinSize =200;
 splitContainer.Panel2MinSize =400;
 int min = splitContainer.Panel1MinSize;
 int max = this.ClientSize.Width - splitContainer.Panel2MinSize;
 int desired = (int)(this.ClientSize.Width *0.33); //33% of width
 splitContainer.SplitterDistance = Math.Max(min, Math.Min(desired, max));
 }
 }
}
