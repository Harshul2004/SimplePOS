using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class ChangeAdminPinControl : UserControl
    {
        private Label lblTitle;
        private Label lblOldPin;
        private TextBox txtOldPin;
        private Label lblNewPin;
        private TextBox txtNewPin;
        private Label lblReNewPin;
        private TextBox txtReNewPin;
        private Button btnSave;
        private Label lblMessage;

        public ChangeAdminPinControl()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            int leftLabel = 40;
            int leftInput = 260;
            int top = 40;
            int spacingY = 48;

            lblTitle = new Label
            {
                Text = "Change Admin PIN",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                AutoSize = true,
                Top = top,
                Left = leftLabel
            };
            this.Controls.Add(lblTitle);
            top = lblTitle.Bottom + 40;

            lblOldPin = new Label
            {
                Text = "Enter Old PIN:",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Top = top,
                Left = leftLabel
            };
            this.Controls.Add(lblOldPin);
            txtOldPin = new TextBox
            {
                Font = new Font("Segoe UI", 15F, FontStyle.Regular),
                Width = 220,
                Top = top - 4,
                Left = leftInput,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(txtOldPin);
            top += spacingY;

            lblNewPin = new Label
            {
                Text = "Enter New PIN:",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Top = top,
                Left = leftLabel
            };
            this.Controls.Add(lblNewPin);
            txtNewPin = new TextBox
            {
                Font = new Font("Segoe UI", 15F, FontStyle.Regular),
                Width = 220,
                Top = top - 4,
                Left = leftInput,
                UseSystemPasswordChar = true
            };
            this.Controls.Add(txtNewPin);
            top += spacingY;

            lblReNewPin = new Label
            {
                Text = "Re-Enter New PIN:",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Top = top,
                Left = leftLabel
            };
            this.Controls.Add(lblReNewPin);
            txtReNewPin = new TextBox
            {
                Font = new Font("Segoe UI", 15F, FontStyle.Regular),
                Width = 220,
                Top = top - 4,
                Left = leftInput,
                UseSystemPasswordChar = true
            };
            txtReNewPin.TextChanged += TxtReNewPin_TextChanged;
            this.Controls.Add(txtReNewPin);
            top += spacingY + 10;

            btnSave = new Button
            {
                Text = "Save",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                Width = 120,
                Height = 44,
                Top = top,
                Left = leftLabel,
                BackColor = Color.FromArgb(200, 255, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            lblMessage = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Top = btnSave.Bottom + 18,
                Left = leftLabel,
                ForeColor = Color.Red
            };
            this.Controls.Add(lblMessage);

            txtOldPin.TextChanged += AnyPinField_TextChanged;
            txtNewPin.TextChanged += AnyPinField_TextChanged;
        }

        private void TxtReNewPin_TextChanged(object sender, EventArgs e)
        {
            if (txtNewPin.Text != txtReNewPin.Text)
            {
                lblMessage.Text = "New PIN and Re-Entered PIN do not match.";
                lblMessage.ForeColor = Color.Red;
            }
            else
            {
                lblMessage.Text = "";
            }
        }

        private void AnyPinField_TextChanged(object sender, EventArgs e)
        {
            // Clear message when user edits any field
            lblMessage.Text = "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = true;
            lblMessage.ForeColor = Color.Red;
            lblMessage.Text = "";
            string oldPin = txtOldPin.Text.Trim();
            string newPin = txtNewPin.Text.Trim();
            string reNewPin = txtReNewPin.Text.Trim();

            if (string.IsNullOrEmpty(oldPin) || string.IsNullOrEmpty(newPin) || string.IsNullOrEmpty(reNewPin))
            {
                lblMessage.Text = "All fields are required.";
                return;
            }

            string dbPin = DBHelper.GetAdminPIN();
            if (oldPin != dbPin)
            {
                lblMessage.Text = "Old PIN is incorrect.";
                return;
            }

            if (newPin != reNewPin)
            {
                lblMessage.Text = "New PIN and Re-Entered PIN do not match.";
                return;
            }

            if (newPin.Length < 4)
            {
                lblMessage.Text = "PIN must be at least 4 characters.";
                return;
            }

            DBHelper.SetAdminPIN(newPin);
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = "PIN changed successfully.";
            lblMessage.Visible = true;
            lblMessage.BringToFront();
            txtOldPin.Text = "";
            txtNewPin.Text = "";
            txtReNewPin.Text = "";
        }
    }
}
