using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace SimplePOS
{
    public partial class ChangeCounterControl : UserControl
    {
        private Button btn500;
        private Button btn100;
        private Button btn50;
        private Button btn20;
        private Button btn10;
        private Button btn5;
        private Button btn1;
        private Button btnClear;

        // Field to keep track of cash sum
        private int cashSum = 0;

        // Event to notify cash value changed
        public event EventHandler<int> CashValueChanged;

        public ChangeCounterControl()
        {
            InitializeComponent();
            InitializeChangeCounterButtons();
        }

        private void InitializeChangeCounterButtons()
        {
            // Button size and spacing
            int btnWidth = 160; // Twice the original width
            int btnHeight = 50;
            int spacing = 4; // Reduced spacing

            btn500 = CreateButton("500", btnWidth, btnHeight);
            btn100 = CreateButton("100", btnWidth, btnHeight);
            btn50 = CreateButton("50", btnWidth, btnHeight);
            btn20 = CreateButton("20", btnWidth, btnHeight);
            btn10 = CreateButton("10", btnWidth, btnHeight);
            btn5 = CreateButton("5", btnWidth, btnHeight);
            btn1 = CreateButton("1", btnWidth, btnHeight);
            btnClear = CreateButton("Clear", btnWidth, btnHeight);

            // Wire up click events
            btn500.Click += (s, e) => AddCash(500);
            btn100.Click += (s, e) => AddCash(100);
            btn50.Click += (s, e) => AddCash(50);
            btn20.Click += (s, e) => AddCash(20);
            btn10.Click += (s, e) => AddCash(10);
            btn5.Click += (s, e) => AddCash(5);
            btn1.Click += (s, e) => AddCash(1);
            btnClear.Click += (s, e) => ClearCash();

            // Arrange buttons in grid (2 columns, 4 rows)
            btn500.Left = 0;
            btn500.Top = 0;
            btn100.Left = btnWidth + spacing;
            btn100.Top = 0;

            btn50.Left = 0;
            btn50.Top = btnHeight + spacing;
            btn20.Left = btnWidth + spacing;
            btn20.Top = btnHeight + spacing;

            btn10.Left = 0;
            btn10.Top = 2 * (btnHeight + spacing);
            btn5.Left = btnWidth + spacing;
            btn5.Top = 2 * (btnHeight + spacing);

            btn1.Left = 0;
            btn1.Top = 3 * (btnHeight + spacing);
            btnClear.Left = btnWidth + spacing;
            btnClear.Top = 3 * (btnHeight + spacing);

            // Add buttons to control
            this.Controls.Add(btn500);
            this.Controls.Add(btn100);
            this.Controls.Add(btn50);
            this.Controls.Add(btn20);
            this.Controls.Add(btn10);
            this.Controls.Add(btn5);
            this.Controls.Add(btn1);
            this.Controls.Add(btnClear);

            // Ensure parent is wide enough for all buttons
            this.Width = 2 * btnWidth + spacing;
        }

        private Button CreateButton(string text, int width, int height)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Size = new Size(width, height),
                BackColor = Color.LightGray, // Match KoTButtonsControl
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Standard
            };
        }

        // AddCash and ClearCash logic
        private void AddCash(int amount)
        {
            cashSum += amount;
            CashValueChanged?.Invoke(this, cashSum);
        }

        private void ClearCash()
        {
            cashSum = 0;
            CashValueChanged?.Invoke(this, cashSum);
        }

        private void ChangeCounterControl_Load(object sender, EventArgs e)
        {

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!this.IsHandleCreated)
                return;

            try
            {
                int rows = 4;
                int cols = 2;
                int spacing = 4;
                int btnWidth = 160;
                int btnHeight = (this.Height - (rows - 1) * spacing) / rows;

                // Prevent shrinking below required width
                if (this.Width < 2 * btnWidth + spacing)
                    this.Width = 2 * btnWidth + spacing;

                // Row 1
                if (btn500 != null)
                {
                    btn500.Width = btnWidth;
                    btn500.Height = btnHeight;
                    btn500.Left = 0;
                    btn500.Top = 0;
                }
                if (btn100 != null)
                {
                    btn100.Width = btnWidth;
                    btn100.Height = btnHeight;
                    btn100.Left = btnWidth + spacing;
                    btn100.Top = 0;
                }

                // Row 2
                if (btn50 != null)
                {
                    btn50.Width = btnWidth;
                    btn50.Height = btnHeight;
                    btn50.Left = 0;
                    btn50.Top = btnHeight + spacing;
                }
                if (btn20 != null)
                {
                    btn20.Width = btnWidth;
                    btn20.Height = btnHeight;
                    btn20.Left = btnWidth + spacing;
                    btn20.Top = btnHeight + spacing;
                }

                // Row 3
                if (btn10 != null)
                {
                    btn10.Width = btnWidth;
                    btn10.Height = btnHeight;
                    btn10.Left = 0;
                    btn10.Top = 2 * (btnHeight + spacing);
                }
                if (btn5 != null)
                {
                    btn5.Width = btnWidth;
                    btn5.Height = btnHeight;
                    btn5.Left = btnWidth + spacing;
                    btn5.Top = 2 * (btnHeight + spacing);
                }

                // Row 4
                if (btn1 != null)
                {
                    btn1.Width = btnWidth;
                    btn1.Height = btnHeight;
                    btn1.Left = 0;
                    btn1.Top = 3 * (btnHeight + spacing);
                }
                if (btnClear != null)
                {
                    btnClear.Width = btnWidth;
                    btnClear.Height = btnHeight;
                    btnClear.Left = btnWidth + spacing;
                    btnClear.Top = 3 * (btnHeight + spacing);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ChangeCounterControl.OnResize exception: {ex}");
            }
        }
    }
}
