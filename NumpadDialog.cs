using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
    public class NumpadDialog : Form
    {
        private TextBox txtInput = new TextBox();
        public string EnteredValue => txtInput.Text;

        public NumpadDialog(string title = "Enter Value")
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            // Layout constants
            int padding = 32;
            int btnWidth = 100, btnHeight = 80, spacing = 8;
            int numpadCols = 3, numpadRows = 4;
            int numpadWidth = numpadCols * btnWidth + (numpadCols - 1) * spacing;
            int numpadHeight = numpadRows * btnHeight + (numpadRows - 1) * spacing;
            int txtHeight = 70;
            int txtSpacing = 24; // Space between textbox and buttons

            // Calculate dialog size for equal bezels (including bottom)
            int dialogWidth = numpadWidth + 2 * padding;
            // Add space for the Close button
            int closeBtnHeight = btnHeight;
            int dialogHeight = padding + txtHeight + txtSpacing + numpadHeight + spacing + closeBtnHeight + padding;

            this.ClientSize = new Size(dialogWidth, dialogHeight);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Larger font for textbox
            txtInput.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
            txtInput.Size = new Size(numpadWidth, txtHeight);
            txtInput.Location = new Point(padding, padding);
            txtInput.TextAlign = HorizontalAlignment.Center;
            txtInput.ReadOnly = true;
            this.Controls.Add(txtInput);

            string[,] layout = {
                { "7", "8", "9" },
                { "4", "5", "6" },
                { "1", "2", "3" },
                { "Clear", "0", "OK" }
            };

            // Calculate top of first button row for equal top/bottom bezels
            int buttonsTop = txtInput.Bottom + txtSpacing;

            for (int row = 0; row < numpadRows; row++)
            {
                for (int col = 0; col < numpadCols; col++)
                {
                    string text = layout[row, col];
                    var btn = new Button
                    {
                        Text = text,
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        Size = new Size(btnWidth, btnHeight),
                        Location = new Point(padding + col * (btnWidth + spacing), buttonsTop + row * (btnHeight + spacing))
                    };

                    // Set button colors
                    if (text == "Clear")
                    {
                        btn.BackColor = Color.FromArgb(255, 204, 204); // Light Red
                        btn.ForeColor = Color.Black;
                    }
                    else if (text == "OK")
                    {
                        btn.BackColor = Color.FromArgb(204, 255, 204); // Light Green
                        btn.ForeColor = Color.Black;
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(255, 255, 204); // Light Yellow
                        btn.ForeColor = Color.Black;
                    }

                    btn.Click += (s, e) => HandleButtonClick(text);
                    this.Controls.Add(btn);
                }
            }

            // Add Close button below the last row
            var closeBtn = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Size = new Size(btnWidth * 3 + spacing * 2, closeBtnHeight),
                Location = new Point(padding, buttonsTop + numpadRows * (btnHeight + spacing)),
                BackColor = Color.LightGray,
                ForeColor = Color.Black
            };
            closeBtn.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(closeBtn);
        }

        private void HandleButtonClick(string text)
        {
            if (text == "Clear")
            {
                if (txtInput.Text.Length > 0)
                    txtInput.Text = txtInput.Text.Substring(0, txtInput.Text.Length - 1);
            }
            else if (text == "OK")
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                txtInput.Text += text;
            }
        }
    }
}