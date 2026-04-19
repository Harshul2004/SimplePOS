using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplePOS
{
    public partial class BillActionsControl : UserControl
    {
        public Button btnConfirmBill { get; private set; } // New button
        public Button btnBillReprint { get; private set; }
        public Button btnBillModify { get; private set; }
        public Button btnReprintLastBill { get; private set; }

        // Add an event for reprinting the last bill
        public event EventHandler ReprintLastBillClicked;
        public event EventHandler BillModifyClicked;
        public event EventHandler BillReprintClicked;

        public BillActionsControl()
        {
            InitializeComponent();
            InitializeBillActionButtons();
            btnReprintLastBill.Click += BtnReprintLastBill_Click;
            btnBillModify.Click += BtnBillModify_Click;
            btnBillReprint.Click += BtnBillReprint_Click;
        }

        private void InitializeBillActionButtons()
        {
            int totalHeight = 365;
            int btnCount = 4;
            int btnHeight = totalHeight / btnCount;

            btnConfirmBill = CreateButton("Confirm Bill", btnHeight);
            btnBillReprint = CreateButton("Bill Reprint", btnHeight);
            btnBillModify = CreateButton("Bill Modify", btnHeight);
            btnReprintLastBill = CreateButton("Reprint Last Bill", btnHeight);

            // Dock buttons to top so they fill horizontally
            btnConfirmBill.Dock = DockStyle.Top;
            btnBillReprint.Dock = DockStyle.Top;
            btnBillModify.Dock = DockStyle.Top;
            btnReprintLastBill.Dock = DockStyle.Top;

            // Add buttons in reverse order so they appear in correct vertical order
            this.Controls.Add(btnReprintLastBill);
            this.Controls.Add(btnBillModify);
            this.Controls.Add(btnBillReprint);
            this.Controls.Add(btnConfirmBill);

            // Optionally set a minimum width
            this.MinimumSize = new Size(180, totalHeight);
        }

        private Button CreateButton(string text, int height)
        {
            return new Button
            {
                Text = text,
                Height = height,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Standard
            };
        }

        // Handler for the "Reprint Last Bill" button
        private void BtnReprintLastBill_Click(object sender, EventArgs e)
        {
            try
            {
                Bill lastBill = null;

                // Prefer the exact bill associated with the last KOT if available
                try
                {
                    if (!string.IsNullOrEmpty(KoTState.LastBillId))
                    {
                        lastBill = DBHelper.GetBillById(KoTState.LastBillId);
                    }
                }
                catch
                {
                    // ignore
                }

                // Fallback to DB's last bill if we couldn't fetch by ID
                if (lastBill == null)
                    lastBill = DBHelper.GetLastBill();

                if (lastBill == null)
                {
                    MessageBox.Show("No previous bill found.", "Reprint Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // If we have a saved KoT for the last printed bill, ensure it is attached to the bill
                try
                {
                    if (!string.IsNullOrEmpty(KoTState.LastKoTNumber))
                    {
                        lastBill.KoTNumber = KoTState.LastKoTNumber;
                    }
                }
                catch
                {
                    // ignore state read errors
                }

                // Send to printer
                BillPrinter.PrintBill(lastBill);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to reprint bill: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Preserve existing event behavior for other subscribers
            ReprintLastBillClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnBillModify_Click(object sender, EventArgs e)
        {
            BillModifyClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnBillReprint_Click(object sender, EventArgs e)
        {
            try
            {
                using var billListForm = new BillListControls();
                var result = billListForm.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return; // user cancelled
                }

                // Retrieve selected bill from the dialog (private field)
                var selectedBillField = billListForm.GetType().GetField("selectedBill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var selectedBill = selectedBillField?.GetValue(billListForm) as Bill;
                if (selectedBill == null)
                {
                    MessageBox.Show("No bill selected.", "Reprint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Print the selected bill
                // If this selected bill is the last printed bill that had a KOT, attach that KOT number
                try
                {
                    if (!string.IsNullOrEmpty(KoTState.LastBillId) && KoTState.LastBillId == selectedBill.BillId)
                    {
                        selectedBill.KoTNumber = KoTState.LastKoTNumber;
                    }
                }
                catch
                {
                    // ignore
                }

                BillPrinter.PrintBill(selectedBill);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to reprint bill: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
