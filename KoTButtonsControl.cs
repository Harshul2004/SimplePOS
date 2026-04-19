using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using System.Management;

namespace SimplePOS
{
    public partial class KoTButtonsControl : UserControl
    {
        private Button btnBillOnly;
        private Button btnKoTOnly;
        private Button btnKoTBill;
        private Button btnKoTInstructions;
        private Label lblDate;
        private System.Windows.Forms.Timer timer;
        // Store the last entered KoT instructions so they can be included when printing
        private string currentKoTInstructions = string.Empty;

        // Public events for button logic
        public event EventHandler KoTOnlyClicked;
        public event EventHandler KoTBillClicked;
        public event EventHandler<string> KoTInstructionsEntered;

        // Session-scoped KOT counter. Starts at 0 when app launches and increments for each KOT.
        // It is not persisted; it resets when the application exits.
        private static int sessionKoTCounter = 0;

        public KoTButtonsControl()
        {
            InitializeComponent();
            InitializeKoTButtons();
        }

        private void InitializeKoTButtons()
        {
            int spacing = 2;
            int btnCount = 4;

            btnBillOnly = CreateButton("Bill Only", 0, 0);
            btnBillOnly.Click += BtnBillOnly_Click;

            btnKoTOnly = CreateButton("KoT Only", 0, 0);
            btnKoTOnly.Click += BtnKoTOnly_Click;

            btnKoTBill = CreateButton("KoT + Bill", 0, 0);
            btnKoTBill.Click += BtnKoTBill_Click;

            btnKoTInstructions = CreateButton("KoT Instructions", 0, 0);
            btnKoTInstructions.Click += BtnKoTInstructions_Click;

            lblDate = new Label
            {
                Text = "Date: " + DateTime.Now.ToString("dd/MM/yyyy"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add buttons and label to the control
            this.Controls.Add(btnBillOnly);
            this.Controls.Add(btnKoTOnly);
            this.Controls.Add(btnKoTBill);
            this.Controls.Add(btnKoTInstructions);
            this.Controls.Add(lblDate);

            // Handle the Resize event
            this.Resize += KoTButtonsControl_Resize;
            KoTButtonsControl_Resize(this, EventArgs.Empty);

            // Timer for updating date
            timer = new System.Windows.Forms.Timer { Interval = 1000 * 60 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // Helper method for button creation
        private Button CreateButton(string text, int left, int top)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Standard,
                Left = left,
                Top = top,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblDate.Text = "Date: " + DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void KoTButtonsControl_Resize(object sender, EventArgs e)
        {
            int totalHeight = this.Height;
            int totalWidth = this.Width;

            // Height ratios
            int btnSpacing = 2;
            int btnCount = 4;
            int btnHeight = (int)((totalHeight - btnSpacing * (btnCount - 1)) * 0.90 / btnCount);
            int dateLabelHeight = totalHeight - (btnHeight * btnCount + btnSpacing * (btnCount - 1));

            int currentTop = 0;

            btnBillOnly.Width = totalWidth;
            btnBillOnly.Height = btnHeight;
            btnBillOnly.Left = 0;
            btnBillOnly.Top = currentTop;
            currentTop += btnHeight + btnSpacing;

            btnKoTOnly.Width = totalWidth;
            btnKoTOnly.Height = btnHeight;
            btnKoTOnly.Left = 0;
            btnKoTOnly.Top = currentTop;
            currentTop += btnHeight + btnSpacing;

            btnKoTBill.Width = totalWidth;
            btnKoTBill.Height = btnHeight;
            btnKoTBill.Left = 0;
            btnKoTBill.Top = currentTop;
            currentTop += btnHeight + btnSpacing;

            btnKoTInstructions.Width = totalWidth;
            btnKoTInstructions.Height = btnHeight;
            btnKoTInstructions.Left = 0;
            btnKoTInstructions.Top = currentTop;
            currentTop += btnHeight + btnSpacing;

            lblDate.Width = totalWidth;
            lblDate.Height = dateLabelHeight;
            lblDate.Left = 0;
            lblDate.Top = currentTop;
        }

        private void BtnKoTInstructions_Click(object sender, EventArgs e)
        {
            using (var keyboard = new KeyboardDialog("Enter KoT Instructions"))
            {
                if (keyboard.ShowDialog() == DialogResult.OK)
                {
                    string instructions = keyboard.EnteredValue;
                    // Save for later inclusion in printed KoT
                    currentKoTInstructions = instructions ?? string.Empty;
                    KoTInstructionsEntered?.Invoke(this, instructions);
                }
            }
        }

        private void BtnKoTOnly_Click(object sender, EventArgs e)
        {
            // Build a KoT object with current time and items gathered from MainForm's cart via reflection.
            var kot = new KoTPrinter.KoT
            {
                Time = DateTime.Now.ToString("HH:mm"),
                Items = "[]", // will replace below if we can read cart
                KoTNumber = GenerateKoTNumber(),
                Instructions = currentKoTInstructions // include latest instructions
            };

            // Save generated KoT number immediately so other code paths (MainForm handler, DB inserts) can reuse it for this session
            try
            {
                KoTState.LastKoTNumber = kot.KoTNumber ?? string.Empty;
                KoTState.LastKoTInstructions = kot.Instructions ?? string.Empty;
                KoTState.LastKoTItems = kot.Items ?? string.Empty;
            }
            catch { }

            // Try to find the running MainForm and retrieve its private 'cart' field without modifying MainForm.
            Form mainForm = null;
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f.GetType().Name == "MainForm")
                    {
                        mainForm = f;
                        break;
                    }
                }

                if (mainForm != null)
                {
                    var cartField = mainForm.GetType().GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (cartField != null)
                    {
                        var cartObj = cartField.GetValue(mainForm);
                        if (cartObj is System.Collections.IEnumerable enumerable)
                        {
                            var itemsList = new System.Collections.Generic.List<object>();
                            foreach (var kv in enumerable)
                            {
                                // kv is KeyValuePair<string, (int qty, double price>
                                var kvType = kv.GetType();
                                var key = kvType.GetProperty("Key")?.GetValue(kv)?.ToString() ?? "";
                                var value = kvType.GetProperty("Value")?.GetValue(kv);
                                if (value != null)
                                {
                                    var valType = value.GetType();
                                    // Value is a ValueTuple<int,double>
                                    object qtyObj = valType.GetProperty("Item1")?.GetValue(value) ?? valType.GetField("Item1")?.GetValue(value);
                                    object priceObj = valType.GetProperty("Item2")?.GetValue(value) ?? valType.GetField("Item2")?.GetValue(value);

                                    int qty = 1;
                                    if (qtyObj != null && int.TryParse(qtyObj.ToString(), out int q)) qty = q;

                                    double price = 0.0;
                                    if (priceObj != null && double.TryParse(priceObj.ToString(), out double p)) price = p;

                                    itemsList.Add(new { Name = key, Quantity = qty, Price = price });
                                }
                            }

                            if (itemsList.Count > 0)
                            {
                                kot.Items = JsonSerializer.Serialize(itemsList);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore and proceed with empty items
            }

            // Print KOT to both printers, fallback if kitchen fails
            PrintKoTToBothPrintersWithFallback(kot);

            // Clear instructions after printing so they are per-KOT only
            currentKoTInstructions = string.Empty;

            // After successful print, clear the cart on MainForm (if available) and refresh UI, without modifying MainForm directly.
            try
            {
                if (mainForm != null)
                {
                    var cartField = mainForm.GetType().GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (cartField != null)
                    {
                        var cartObj = cartField.GetValue(mainForm);
                        // Try to call Clear() if available
                        var clearMethod = cartObj?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (clearMethod != null)
                        {
                            clearMethod.Invoke(cartObj, null);
                        }
                        else
                        {
                            // Replace with a new empty instance of the same type if possible
                            var dictType = cartField.FieldType;
                            try
                            {
                                var empty = Activator.CreateInstance(dictType);
                                cartField.SetValue(mainForm, empty);
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    // Try to call RefreshCartListBox() if it exists
                    var refreshMethod = mainForm.GetType().GetMethod("RefreshCartListBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    refreshMethod?.Invoke(mainForm, null);

                    // Clear BillNo textbox if present
                    var txtBillField = mainForm.GetType().GetField("txtBillNo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (txtBillField != null)
                    {
                        var txtObj = txtBillField.GetValue(mainForm) as TextBox;
                        if (txtObj != null) txtObj.Text = string.Empty;
                    }
                }
            }
            catch
            {
                // ignore reflection errors
            }

            // Preserve external subscribers
            KoTOnlyClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnKoTBill_Click(object sender, EventArgs e)
        {
            // Build KoT and Bill from the current cart (gathered via reflection) and print KOT first, then Bill.
            var kot = new KoTPrinter.KoT
            {
                Time = DateTime.Now.ToString("HH:mm"),
                Items = "[]",
                KoTNumber = GenerateKoTNumber(),
                Instructions = currentKoTInstructions // include latest instructions
            };

            // Save generated KoT number immediately so other code paths (MainForm handler, DB inserts) can reuse it for this session
            try
            {
                KoTState.LastKoTNumber = kot.KoTNumber ?? string.Empty;
                KoTState.LastKoTInstructions = kot.Instructions ?? string.Empty;
                KoTState.LastKoTItems = kot.Items ?? string.Empty;
            }
            catch { }

            Bill billToPrint = null;
            Form mainForm = null;

            // Gather cart items via reflection (same approach as KoTOnly)
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f.GetType().Name == "MainForm")
                    {
                        mainForm = f;
                        break;
                    }
                }

                List<object> itemsList = new List<object>();
                double total = 0.0;
                if (mainForm != null)
                {
                    var cartField = mainForm.GetType().GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (cartField != null)
                    {
                        var cartObj = cartField.GetValue(mainForm);
                        if (cartObj is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var kv in enumerable)
                            {
                                var kvType = kv.GetType();
                                var key = kvType.GetProperty("Key")?.GetValue(kv)?.ToString() ?? "";
                                var value = kvType.GetProperty("Value")?.GetValue(kv);
                                if (value != null)
                                {
                                    var valType = value.GetType();
                                    object qtyObj = valType.GetProperty("Item1")?.GetValue(value) ?? valType.GetField("Item1")?.GetValue(value);
                                    object priceObj = valType.GetProperty("Item2")?.GetValue(value) ?? valType.GetField("Item2")?.GetValue(value);

                                    int qty = 1;
                                    if (qtyObj != null && int.TryParse(qtyObj.ToString(), out int q)) qty = q;

                                    double price = 0.0;
                                    if (priceObj != null && double.TryParse(priceObj.ToString(), out double p)) price = p;

                                    itemsList.Add(new { Name = key, Quantity = qty, Price = price });
                                    total += price * qty;
                                }
                            }
                        }
                    }
                }

                if (itemsList.Count > 0)
                {
                    kot.Items = JsonSerializer.Serialize(itemsList);

                    // Create Bill object and persist to DB (like MainForm does) before printing the bill
                    string billId = DBHelper.GetNextBillId();
                    string itemsJson = JsonSerializer.Serialize(itemsList);
                    billToPrint = new Bill
                    {
                        BillId = billId,
                        KoTNumber = kot.KoTNumber, // Pass KOT number to Bill
                        Date = DateTime.Now.ToString("dd/MM/yyyy"),
                        Time = DateTime.Now.ToString("HH:mm"),
                        Items = itemsJson,
                        TotalAmount = total
                    };

                    // Persist bill to DB so records match printed bill
                    try
                    {
                        DBHelper.AddBill(billId, DateTime.Now, itemsJson, total);
                    }
                    catch
                    {
                        // ignore DB errors for now; still attempt to print
                    }

                    // Notify main form to refresh last bill info
                    try
                    {
                        foreach (Form f2 in Application.OpenForms)
                        {
                            if (f2.GetType().Name == "MainForm")
                            {
                                var main = f2 as MainForm;
                                try { main?.RefreshLastBillDetails(); } catch { }
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // ignore and proceed with empty items/bill
            }

            // Print KOT to both printers, fallback if kitchen fails
            PrintKoTToBothPrintersWithFallback(kot);

            // Print Bill to Counter printer only
            var config = LoadPrinterConfig();
            if (!string.IsNullOrEmpty(config.CounterPrinterName) && billToPrint != null)
            {
                BillPrinter.PrintBill(billToPrint);
            }

            // Clear instructions after printing so they are per-KOT only
            currentKoTInstructions = string.Empty;

            // Clear cart and refresh UI (same approach as KoTOnly)
            try
            {
                if (mainForm != null)
                {
                    var cartField = mainForm.GetType().GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (cartField != null)
                    {
                        var cartObj = cartField.GetValue(mainForm);
                        var clearMethod = cartObj?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (clearMethod != null)
                        {
                            clearMethod.Invoke(cartObj, null);
                        }
                        else
                        {
                            var dictType = cartField.FieldType;
                            try
                            {
                                var empty = Activator.CreateInstance(dictType);
                                cartField.SetValue(mainForm, empty);
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    var refreshMethod = mainForm.GetType().GetMethod("RefreshCartListBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    refreshMethod?.Invoke(mainForm, null);

                    var txtBillField = mainForm.GetType().GetField("txtBillNo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (txtBillField != null)
                    {
                        var txtObj = txtBillField.GetValue(mainForm) as TextBox;
                        if (txtObj != null) txtObj.Text = string.Empty;
                    }
                }
            }
            catch
            {
                // ignore reflection errors
            }

            // Preserve external subscribers
            KoTBillClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnBillOnly_Click(object sender, EventArgs e)
        {
            // Print only the bill to the counter printer (not kitchen)
            Form mainForm = null;
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f.GetType().Name == "MainForm")
                    {
                        mainForm = f;
                        break;
                    }
                }

                if (mainForm != null)
                {
                    var cartField = mainForm.GetType().GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (cartField != null)
                    {
                        var cartObj = cartField.GetValue(mainForm);
                        if (cartObj is System.Collections.IEnumerable enumerable)
                        {
                            var itemsList = new System.Collections.Generic.List<object>();
                            double total = 0.0;
                            foreach (var kv in enumerable)
                            {
                                var kvType = kv.GetType();
                                var key = kvType.GetProperty("Key")?.GetValue(kv)?.ToString() ?? "";
                                var value = kvType.GetProperty("Value")?.GetValue(kv);
                                if (value != null)
                                {
                                    var valType = value.GetType();
                                    object qtyObj = valType.GetProperty("Item1")?.GetValue(value) ?? valType.GetField("Item1")?.GetValue(value);
                                    object priceObj = valType.GetProperty("Item2")?.GetValue(value) ?? valType.GetField("Item2")?.GetValue(value);

                                    int qty = 1;
                                    if (qtyObj != null && int.TryParse(qtyObj.ToString(), out int q)) qty = q;

                                    double price = 0.0;
                                    if (priceObj != null && double.TryParse(priceObj.ToString(), out double p)) price = p;

                                    itemsList.Add(new { Name = key, Quantity = qty, Price = price });
                                    total += price * qty;
                                }
                            }
                            if (itemsList.Count > 0)
                            {
                                string itemsJson = JsonSerializer.Serialize(itemsList);
                                string billId = DBHelper.GetNextBillId();
                                var bill = new Bill
                                {
                                    BillId = billId,
                                    Date = DateTime.Now.ToString("dd/MM/yyyy"),
                                    Time = DateTime.Now.ToString("HH:mm"),
                                    Items = itemsJson,
                                    TotalAmount = total
                                };
                                DBHelper.AddBill(billId, DateTime.Now, itemsJson, total);
                                BillPrinter.PrintBill(bill);

                                // Notify main form to refresh last bill info
                                try
                                {
                                    foreach (Form f2 in Application.OpenForms)
                                    {
                                        if (f2.GetType().Name == "MainForm")
                                        {
                                            var main = f2 as MainForm;
                                            try { main?.RefreshLastBillDetails(); } catch { }
                                            break;
                                        }
                                    }
                                }
                                catch { }

                                // After printing, clear the cart on MainForm and refresh UI, similar to other buttons
                                try
                                {
                                    // Attempt to clear the cart collection
                                    var clearMethod = cartObj?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (clearMethod != null)
                                    {
                                        clearMethod.Invoke(cartObj, null);
                                    }
                                    else
                                    {
                                        // Replace with a new empty instance of the same type if possible
                                        var dictType = cartField.FieldType;
                                        try
                                        {
                                            var empty = Activator.CreateInstance(dictType);
                                            cartField.SetValue(mainForm, empty);
                                        }
                                        catch
                                        {
                                            // ignore
                                        }
                                    }

                                    // Try to call RefreshCartListBox() if it exists
                                    var refreshMethod = mainForm.GetType().GetMethod("RefreshCartListBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                    refreshMethod?.Invoke(mainForm, null);

                                    // Clear BillNo textbox if present
                                    var txtBillField = mainForm.GetType().GetField("txtBillNo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                    if (txtBillField != null)
                                    {
                                        var txtObj = txtBillField.GetValue(mainForm) as TextBox;
                                        if (txtObj != null) txtObj.Text = string.Empty;
                                    }
                                }
                                catch
                                {
                                    // ignore reflection errors
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore errors
            }
        }

        private string GenerateKoTNumber()
        {
            // Increment session counter and return as KoT number (starts at 1 for first KOT this session)
            int num = Interlocked.Increment(ref sessionKoTCounter);
            return num.ToString();
        }

        // Helper: Load printer config from JSON
        private PrinterConfig LoadPrinterConfig()
        {
            try
            {
                string configPath = "printer_config.json";
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<PrinterConfig>(json) ?? new PrinterConfig();
                }
            }
            catch { }
            return new PrinterConfig();
        }

        // Check whether a printer appears available/online via WMI (Win32_Printer)
        private bool IsPrinterAvailable(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer"))
                {
                    foreach (ManagementObject printer in searcher.Get())
                    {
                        var nameObj = printer["Name"];
                        if (nameObj == null) continue;
                        string name = nameObj.ToString();
                        if (!string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // WorkOffline indicates printer configured as offline
                        try
                        {
                            var workOfflineObj = printer["WorkOffline"];
                            if (workOfflineObj != null && bool.TryParse(workOfflineObj.ToString(), out bool workOffline) && workOffline)
                                return false;
                        }
                        catch { }

                        // PrinterStatus: try to interpret common offline code (7) as offline
                        try
                        {
                            var statusObj = printer["PrinterStatus"];
                            if (statusObj != null && int.TryParse(statusObj.ToString(), out int status))
                            {
                                // 7 is reported as Offline in some systems; treat it as unavailable
                                if (status == 7) return false;
                            }
                        }
                        catch { }

                        // DetectedErrorState or other flags could indicate problems; treat as available if not explicitly offline
                        return true;
                    }
                }
            }
            catch
            {
                // If WMI query fails, assume unavailable to avoid spooling
                return false;
            }

            // Printer not found
            return false;
        }

        // Helper: Print KOT to both printers, fallback if kitchen fails
        private void PrintKoTToBothPrintersWithFallback(KoTPrinter.KoT kot)
        {
            var config = LoadPrinterConfig();
            bool kitchenPrinted = false;
            Exception kitchenEx = null;

            // Try Kitchen printer first if configured and appears available
            if (!string.IsNullOrEmpty(config.KitchenPrinterName) && IsPrinterAvailable(config.KitchenPrinterName))
            {
                try
                {
                    var kotKitchen = new KoTPrinter.KoT
                    {
                        Time = kot.Time,
                        Items = kot.Items,
                        KoTNumber = kot.KoTNumber,
                        Instructions = kot.Instructions
                    };
                    // Override printer for kitchen
                    PrintKoTOnSpecificPrinter(kotKitchen, config.KitchenPrinterName);
                    kitchenPrinted = true;
                }
                catch (Exception ex)
                {
                    kitchenEx = ex;
                    kitchenPrinted = false;
                }
            }
            else
            {
                // Kitchen printer not available — record reason silently and fallback to counter later
                kitchenPrinted = false;
            }

            // Always print to Counter printer if configured
            if (!string.IsNullOrEmpty(config.CounterPrinterName))
            {
                var kotCounter = new KoTPrinter.KoT
                {
                    Time = kot.Time,
                    Items = kot.Items,
                    KoTNumber = kot.KoTNumber,
                    Instructions = kot.Instructions
                };

                try
                {
                    PrintKoTOnSpecificPrinter(kotCounter, config.CounterPrinterName);
                }
                catch (Exception ex)
                {
                    // If counter printing fails we want to inform the user
                    MessageBox.Show("Counter printer failed: " + ex.Message, "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // If kitchen failed earlier, print a second copy to counter (so staff get kitchen copy)
                if (!kitchenPrinted)
                {
                    try
                    {
                        PrintKoTOnSpecificPrinter(kotCounter, config.CounterPrinterName);
                        // Fallback copy printed to counter silently (no user message)
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to print fallback KOT on Counter printer: " + ex.Message, "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (!kitchenPrinted)
            {
                // No counter configured and kitchen didn't print — notify user
                MessageBox.Show("No counter printer configured and kitchen printer failed. Please check printer setup.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper: Print KOT on a specific printer (override config)
        private void PrintKoTOnSpecificPrinter(KoTPrinter.KoT kot, string printerName)
        {
            // Call PrintKoT with forced printer name instead of writing to the config file
            KoTPrinter.PrintKoT(kot, printerName);
        }
    }
}
