using System;
using System.Windows.Forms;

namespace SimplePOS
{
 public class BillItemEditDialog : Form
 {
 private TextBox txtName;
 private NumericUpDown numQty;
 private NumericUpDown numPrice;
 private Button btnOK;
 private Button btnCancel;
 private Button btnDelete;

 public BillItem EditedItem { get; private set; }

 public BillItemEditDialog(BillItem item = null)
 {
 this.Text = item == null ? "Add Item" : "Edit Item";
 this.Size = new System.Drawing.Size(350,220);
 this.FormBorderStyle = FormBorderStyle.FixedDialog;
 this.MaximizeBox = false;
 this.MinimizeBox = false;
 this.StartPosition = FormStartPosition.CenterParent;

 var lblName = new Label { Text = "Item Name:", Left =20, Top =20, Width =90 };
 txtName = new TextBox { Left =120, Top =20, Width =180 };
 var lblQty = new Label { Text = "Quantity:", Left =20, Top =60, Width =90 };
 numQty = new NumericUpDown { Left =120, Top =60, Width =80, Minimum =1, Maximum =1000 };
 var lblPrice = new Label { Text = "Price:", Left =20, Top =100, Width =90 };
 numPrice = new NumericUpDown { Left =120, Top =100, Width =80, Minimum =0, Maximum =10000, DecimalPlaces =2, Increment =1 };

 btnOK = new Button { Text = "OK", Left =40, Top =150, Width =80 };
 btnCancel = new Button { Text = "Cancel", Left =130, Top =150, Width =80 };
 btnDelete = new Button { Text = "Delete Item", Left =220, Top =150, Width =100, BackColor = System.Drawing.Color.FromArgb(255,220,220) };
 btnOK.Click += BtnOK_Click;
 btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
 btnDelete.Click += BtnDelete_Click;

 this.Controls.Add(lblName);
 this.Controls.Add(txtName);
 this.Controls.Add(lblQty);
 this.Controls.Add(numQty);
 this.Controls.Add(lblPrice);
 this.Controls.Add(numPrice);
 this.Controls.Add(btnOK);
 this.Controls.Add(btnCancel);
 if (item != null) this.Controls.Add(btnDelete);

 if (item != null)
 {
 txtName.Text = item.Name;
 numQty.Value = item.Quantity;
 numPrice.Value = (decimal)item.Price;
 }
 }

 private void BtnOK_Click(object sender, EventArgs e)
 {
 if (string.IsNullOrWhiteSpace(txtName.Text))
 {
 MessageBox.Show("Item name required.");
 return;
 }
 EditedItem = new BillItem
 {
 Name = txtName.Text.Trim(),
 Quantity = (int)numQty.Value,
 Price = (double)numPrice.Value
 };
 this.DialogResult = DialogResult.OK;
 }

 private void BtnDelete_Click(object sender, EventArgs e)
 {
 if (string.IsNullOrWhiteSpace(txtName.Text))
 {
 MessageBox.Show("Item name required.");
 return;
 }
 EditedItem = new BillItem
 {
 Name = txtName.Text.Trim(),
 Quantity =0, // Logical removal
 Price = (double)numPrice.Value
 };
 this.DialogResult = DialogResult.OK;
 }
 }
}
