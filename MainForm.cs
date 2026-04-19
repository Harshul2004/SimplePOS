using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace SimplePOS
{
    public partial class MainForm : Form
    {
        private Panel pnlCategories = new Panel();
        private Panel pnlSubCategories = new Panel();
        private Panel pnlItems = new Panel();

        private int categoryStartIndex = 0;
        private int categoriesPerPage = 5;
        private List<(int ID, string Name)> allCategories;

        private int subCategoryStartIndex = 0;
        private int subCategoriesPerPage = 10;
        private List<(int ID, string Name)> currentSubCategories;

        private List<(int ID, string Name, double Price)> currentItems;

        // Admin button
        private Button btnAdminPanel = new Button();

        // Navigation buttons
        private Button btnPrevSubCat = new Button();
        private Button btnNextSubCat = new Button();
        private Button btnUpItems = new Button();
        private Button btnDownItems = new Button();
        private Button btnUpCart = new Button();
        private Button btnDownCart = new Button();

        // Cart action buttons
        private Button btnClearCart = new Button();
        private Button btnDeleteLast = new Button();
        private Button btnQuantity = new Button();
        private Button btnExtraFoodAction = new Button();

        // Add Search button field
        private Button btnSearch = new Button();

        private Dictionary<string, (int qty, double price)> cart = new();

        private int itemRowStartIndex = 0;
        private int cartRowStartIndex = 0;
        private int cartRowsPerPage = 2;

        private Label lblTotal = new Label();
        private Label lblCashGiven = new Label();
        private Label lblChange = new Label();
        private Label lblBillNo = new Label();
        private Label lblLastBill = new Label();
        private Label lblLastBillNo = new Label();
        private TextBox txtTotal = new TextBox();
        private TextBox txtCashGiven = new TextBox();
        private TextBox txtChange = new TextBox();
        private TextBox txtBillNo = new TextBox();
        private TextBox txtLastBill = new TextBox();
        private TextBox txtLastBillNo = new TextBox();
        private KoTButtonsControl kotButtonsControl = new KoTButtonsControl();
        private ChangeCounterControl changeCounterControl = new ChangeCounterControl();

        // Add this field to MainForm
        private BillActionsControl billActionsControl = new BillActionsControl();

        // Cached menu data
        private List<(int ID, string Name)> cachedCategories;
        private List<(int ID, string Name, int CategoryID)> cachedSubCategories;
        private List<(int ID, string Name, double Price, int SubCategoryID)> cachedMenuItems;

        // At the top of MainForm class:
        private string editingBillId = null;

        // Add a label for the brand
        private Label lblBrand = new Label();

        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Prevent resizing
            this.MaximizeBox = false; // Disable maximize/restore button
            this.MinimizeBox = true; // Allow minimizing
            this.ControlBox = true; // Show close/minimize
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized; // Normal maximized, taskbar visible

            // Subscribe to ChangeCounterControl cash value event
            changeCounterControl.CashValueChanged += ChangeCounterControl_CashValueChanged;

            billActionsControl.BillModifyClicked += (s, e) =>
            {
                using var billListForm = new BillListControls();
                var result = billListForm.ShowDialog(this);
                if (result == DialogResult.OK && billListForm.SelectedBill != null)
                {
                    // Load selected bill into the cart for editing
                    LoadBillForEditing(billListForm.SelectedBill);
                }
            };
            billActionsControl.BillReprintClicked += (s, e) =>
            {
                using var billListForm = new BillListControls();
                var result = billListForm.ShowDialog(this);
                // Only proceed if user clicked Ok and a bill is selected
                var selectedBillField = billListForm.GetType().GetField("selectedBill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var selectedBill = selectedBillField?.GetValue(billListForm) as Bill;
                if (result == DialogResult.OK && selectedBill != null)
                {
                    // Reprint only the bill (not KoT)
                    string billDetails = $"Bill No: {selectedBill.BillId}\nDate: {selectedBill.Date} {selectedBill.Time}\nTotal: ₹{selectedBill.TotalAmount:0.00}\nItems: {selectedBill.Items}";
                    MessageBox.Show(billDetails, "Reprint Bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // TODO: Replace with actual print logic if needed
                }
            };
            billActionsControl.btnConfirmBill.Click += (s, e) =>
            {
                ConfirmOrUpdateBill();
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Run dynamic panel/layout initialization after form is created
            InitializeDynamicPanels();

            // Load all menu data into cache
            DBHelper.EnsureDatabase();
            cachedCategories = DBHelper.GetCategories();
            cachedSubCategories = DBHelper.GetAllSubCategories();
            cachedMenuItems = DBHelper.GetAllMenuItems();

            allCategories = cachedCategories;
            ShowCategories();
            ShowLastBillDetails();

            // Defensive ResizeLayout call
            if (this.IsHandleCreated)
            {
                try
                {
                    ResizeLayout();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MainForm.ResizeLayout exception: {ex}");
                }
            }

            this.Resize += (s, ev) =>
            {
                if (this.IsHandleCreated)
                {
                    try
                    {
                        ResizeLayout();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MainForm.ResizeLayout exception: {ex}");
                    }
                }
            };
        }

        // Handler for cash value change event
        private void ChangeCounterControl_CashValueChanged(object sender, int cashValue)
        {
            if (cashValue == 0)
            {
                txtCashGiven.Text = "0";
                txtChange.Text = "0";
                return;
            }
            txtCashGiven.Text = cashValue.ToString();
            // Calculate change: cash - total
            if (double.TryParse(txtTotal.Text, out double total))
            {
                double change = cashValue - total;
                txtChange.Text = change.ToString("0.00");
            }
            else
            {
                txtChange.Text = "";
            }
        }

        // Remove MainForm_ForceMaximized event handler
        // Remove this.Resize += MainForm_ForceMaximized;

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

        private void InitializeDynamicPanels()
        {
            int padding = 12;
            int rowHeight = 90;
            int cartHeight = 220;
            int navBtnWidth = 100;
            int navBtnSpacing = 6;
            int navTotalWidth = navBtnWidth * 2 + navBtnSpacing;

            // Categories panel (row 1)
            pnlCategories.Left = padding;
            pnlCategories.Top = padding;
            pnlCategories.Width = this.ClientSize.Width - 2 * padding;
            pnlCategories.Height = rowHeight;
            pnlCategories.BackColor = Color.WhiteSmoke;
            pnlCategories.AutoScroll = false;
            this.Controls.Add(pnlCategories);

            // Subcategories panel (row 2) - leave space for nav buttons
            pnlSubCategories.Left = padding;
            pnlSubCategories.Top = pnlCategories.Top + pnlCategories.Height;
            pnlSubCategories.Width = this.ClientSize.Width - 2 * padding - navTotalWidth;
            pnlSubCategories.Height = rowHeight;
            pnlSubCategories.BackColor = Color.WhiteSmoke;
            pnlSubCategories.AutoScroll = false;
            this.Controls.Add(pnlSubCategories);

            // Previous button for subcategories
            btnPrevSubCat.Text = "Previous";
            btnPrevSubCat.Width = navBtnWidth;
            btnPrevSubCat.Height = rowHeight - 10;
            btnPrevSubCat.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnPrevSubCat.BackColor = Color.LightGray;
            btnPrevSubCat.Click += BtnPrevSubCat_Click;
            btnPrevSubCat.Visible = true;
            this.Controls.Add(btnPrevSubCat);

            // Next button for subcategories
            btnNextSubCat.Text = "Next";
            btnNextSubCat.Width = navBtnWidth;
            btnNextSubCat.Height = rowHeight - 10;
            btnNextSubCat.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnNextSubCat.BackColor = Color.LightGray;
            btnNextSubCat.Click += BtnNextSubCat_Click;
            btnNextSubCat.Visible = true;
            this.Controls.Add(btnNextSubCat);

            // Food items panel (row 3)
            pnlItems.Left = padding;
            pnlItems.Top = pnlSubCategories.Top + pnlSubCategories.Height;
            // Reduce width so button column is fully visible
            pnlItems.Width = this.ClientSize.Width - navBtnWidth - navBtnSpacing - 2 - padding;
            pnlItems.Height = (int)(this.ClientSize.Height * 0.45);
            pnlItems.BackColor = Color.White;
            pnlItems.AutoScroll = false;
            this.Controls.Add(pnlItems);

            // Up button for food items
            btnUpItems.Text = "Up";
            btnUpItems.Width = navBtnWidth;
            btnUpItems.Height = rowHeight - 10; // Match Next/Previous buttons
            btnUpItems.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnUpItems.BackColor = Color.LightGray;
            btnUpItems.Click += BtnUpItems_Click;
            btnUpItems.Visible = true;
            this.Controls.Add(btnUpItems);

            // Down button for food items
            btnDownItems.Text = "Down";
            btnDownItems.Width = navBtnWidth;
            btnDownItems.Height = rowHeight - 10; // Match Next/Previous buttons
            btnDownItems.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDownItems.BackColor = Color.LightGray;
            btnDownItems.Click += BtnDownItems_Click;
            btnDownItems.Visible = true;
            this.Controls.Add(btnDownItems);

            // Extra button below Up/Down for food items (right edge)
            btnExtraFoodAction.Text = "Open Item (Food)";
            btnExtraFoodAction.Width = navBtnWidth;
            btnExtraFoodAction.Height = 120; // Set to 80 for uniformity
            btnExtraFoodAction.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnExtraFoodAction.BackColor = Color.FromArgb(255, 255, 240);
            btnExtraFoodAction.Visible = true;
            btnExtraFoodAction.Click += BtnExtraFoodAction_Click;
            this.Controls.Add(btnExtraFoodAction);

            // --- Add Search button below Open Item (Food) and above Admin Panel ---
            btnSearch.Text = "Search";
            btnSearch.Width = navBtnWidth;
            btnSearch.Height = 80; // Match height
            btnSearch.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSearch.BackColor = Color.FromArgb(230, 255, 230);
            btnSearch.Visible = true;
            btnSearch.Click += BtnSearch_Click;
            this.Controls.Add(btnSearch);

            // Admin Panel button (bottom-right)
            btnAdminPanel.Width = navBtnWidth;
            btnAdminPanel.Height = 80; // Match height
            btnAdminPanel.Text = "Admin\nPanel";
            btnAdminPanel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            btnAdminPanel.BackColor = Color.FromArgb(230, 240, 255);
            btnAdminPanel.ForeColor = Color.Black;
            btnAdminPanel.FlatStyle = FlatStyle.Standard;
            btnAdminPanel.FlatAppearance.BorderSize = 1;
            btnAdminPanel.FlatAppearance.BorderColor = Color.LightGray;
            btnAdminPanel.UseCompatibleTextRendering = true; // for \n support
            btnAdminPanel.Cursor = Cursors.Hand;
            btnAdminPanel.Click += btnAdminPanel_Click;
            this.Controls.Add(btnAdminPanel);

            // Up button for cart listbox
            btnUpCart.Text = "Up";
            btnUpCart.Width = 100;
            btnUpCart.Height = 56;
            btnUpCart.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnUpCart.BackColor = Color.LightGray;
            btnUpCart.Click += BtnUpCart_Click;
            btnUpCart.Visible = true;
            btnUpCart.Enabled = false;
            this.Controls.Add(btnUpCart);

            // Down button for cart listbox
            btnDownCart.Text = "Down";
            btnDownCart.Width = 100;
            btnDownCart.Height = 56;
            btnDownCart.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDownCart.BackColor = Color.LightGray;
            btnDownCart.Click += BtnDownCart_Click;
            btnDownCart.Visible = true;
            btnDownCart.Enabled = false;
            this.Controls.Add(btnDownCart);

            // Clear Cart button
            btnClearCart.Text = "Clear";
            btnClearCart.Width = 100;
            btnClearCart.Height = 56;
            btnClearCart.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnClearCart.BackColor = Color.FromArgb(255, 200, 200); // Light red
            btnClearCart.ForeColor = Color.Black; // Pure black text
            btnClearCart.FlatStyle = FlatStyle.Standard;
            btnClearCart.Click += BtnClearCart_Click;
            btnClearCart.Visible = true;
            this.Controls.Add(btnClearCart);

            // Delete button
            btnDeleteLast.Text = "Delete";
            btnDeleteLast.Width = 100;
            btnDeleteLast.Height = 56;
            btnDeleteLast.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDeleteLast.BackColor = Color.LightGray;
            btnDeleteLast.ForeColor = Color.Black;
            btnDeleteLast.FlatStyle = FlatStyle.Standard;
            btnDeleteLast.Click += BtnDeleteLast_Click;
            btnDeleteLast.Visible = true;
            this.Controls.Add(btnDeleteLast);

            // Quantity button
            btnQuantity.Text = "Quantity";
            btnQuantity.Width = 100;
            btnQuantity.Height = 56;
            btnQuantity.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnQuantity.BackColor = Color.LightGray;
            btnQuantity.ForeColor = Color.Black;
            btnQuantity.FlatStyle = FlatStyle.Standard;
            btnQuantity.Visible = true;
            // Position Quantity button just below Delete button
            btnQuantity.Left = btnDeleteLast.Left;
            btnQuantity.Top = btnDeleteLast.Bottom + 6;
            this.Controls.Add(btnQuantity);
            btnQuantity.Click += BtnQuantity_Click;

            // Add summary labels and textboxes
            int summaryLeft = btnUpCart.Right + 16;
            int summaryWidth = 160;
            int summaryHeight = 36;
            int summarySpacing = 12;
            int summaryLabelWidth = 90;
            int summaryTextBoxWidth = 70;
            int summaryTop = btnUpCart.Top;

            lblTotal.Text = "Total Amount";
            lblTotal.Font = new Font("Segoe UI", 25F, FontStyle.Bold);
            lblTotal.Width = 250;
            lblTotal.Height = 50;
            lblTotal.Left = summaryLeft;
            lblTotal.Top = summaryTop;
            lblTotal.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblTotal);

            txtTotal.ReadOnly = true;
            txtTotal.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            txtTotal.Width = 235;
            txtTotal.Height = 70;
            txtTotal.Left = lblTotal.Left;
            txtTotal.Top = lblTotal.Bottom + 8; // stack vertically
            txtTotal.TextAlign = HorizontalAlignment.Center;
            this.Controls.Add(txtTotal);

            lblCashGiven.Text = "Cash Given:";
            lblCashGiven.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblCashGiven.Width = summaryLabelWidth;
            lblCashGiven.Height = summaryHeight;
            lblCashGiven.Left = summaryLeft;
            lblCashGiven.Top = txtTotal.Bottom + summarySpacing;
            this.Controls.Add(lblCashGiven);

            txtCashGiven.ReadOnly = true;
            txtCashGiven.Font = new Font("Segoe UI", 14F);
            txtCashGiven.Width = 140;
            txtCashGiven.Height = summaryHeight;
            txtCashGiven.Left = lblCashGiven.Right + 4;
            txtCashGiven.Top = lblCashGiven.Top;
            txtCashGiven.Text = "";
            this.Controls.Add(txtCashGiven);

            lblChange.Text = "Change:";
            lblChange.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblChange.Width = summaryLabelWidth;
            lblChange.Height = summaryHeight;
            lblChange.Left = summaryLeft;
            lblChange.Top = lblCashGiven.Bottom + summarySpacing;
            this.Controls.Add(lblChange);

            txtChange.ReadOnly = true;
            txtChange.Font = new Font("Segoe UI", 14F);
            txtChange.Width = 140;
            txtChange.Height = summaryHeight;
            txtChange.Left = lblChange.Right + 4;
            txtChange.Top = lblChange.Top;
            txtChange.Text = "";
            this.Controls.Add(txtChange);

            // Bill No. label and textbox
            lblBillNo.Text = "Bill No.";
            lblBillNo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblBillNo.Width = 90;
            lblBillNo.Height = 36;
            lblBillNo.Left = lblChange.Left;
            lblBillNo.Top = lblChange.Bottom + 12;
            this.Controls.Add(lblBillNo);

            txtBillNo.ReadOnly = true;
            txtBillNo.Font = new Font("Segoe UI", 14F);
            txtBillNo.Width = 140;
            txtBillNo.Height = 36;
            txtBillNo.Left = lblBillNo.Right + 4;
            txtBillNo.Top = lblBillNo.Top;
            txtBillNo.Text = "";
            this.Controls.Add(txtBillNo);

            // Last Bill label and textbox
            lblLastBill.Text = "Last Bill";
            lblLastBill.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblLastBill.Width = 90;
            lblLastBill.Height = 36;
            lblLastBill.Left = lblBillNo.Left;
            lblLastBill.Top = lblBillNo.Bottom + 12;
            this.Controls.Add(lblLastBill);

            txtLastBill.ReadOnly = true;
            txtLastBill.Font = new Font("Segoe UI", 14F);
            txtLastBill.Width = 140;
            txtLastBill.Height = 36;
            txtLastBill.Left = lblLastBill.Right + 4;
            txtLastBill.Top = lblLastBill.Top;
            txtLastBill.Text = "";
            this.Controls.Add(txtLastBill);

            // Last bill No. label and textbox
            lblLastBillNo.Text = "Last Bill";
            lblLastBillNo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblLastBillNo.Width = 90;
            lblLastBillNo.Height = 36;
            lblLastBillNo.Left = lblLastBill.Left;
            lblLastBillNo.Top = lblLastBill.Bottom + 12;
            this.Controls.Add(lblLastBillNo);

            txtLastBillNo.ReadOnly = true;
            txtLastBillNo.Font = new Font("Segoe UI", 14F);
            txtLastBillNo.Width = 140;
            txtLastBillNo.Height = 36;
            txtLastBillNo.Left = lblLastBillNo.Right + 4;
            txtLastBillNo.Top = lblLastBillNo.Top;
            txtLastBillNo.Text = "";
            this.Controls.Add(txtLastBillNo);

            // Add KoTButtonsControl to the form
            kotButtonsControl.Visible = true;
            kotButtonsControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            kotButtonsControl.KoTBillClicked += KotButtonsControl_KoTBillClicked;
            this.Controls.Add(kotButtonsControl);

            // Add ChangeCounterControl to the form
            changeCounterControl.Visible = true;
            changeCounterControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            this.Controls.Add(changeCounterControl);

            // Add BillActionsControl to the form (bottom-right, position set in ResizeLayout)
            billActionsControl.Visible = true;
            this.Controls.Add(billActionsControl);
        }

        private void ResizeLayout()
        {
            int padding = 12;
            int rowHeight = 90;
            int cartHeight = 220;
            int navBtnWidth = 100;
            int navBtnSpacing = 6;
            int navTotalWidth = navBtnWidth * 2 + navBtnSpacing;

            pnlCategories.Left = padding;
            pnlCategories.Top = padding;
            pnlCategories.Width = this.ClientSize.Width - 2 * padding;
            pnlCategories.Height = rowHeight;

            pnlSubCategories.Left = padding;
            pnlSubCategories.Top = pnlCategories.Top + pnlCategories.Height;
            pnlSubCategories.Width = this.ClientSize.Width - 2 * padding - navTotalWidth;
            pnlSubCategories.Height = rowHeight;

            // Position Previous/Next buttons just outside the right edge of subcategory row
            btnPrevSubCat.Top = pnlSubCategories.Top;
            btnPrevSubCat.Left = pnlSubCategories.Right + navBtnSpacing;
            btnPrevSubCat.Height = pnlSubCategories.Height;
            btnPrevSubCat.Visible = true;

            btnNextSubCat.Top = pnlSubCategories.Top;
            btnNextSubCat.Left = btnPrevSubCat.Right + navBtnSpacing;
            btnNextSubCat.Height = pnlSubCategories.Height;
            btnNextSubCat.Visible = true;

            pnlItems.Left = padding;
            pnlItems.Top = pnlSubCategories.Top + pnlSubCategories.Height;
            // Reduce width so button column is fully visible
            pnlItems.Width = this.ClientSize.Width - navBtnWidth - navBtnSpacing - 2 - padding;
            pnlItems.Height = (int)(this.ClientSize.Height * 0.45);

            // Align Up/Down buttons with Next button
            btnUpItems.Left = btnNextSubCat.Left;
            btnUpItems.Top = pnlItems.Top + 10;
            btnUpItems.Height = rowHeight - 10;
            btnUpItems.Width = navBtnWidth;
            btnUpItems.Visible = true;

            btnDownItems.Left = btnNextSubCat.Left;
            btnDownItems.Top = btnUpItems.Bottom + navBtnSpacing;
            btnDownItems.Height = rowHeight - 10;
            btnDownItems.Width = navBtnWidth;
            btnDownItems.Visible = true;

            // Position Extra button below Down button
            btnExtraFoodAction.Left = btnNextSubCat.Left;
            btnExtraFoodAction.Top = btnDownItems.Bottom + navBtnSpacing;
            btnExtraFoodAction.Height = 120; // Uniform height
            btnExtraFoodAction.Width = navBtnWidth;
            btnExtraFoodAction.Visible = true;

            // Position Search button below Open Item (Food) and above Admin Panel
            btnSearch.Left = btnExtraFoodAction.Left;
            btnSearch.Top = btnExtraFoodAction.Bottom + navBtnSpacing;
            btnSearch.Width = navBtnWidth;
            btnSearch.Height = 80; // Uniform height
            btnSearch.Visible = true;

            // Position Admin Panel button directly below Search button
            btnAdminPanel.Left = btnSearch.Left;
            btnAdminPanel.Top = btnSearch.Bottom + navBtnSpacing;
            btnAdminPanel.Width = navBtnWidth;
            btnAdminPanel.Height = 80; // Uniform height
            btnAdminPanel.Visible = true;

            listBox1.Width = (int)((this.ClientSize.Width - 2 * padding) * 0.35);
            listBox1.Left = padding;
            listBox1.Top = pnlItems.Top + pnlItems.Height + padding;
            listBox1.Height = this.ClientSize.Height - listBox1.Top - padding;
            listBox1.IntegralHeight = false; // Fixed size, does not resize with content

            // Position Up/Down buttons for cart listbox
            btnUpCart.Left = listBox1.Right + 6;
            btnUpCart.Top = listBox1.Top;
            btnUpCart.Width = 100;
            btnUpCart.Height = 56;
            btnUpCart.Visible = true;

            btnDownCart.Left = listBox1.Right + 6;
            btnDownCart.Top = btnUpCart.Bottom + 6;
            btnDownCart.Width = 100;
            btnDownCart.Height = 56;
            btnDownCart.Visible = true;

            // Position Clear Cart button
            btnClearCart.Left = btnDownCart.Left;
            btnClearCart.Top = btnDownCart.Bottom + 6;
            btnClearCart.Width = 100;
            btnClearCart.Height = 68;
            btnClearCart.Visible = true;

            // Position Delete button
            btnDeleteLast.Left = btnClearCart.Left;
            btnDeleteLast.Top = btnClearCart.Bottom + 6;
            btnDeleteLast.Width = 100;
            btnDeleteLast.Height = 68;
            btnDeleteLast.Visible = true;

            // Position Quantity button just below Delete button
            btnQuantity.Left = btnDeleteLast.Left;
            btnQuantity.Top = btnDeleteLast.Bottom + 6;
            btnQuantity.Width = 100;
            btnQuantity.Height = 95;
            btnQuantity.Visible = true;

            // Position summary labels and textboxes
            int summaryLeft = btnUpCart.Right + 16;
            int summaryWidth = 160;
            int summaryHeight = 36;
            int summarySpacing = 12;
            int summaryLabelWidth = 90;
            int summaryTextBoxWidth = 70;
            int summaryTop = btnUpCart.Top;

            lblTotal.Left = summaryLeft;
            lblTotal.Top = summaryTop;
            txtTotal.Left = lblTotal.Left;
            txtTotal.Top = lblTotal.Bottom + 8;

            lblCashGiven.Left = summaryLeft;
            lblCashGiven.Top = txtTotal.Bottom + summarySpacing;
            txtCashGiven.Left = lblCashGiven.Right + 4;
            txtCashGiven.Top = lblCashGiven.Top;

            lblChange.Left = summaryLeft;
            lblChange.Top = lblCashGiven.Bottom + summarySpacing;
            txtChange.Left = lblChange.Right + 4;
            txtChange.Top = lblChange.Top;

            // Position Bill No. label and textbox
            lblBillNo.Left = lblChange.Left;
            lblBillNo.Top = lblChange.Bottom + 12;
            txtBillNo.Left = lblBillNo.Right + 4;
            txtBillNo.Top = lblBillNo.Top;

            // Position Last Bill label and textbox
            lblLastBill.Left = lblBillNo.Left;
            lblLastBill.Top = lblBillNo.Bottom + 12;
            txtLastBill.Left = lblLastBill.Right + 4;
            txtLastBill.Top = lblLastBill.Top;

            // Position Last bill No. label and textbox
            lblLastBillNo.Left = lblLastBill.Left;
            lblLastBillNo.Top = lblLastBill.Bottom + 12;
            txtLastBillNo.Left = lblLastBillNo.Right + 4;
            txtLastBillNo.Top = lblLastBillNo.Top;

            // Position KoTButtonsControl to the right of summary info, spanning from lblTotal to txtLastBill
            int kotLeft = txtTotal.Right + 32; // 32px right of summary info
            int kotTop = lblTotal.Top;
            int kotHeight = this.ClientSize.Height - kotTop - padding; // Stretch to bottom edge
            kotButtonsControl.Left = kotLeft;
            kotButtonsControl.Top = summaryTop; // Use the same top as summary labels
            kotButtonsControl.Width = 160;
            kotButtonsControl.Height = kotHeight;
            kotButtonsControl.Visible = true;

            // Position ChangeCounterControl to the right of KoTButtonsControl
            int changeCounterLeft = kotButtonsControl.Right + 32; // 32px right of KoTButtonsControl
            int changeCounterTop = kotButtonsControl.Top;
            int changeCounterWidth = 200;
            int changeCounterHeight = kotButtonsControl.Height;
            changeCounterControl.Left = changeCounterLeft;
            changeCounterControl.Top = changeCounterTop;
            changeCounterControl.Width = changeCounterWidth;
            changeCounterControl.Height = changeCounterHeight;
            changeCounterControl.Visible = true;

            // Position BillActionsControl to the right of ChangeCounterControl, at the bottom right
            int billActionsLeft = changeCounterControl.Right + 32; // 32px right of ChangeCounterControl
            int billActionsTop = this.ClientSize.Height - billActionsControl.Height - 12; // 12px from bottom
            billActionsControl.Left = billActionsLeft;
            billActionsControl.Top = billActionsTop;
            billActionsControl.Width = 160;
            billActionsControl.Height = billActionsControl.Height;
            billActionsControl.Visible = true;

            int bottomEdge = this.ClientSize.Height - padding;
            kotButtonsControl.Top = summaryTop; // Use the same top as summary labels
            kotButtonsControl.Height = bottomEdge - kotButtonsControl.Top;

            changeCounterControl.Top = kotButtonsControl.Top;
            changeCounterControl.Height = kotButtonsControl.Height;
        }

        private void ShowCategories()
        {
            pnlCategories.Controls.Clear();
            int btnSize = 88, spacing = 4, x = 0, y = (pnlCategories.Height - btnSize) / 2, btnWidth = (int)(btnSize * 1.8);
            int end = Math.Min(categoryStartIndex + categoriesPerPage, allCategories.Count);
            for (int i = categoryStartIndex; i < end; i++)
            {
                var cat = allCategories[i];
                var btn = new Button
                {
                    Text = cat.Name,
                    Width = btnWidth,
                    Height = btnSize,
                    Left = x,
                    Top = y,
                    Tag = cat.ID,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    BackColor = Color.FromArgb(230, 240, 255)
                };
                btn.Click += CategoryButton_Click;
                pnlCategories.Controls.Add(btn);
                x += btnWidth + spacing;
            }
            // Add brand label to the rightmost area
            lblBrand.Text = "Bhagat Ji Fast Food";
            lblBrand.Font = new Font("Segoe UI", 34F, FontStyle.Bold);
            lblBrand.AutoSize = true;
            lblBrand.ForeColor = Color.Black;
            // Place brand near the top of the categories row (small top padding)
            lblBrand.Top = 8; // moved up from vertical center
            lblBrand.Left = pnlCategories.Width - lblBrand.Width - 350; // 12px padding from right
            lblBrand.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (!pnlCategories.Controls.Contains(lblBrand))
                pnlCategories.Controls.Add(lblBrand);
            btnPrevSubCat.Enabled = btnNextSubCat.Enabled = btnUpItems.Enabled = btnDownItems.Enabled = false;
        }

        private void CategoryButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int categoryId)
            {
                // Filter subcategories from cache
                currentSubCategories = cachedSubCategories
                    .Where(sub => sub.CategoryID == categoryId)
                    .Select(sub => (sub.ID, sub.Name)).ToList();
                subCategoryStartIndex = 0;
                ShowSubCategories();
            }
        }

        private void BtnPrevSubCat_Click(object sender, EventArgs e)
        {
            if (subCategoryStartIndex > 0)
            {
                subCategoryStartIndex = Math.Max(0, subCategoryStartIndex - subCategoriesPerPage);
                ShowSubCategories();
            }
        }

        private void BtnNextSubCat_Click(object sender, EventArgs e)
        {
            if (currentSubCategories != null && subCategoryStartIndex + subCategoriesPerPage < currentSubCategories.Count)
            {
                subCategoryStartIndex += subCategoriesPerPage;
                ShowSubCategories();
            }
        }

        // Flickerless paging: double buffering for panels
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            pnlSubCategories.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(pnlSubCategories, true, null);
            pnlItems.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(pnlItems, true, null);
        }

        // Flickerless paging: reusable button lists
        private List<Button> subCategoryButtons = new();
        private List<Button> itemButtons = new();
        private int itemRowsPerPage = 5; // Always show 5 rows per page

        private void ShowSubCategories()
        {
            pnlSubCategories.SuspendLayout();
            int btnSize = 88, spacing = 4, btnWidth = (int)(btnSize * 1.8);
            int y = (pnlSubCategories.Height - btnSize) / 2;
            int end = Math.Min(subCategoryStartIndex + subCategoriesPerPage, currentSubCategories?.Count ?? 0);

            // Create buttons only once
            if (subCategoryButtons.Count != subCategoriesPerPage)
            {
                pnlSubCategories.Controls.Clear();
                subCategoryButtons.Clear();
                for (int i = 0; i < subCategoriesPerPage; i++)
                {
                    var btn = new Button
                    {
                        Width = btnWidth,
                        Height = btnSize,
                        Top = y,
                        Left = i * (btnWidth + spacing),
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        BackColor = Color.FromArgb(240, 255, 240)
                    };
                    btn.Click += SubCategoryButton_Click;
                    subCategoryButtons.Add(btn);
                    pnlSubCategories.Controls.Add(btn);
                }
            }

            // Update button text/visibility
            for (int i = 0; i < subCategoriesPerPage; i++)
            {
                int idx = subCategoryStartIndex + i;
                if (currentSubCategories != null && idx < currentSubCategories.Count)
                {
                    var sub = currentSubCategories[idx];
                    subCategoryButtons[i].Text = sub.Name;
                    subCategoryButtons[i].Tag = sub.ID;
                    subCategoryButtons[i].Visible = true;
                }
                else
                {
                    subCategoryButtons[i].Visible = false;
                }
            }

            bool hasSubCats = currentSubCategories != null && currentSubCategories.Count > 0;
            btnPrevSubCat.Enabled = hasSubCats && subCategoryStartIndex > 0;
            btnNextSubCat.Enabled = hasSubCats && subCategoryStartIndex + subCategoriesPerPage < currentSubCategories.Count;
            btnUpItems.Enabled = btnDownItems.Enabled = false;
            pnlSubCategories.ResumeLayout();
        }

        private void SubCategoryButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int subCategoryId)
            {
                // Only reload items if subcategory changes
                // Filter menu items from cache
                currentItems = cachedMenuItems
                    .Where(item => item.SubCategoryID == subCategoryId)
                    .Select(item => (item.ID, item.Name, item.Price)).ToList();
                itemRowStartIndex = 0;
                ShowItems();
            }
        }

        private void ShowItems()
        {
            pnlItems.SuspendLayout();
            int btnSize = 88, spacing = 4, btnWidth = (int)(btnSize * 1.8);
            int columns = Math.Max(1, pnlItems.Width / (btnWidth + spacing));
            int visibleRows = itemRowsPerPage;
            int yStart = 10;
            int maxButtons = visibleRows * columns;

            // Create buttons only once
            if (itemButtons.Count != maxButtons)
            {
                pnlItems.Controls.Clear();
                itemButtons.Clear();
                for (int i = 0; i < maxButtons; i++)
                {
                    var btn = new Button
                    {
                        Width = btnWidth,
                        Height = btnSize,
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        BackColor = Color.FromArgb(255, 255, 240)
                    };
                    btn.Click += ItemButton_Click;
                    itemButtons.Add(btn);
                    pnlItems.Controls.Add(btn);
                }
            }

            // Update button text/visibility
            int startIndex = itemRowStartIndex * columns;
            for (int i = 0; i < itemButtons.Count; i++)
            {
                int idx = startIndex + i;
                if (currentItems != null && idx < currentItems.Count)
                {
                    var item = currentItems[idx];
                    itemButtons[i].Text = $"{item.Name}\n₹{item.Price}";
                    itemButtons[i].Tag = item;
                    int row = i / columns, col = i % columns;
                    itemButtons[i].Left = col * (btnWidth + spacing);
                    itemButtons[i].Top = yStart + row * (btnSize + spacing);
                    itemButtons[i].Visible = true;
                }
                else
                {
                    itemButtons[i].Visible = false;
                }
            }

            bool hasItems = currentItems != null && currentItems.Count > 0;
            int totalRows = hasItems ? (int)Math.Ceiling((double)currentItems.Count / columns) : 0;
            btnUpItems.Enabled = hasItems && itemRowStartIndex > 0;
            btnDownItems.Enabled = hasItems && itemRowStartIndex + visibleRows < totalRows;
            pnlItems.ResumeLayout();
        }

        private void BtnUpItems_Click(object sender, EventArgs e)
        {
            if (itemRowStartIndex > 0)
            {
                itemRowStartIndex = Math.Max(0, itemRowStartIndex - itemRowsPerPage);
                ShowItems();
            }
        }

        private void BtnDownItems_Click(object sender, EventArgs e)
        {
            int btnSize = 88, spacing = 6, btnWidth = (int)(btnSize * 1.8);
            int columns = Math.Max(1, pnlItems.Width / (btnWidth + spacing));
            int visibleRows = itemRowsPerPage;
            int totalRows = currentItems != null ? (int)Math.Ceiling((double)currentItems.Count / columns) : 0;
            int maxStartIndex = Math.Max(0, totalRows - visibleRows);
            if (itemRowStartIndex < maxStartIndex)
            {
                itemRowStartIndex = Math.Min(maxStartIndex, itemRowStartIndex + visibleRows);
                ShowItems();
            }
        }

        private void BtnUpCart_Click(object sender, EventArgs e)
        {
            if (cartRowStartIndex > 0)
            {
                cartRowStartIndex = Math.Max(0, cartRowStartIndex - cartRowsPerPage);
                RefreshCartListBox();
            }
        }

        private void BtnDownCart_Click(object sender, EventArgs e)
        {
            int visibleRows = GetCartVisibleRows();
            if (cartRowStartIndex + visibleRows < cart.Count)
            {
                cartRowStartIndex = Math.Min(cart.Count - visibleRows, cartRowStartIndex + cartRowsPerPage);
                RefreshCartListBox();
            }
        }

        private void BtnClearCart_Click(object sender, EventArgs e)
        {
            cart.Clear();
            cartRowStartIndex = 0;
            RefreshCartListBox();
            txtBillNo.Text = "";
            // Reset Cash and Change to 0
            txtCashGiven.Text = "0";
            txtChange.Text = "0";
        }

        private void BtnDeleteLast_Click(object sender, EventArgs e)
        {
            if (cart.Count > 0)
            {
                cart.Remove(new List<string>(cart.Keys)[cart.Count - 1]);
                cartRowStartIndex = Math.Max(0, Math.Min(cartRowStartIndex, cart.Count - 1));
                RefreshCartListBox();
            }
        }

        // Search button click event
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            // Step 1: Show KeyboardDialog for input
            using var keyboardDlg = new KeyboardDialog("Search Food Item");
            var keyboardResult = keyboardDlg.ShowDialog(this);
            if (keyboardResult == DialogResult.OK && !string.IsNullOrWhiteSpace(keyboardDlg.EnteredValue))
            {
                string searchText = keyboardDlg.EnteredValue.Trim();
                // Step 2: Show SearchItemDialog with initial search text
                using var searchDlg = new SearchItemDialog(cachedMenuItems.Select(i => (i.ID, i.Name, i.Price)).ToList(), searchText);
                var searchResult = searchDlg.ShowDialog(this);
                if (searchResult == DialogResult.OK && searchDlg.SelectedItem.HasValue)
                {
                    var item = searchDlg.SelectedItem.Value;
                    cart[item.Name] = cart.TryGetValue(item.Name, out var v) ? (v.qty + 1, item.Price) : (1, item.Price);
                    AutoScrollCart();
                    txtBillNo.Text = DBHelper.GetNextBillId();
                }
            }
        }

        private void ItemButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is ValueTuple<int, string, double> item)
            {
                string name = item.Item2;
                double price = item.Item3;
                cart[name] = cart.TryGetValue(name, out var v) ? (v.qty + 1, price) : (1, price);
                AutoScrollCart();
                txtBillNo.Text = DBHelper.GetNextBillId();
            }
        }

        private void AutoScrollCart()
        {
            int visibleRows = GetCartVisibleRows();
            if (cart.Count > visibleRows)
                cartRowStartIndex = Math.Max(0, cart.Count - visibleRows);
            RefreshCartListBox();
            if (listBox1.Items.Count > 2)
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private int GetCartVisibleRows() => (listBox1.Height / listBox1.ItemHeight) - 2;

        private void RefreshCartListBox()
        {
            listBox1.Items.Clear();
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.ItemHeight = 36;
            listBox1.Font = new Font("Consolas", 17F);
            string header = string.Format("{0,-5}{1,-25}{2,15}", "Qty", "Item Name", "Amount");
            listBox1.Items.Add(header);
            listBox1.Items.Add(new string('-', header.Length));
            int visibleRows = GetCartVisibleRows();
            var cartItems = new List<string>();
            foreach (var kvp in cart)
                cartItems.Add(string.Format("{0,-5}{1,-25}{2,15}", kvp.Value.qty, kvp.Key, $"₹{kvp.Value.price * kvp.Value.qty:0.##}"));
            int start = cartRowStartIndex, end = Math.Min(start + visibleRows, cartItems.Count);
            for (int i = start; i < end; i++)
                listBox1.Items.Add(cartItems[i]);
            listBox1.DrawItem -= ListBox1_DrawItem_Center;
            listBox1.DrawItem += ListBox1_DrawItem_Center;
            btnUpCart.Enabled = cartRowStartIndex > 0;
            btnDownCart.Enabled = cartRowStartIndex + visibleRows < cartItems.Count;
            txtTotal.Text = cart.Values.Sum(v => v.price * v.qty).ToString("0.00");
        }

        private void ListBox1_DrawItem_Center(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            string text = listBox1.Items[e.Index].ToString();
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var brush = new SolidBrush(e.ForeColor);
            e.Graphics.DrawString(text, e.Font, brush, e.Bounds, sf);
            e.DrawFocusRectangle();
        }

        private void btnAdminPanel_Click(object sender, EventArgs e)
        {
            using var pinForm = new Form
            {
                Text = "Admin Access",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Width = 350,
                Height = 200,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Font = new Font("Segoe UI", 11F)
            };
            var lbl = new Label { Text = "Please enter the Admin PIN to access the Admin Panel:", Left = 24, Top = 20, Width = 290, Height = 32 };
            var txt = new TextBox { Left = 24, Top = 60, Width = 290, UseSystemPasswordChar = true, TabIndex = 0, BorderStyle = BorderStyle.FixedSingle };
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 70, Top = 110, Width = 80, Height = 30, TabIndex = 1 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 180, Top = 110, Width = 80, Height = 30, TabIndex = 2 };
            pinForm.AcceptButton = btnOK;
            pinForm.CancelButton = btnCancel;
            pinForm.Controls.Add(lbl);
            pinForm.Controls.Add(txt);
            pinForm.Controls.Add(btnOK);
            pinForm.Controls.Add(btnCancel);
            pinForm.Load += (s, ev) => txt.Focus();
            if (pinForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txt.Text))
            {
                if (txt.Text == DBHelper.GetAdminPIN())
                    new AdminPanelForm().ShowDialog();
                else
                    MessageBox.Show("Incorrect PIN!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnQuantity_Click(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Cart is empty. Add an item first.", "No Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var dlg = new NumpadDialog("Enter Quantity");
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (int.TryParse(dlg.EnteredValue, out int qty) && qty > 0)
                {
                    var lastKey = new List<string>(cart.Keys)[cart.Count - 1];
                    cart[lastKey] = (qty, cart[lastKey].price);
                    RefreshCartListBox();
                }
            }
            // If Cancel/Close is pressed, do nothing
        }

        private void BtnExtraFoodAction_Click(object sender, EventArgs e)
        {
            using var dlg = new NumpadDialog("Open Item (Food)");
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (double.TryParse(dlg.EnteredValue, out double price) && price > 0)
                {
                    string itemName = $"Open Item ({price:0.##})";
                    cart[itemName] = (1, price);
                    AutoScrollCart();
                }
            }
            // If Cancel/Close is pressed, do nothing
        }

        private void KotButtonsControl_KoTBillClicked(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                // MessageBox.Show("Cart is empty. Add items before saving the bill.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string billId = DBHelper.GetNextBillId();
            var itemsList = cart.Select(kvp => new {
                Name = kvp.Key,
                Quantity = kvp.Value.qty,
                Price = kvp.Value.price
            }).ToList();
            string itemsJson = System.Text.Json.JsonSerializer.Serialize(itemsList);
            double total = cart.Values.Sum(v => v.price * v.qty);

            DBHelper.AddBill(billId, DateTime.Now, itemsJson, total);
            ShowLastBillDetails();

            txtBillNo.Text = billId;

            // --- PRINT BILL LOGIC ---
            var bill = new Bill
            {
                BillId = billId,
                Date = DateTime.Now.ToString("dd/MM/yyyy"),
                Time = DateTime.Now.ToString("HH:mm"),
                Items = itemsJson,
                TotalAmount = total
            };
            BillPrinter.PrintBill(bill);
            // --- END PRINT BILL LOGIC ---

            cart.Clear();
            RefreshCartListBox();
            txtBillNo.Text = ""; // Clear Bill No. after bill is generated
        }

        private void ShowLastBillDetails()
        {
            var lastBill = DBHelper.GetLastBill();
            if (lastBill != null)
            {
                txtLastBill.Text = lastBill.TotalAmount.ToString("0.00");
                txtLastBillNo.Text = lastBill.BillId;
            }
            else
            {
                txtLastBill.Text = "";
                txtLastBillNo.Text = "";
            }
        }

        // Add this public method to allow other controls to refresh last bill info
        public void RefreshLastBillDetails()
        {
            // Call existing private helper
            ShowLastBillDetails();
        }
        
        // Add this method to MainForm:
        private void LoadBillForEditing(Bill bill)
        {
            cart.Clear();

            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<BillItem>>(bill.Items);
                foreach (var item in items)
                {
                    cart[item.Name] = (item.Quantity, item.Price);
                }
            }
            catch
            {
                MessageBox.Show("Failed to load bill items for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtBillNo.Text = bill.BillId;
            editingBillId = bill.BillId;
            cartRowStartIndex = 0;
            RefreshCartListBox();
        }

        // Add this method to MainForm:
        private void ConfirmOrUpdateBill()
        {
            if (cart.Count == 0)
            {
                // MessageBox.Show("Cart is empty. Add items before saving the bill.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string billId = editingBillId ?? DBHelper.GetNextBillId();
            var itemsList = cart.Select(kvp => new {
                Name = kvp.Key,
                Quantity = kvp.Value.qty,
                Price = kvp.Value.price
            }).ToList();
            string itemsJson = System.Text.Json.JsonSerializer.Serialize(itemsList);
            double total = cart.Values.Sum(v => v.price * v.qty);

            DateTime billDateTime;
            if (editingBillId != null)
            {
                // Fetch original bill to retain its date and time
                var originalBill = DBHelper.GetBillById(billId);
                if (originalBill != null && DateTime.TryParseExact(originalBill.Date + " " + originalBill.Time, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                {
                    billDateTime = dt;
                }
                else
                {
                    billDateTime = DateTime.Now; // fallback if parsing fails
                }
                // Update existing bill
                DBHelper.UpdateBill(billId, billDateTime, itemsJson, total);

                // Build bill object for printing
                var updatedBill = new Bill
                {
                    BillId = billId,
                    Date = billDateTime.ToString("dd/MM/yyyy"),
                    Time = billDateTime.ToString("HH:mm"),
                    Items = itemsJson,
                    TotalAmount = total
                };

                // Try printing the updated bill
                try
                {
                    BillPrinter.PrintBill(updatedBill);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to print updated bill: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Create new bill with current date/time
                billDateTime = DateTime.Now;
                DBHelper.AddBill(billId, billDateTime, itemsJson, total);
            }

            ShowLastBillDetails();

            // Clear editing state and cart
            editingBillId = null;
            txtBillNo.Text = "";
            cart.Clear();
            RefreshCartListBox();
        }

        // Add this inside the MainForm class or as a private class in MainForm.cs
        private class BillItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public double Price { get; set; }
        }
    }
}
