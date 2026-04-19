using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
    public class KeyboardDialog : Form
    {
        private TextBox txtInput = new TextBox();
        public string EnteredValue => txtInput.Text;

        public KeyboardDialog(string title = "Enter Text")
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Layout constants (same as NumpadDialog)
            int padding = 32;
            int btnWidth = 100, btnHeight = 80, spacing = 8;
            int txtHeight = 70;
            int txtSpacing = 24;

            // Number row (added above QWERTY)
            string[] numberRow = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

            // QWERTY layout
            string[][] layout = new string[][] {
                numberRow,
                new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
                new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" },
                new string[] { "Z", "X", "C", "V", "B", "N", "M" }
            };

            // Last row: Clear, Clear All, Space, Back, OK
            string[] lastRow = new string[] { "Clear", "Clear All", "Space", "Back", "OK" };

            int rows = layout.Length + 1; // 4 alpha/number rows + 1 action row
            int maxCols = 10; // For the widest row
            int keyboardWidth = maxCols * btnWidth + (maxCols - 1) * spacing;
            int keyboardHeight = rows * btnHeight + (rows - 1) * spacing;
            int dialogWidth = keyboardWidth + 2 * padding;
            int dialogHeight = padding + txtHeight + txtSpacing + keyboardHeight + padding;

            this.ClientSize = new Size(dialogWidth, dialogHeight);

            // Textbox
            txtInput.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
            txtInput.Size = new Size(keyboardWidth, txtHeight);
            txtInput.Location = new Point(padding, padding);
            txtInput.TextAlign = HorizontalAlignment.Center;
            txtInput.ReadOnly = true;
            this.Controls.Add(txtInput);

            int buttonsTop = txtInput.Bottom + txtSpacing;
            // Alpha rows
            for (int row = 0; row < layout.Length; row++)
            {
                int cols = layout[row].Length;
                int rowWidth = cols * btnWidth + (cols - 1) * spacing;
                int rowLeft = padding + (keyboardWidth - rowWidth) / 2;
                for (int col = 0; col < cols; col++)
                {
                    string text = layout[row][col];
                    var btn = new Button
                    {
                        Text = text,
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        Size = new Size(btnWidth, btnHeight),
                        Location = new Point(rowLeft, buttonsTop + row * (btnHeight + spacing)),
                        BackColor = Color.FromArgb(255, 255, 204), // yellow
                        ForeColor = Color.Black
                    };
                    btn.Click += (s, e) => HandleButtonClick(text);
                    this.Controls.Add(btn);
                    rowLeft += btn.Width + spacing;
                }
            }

            // Last row
            int lastRowY = buttonsTop + layout.Length * (btnHeight + spacing);
            int longBtnWidth = (int)(btnWidth * 1.5); // Slightly longer for action buttons
            int[] lastRowWidths = new int[] {
                longBtnWidth, // Clear
                longBtnWidth, // Clear All
                btnWidth * 3 + spacing * 2, // Space (wider)
                longBtnWidth, // Back
                longBtnWidth // OK
            };
            int lastRowTotalWidth = 0;
            foreach (var w in lastRowWidths) lastRowTotalWidth += w;
            lastRowTotalWidth += (lastRow.Length - 1) * spacing;
            int lastRowLeft = padding + (keyboardWidth - lastRowTotalWidth) / 2;
            for (int i = 0; i < lastRow.Length; i++)
            {
                string text = lastRow[i];
                Color backColor = text == "OK" ? Color.FromArgb(204, 255, 204) :
                                  (text == "Clear" || text == "Clear All" || text == "Back") ? Color.FromArgb(255, 204, 204) :
                                  Color.FromArgb(255, 255, 204);
                var btn = new Button
                {
                    Text = text == "Space" ? "Space" : text,
                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                    Size = new Size(lastRowWidths[i], btnHeight),
                    Location = new Point(lastRowLeft, lastRowY),
                    BackColor = backColor,
                    ForeColor = Color.Black
                };
                btn.Click += (s, e) => HandleButtonClick(text);
                this.Controls.Add(btn);
                lastRowLeft += btn.Width + spacing;
            }
        }

        private void HandleButtonClick(string text)
        {
            if (text == "Clear")
            {
                if (txtInput.Text.Length > 0)
                    txtInput.Text = txtInput.Text.Substring(0, txtInput.Text.Length - 1);
            }
            else if (text == "Clear All")
            {
                txtInput.Text = string.Empty;
            }
            else if (text == "OK")
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else if (text == "Back")
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            else if (text == "Space")
            {
                txtInput.Text += " ";
            }
            else
            {
                txtInput.Text += text;
            }
        }
    }
}
