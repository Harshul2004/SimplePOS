using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
 public class QuantityDialog : Form
 {
 private NumericUpDown numQty;
 private Button btnOK;
 private Button btnCancel;
 public int Quantity { get; private set; }

 public QuantityDialog(string itemName, double price, int initialQty =1)
 {
 this.Text = $"Select Quantity - {itemName}";
 this.Size = new Size(340,180);
 this.FormBorderStyle = FormBorderStyle.FixedDialog;
 this.MaximizeBox = false;
 this.MinimizeBox = false;
 this.StartPosition = FormStartPosition.CenterParent;

 var lblName = new Label { Text = $"Item: {itemName}", Left =20, Top =20, Width =280, Font = new Font("Segoe UI",11F, FontStyle.Bold) };
 var lblPrice = new Label { Text = $"Price: ?{price:0.##}", Left =20, Top =50, Width =180, Font = new Font("Segoe UI",10F) };
 var lblQty = new Label { Text = "Quantity:", Left =20, Top =80, Width =80 };
 numQty = new NumericUpDown { Left =110, Top =78, Width =80, Minimum =1, Maximum =1000, Value = initialQty };

 btnOK = new Button { Text = "OK", Left =60, Top =120, Width =80 };
 btnCancel = new Button { Text = "Cancel", Left =160, Top =120, Width =80 };
 btnOK.Click += BtnOK_Click;
 btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

 this.Controls.Add(lblName);
 this.Controls.Add(lblPrice);
 this.Controls.Add(lblQty);
 this.Controls.Add(numQty);
 this.Controls.Add(btnOK);
 this.Controls.Add(btnCancel);
 }

 private void BtnOK_Click(object sender, EventArgs e)
 {
 Quantity = (int)numQty.Value;
 this.DialogResult = DialogResult.OK;
 }
 }
}
