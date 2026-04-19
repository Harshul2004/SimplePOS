using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Linq;

namespace SimplePOS
{
    public class PrinterConfig
    {
        public string CounterPrinterName { get; set; } = "";
        public string KitchenPrinterName { get; set; } = "";
        public string LogoPath { get; set; } = ""; // path to header logo image
    }

    public class PrinterSetupControl : UserControl
    {
        private ComboBox cmbCounterPrinter, cmbKitchenPrinter;
        private Button btnSaveCounter, btnTestCounter, btnScanCounter;
        private Button btnSaveKitchen, btnTestKitchen, btnScanKitchen;
        private Button btnSelectLogo, btnClearKitchen, btnClearCounter;
        private Label lblTitle, lblCounterStatus, lblKitchenStatus, lblLogoPath;
        private PictureBox picLogoPreview;
        private PrinterConfig config;
        private const string ConfigPath = "printer_config.json";

        private List<string> lastCounterPrinters = new List<string>();
        private List<string> lastKitchenPrinters = new List<string>();

        public PrinterSetupControl()
        {
            InitializeUI();
            LoadConfig();
            lastCounterPrinters = GetInstalledPrinters();
            lastKitchenPrinters = GetInstalledPrinters();
        }

        private void InitializeUI()
        {
            this.BackColor = Color.WhiteSmoke;
            this.Dock = DockStyle.Fill;

            lblTitle = new Label
            {
                Text = "Printer Setup",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                AutoSize = true,
                Top = 30,
                Left = 16
            };
            this.Controls.Add(lblTitle);

            int rowSpacing = 40;
            int labelWidth = 200;
            int fieldSpacing = 16;
            int buttonSpacing = 16;
            int fieldHeight = 40;
            int buttonWidth = 180;
            int buttonHeight = 44;
            int comboWidth = 340;
            int leftMargin = 16;
            int topStart = lblTitle.Bottom + rowSpacing;

            // Counter Table Printer Row
            var lblCounter = new Label
            {
                Text = "Counter Table Printer:",
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                AutoSize = false,
                Width = labelWidth,
                Height = fieldHeight,
                Top = topStart,
                Left = leftMargin,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblCounter);

            cmbCounterPrinter = new ComboBox
            {
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                Left = lblCounter.Right + fieldSpacing,
                Top = lblCounter.Top,
                Width = comboWidth,
                Height = fieldHeight,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string printer in PrinterSettings.InstalledPrinters)
                cmbCounterPrinter.Items.Add(printer);
            cmbCounterPrinter.SelectedIndexChanged += cmbCounterPrinter_SelectedIndexChanged;
            this.Controls.Add(cmbCounterPrinter);

            btnSaveCounter = new Button
            {
                Text = "Save Counter",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = cmbCounterPrinter.Right + buttonSpacing,
                Top = lblCounter.Top,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.LightGray
            };
            btnSaveCounter.Click += BtnSaveCounter_Click;
            this.Controls.Add(btnSaveCounter);

            btnTestCounter = new Button
            {
                Text = "Test Counter",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = btnSaveCounter.Right + buttonSpacing,
                Top = lblCounter.Top,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.FromArgb(204, 255, 204)
            };
            btnTestCounter.Click += BtnTestCounter_Click;
            this.Controls.Add(btnTestCounter);

            // Clear Counter button positioned to the right of Test Counter
            btnClearCounter = new Button
            {
                Text = "Clear Counter",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = btnTestCounter.Right + buttonSpacing,
                Top = lblCounter.Top,
                Width = 140,
                Height = buttonHeight,
                BackColor = Color.LightCoral
            };
            btnClearCounter.Click += BtnClearCounter_Click;
            this.Controls.Add(btnClearCounter);

            btnScanCounter = new Button
            {
                Text = "Scan Counter",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = cmbCounterPrinter.Left,
                Top = cmbCounterPrinter.Bottom + 12,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.LightSkyBlue
            };
            btnScanCounter.Click += BtnScanCounter_Click;
            this.Controls.Add(btnScanCounter);

            lblCounterStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 13F, FontStyle.Italic),
                AutoSize = true,
                Top = btnScanCounter.Bottom + 6,
                Left = btnScanCounter.Left,
                ForeColor = Color.DarkGreen
            };
            this.Controls.Add(lblCounterStatus);

            // Kitchen Printer Row
            int kitchenTop = lblCounterStatus.Bottom + rowSpacing;
            var lblKitchen = new Label
            {
                Text = "Kitchen Printer:",
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                AutoSize = false,
                Width = labelWidth,
                Height = fieldHeight,
                Top = kitchenTop,
                Left = leftMargin,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblKitchen);

            cmbKitchenPrinter = new ComboBox
            {
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                Left = lblKitchen.Right + fieldSpacing,
                Top = lblKitchen.Top,
                Width = comboWidth,
                Height = fieldHeight,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string printer in PrinterSettings.InstalledPrinters)
                cmbKitchenPrinter.Items.Add(printer);
            cmbKitchenPrinter.SelectedIndexChanged += cmbKitchenPrinter_SelectedIndexChanged;
            this.Controls.Add(cmbKitchenPrinter);

            btnSaveKitchen = new Button
            {
                Text = "Save Kitchen",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = cmbKitchenPrinter.Right + buttonSpacing,
                Top = lblKitchen.Top,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.LightGray
            };
            btnSaveKitchen.Click += BtnSaveKitchen_Click;
            this.Controls.Add(btnSaveKitchen);

            btnTestKitchen = new Button
            {
                Text = "Test Kitchen",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = btnSaveKitchen.Right + buttonSpacing,
                Top = lblKitchen.Top,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.FromArgb(204, 255, 204)
            };
            btnTestKitchen.Click += BtnTestKitchen_Click;
            this.Controls.Add(btnTestKitchen);

            btnScanKitchen = new Button
            {
                Text = "Scan Kitchen",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = cmbKitchenPrinter.Left,
                Top = cmbKitchenPrinter.Bottom + 12,
                Width = buttonWidth,
                Height = buttonHeight,
                BackColor = Color.LightSkyBlue
            };
            btnScanKitchen.Click += BtnScanKitchen_Click;
            this.Controls.Add(btnScanKitchen);

            // New Clear Kitchen button moved to the right of Test Kitchen
            btnClearKitchen = new Button
            {
                Text = "Clear Kitchen",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = btnTestKitchen.Right + buttonSpacing,
                Top = lblKitchen.Top,
                Width = 140,
                Height = buttonHeight,
                BackColor = Color.LightCoral
            };
            btnClearKitchen.Click += BtnClearKitchen_Click;
            this.Controls.Add(btnClearKitchen);

            lblKitchenStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 13F, FontStyle.Italic),
                AutoSize = true,
                Top = btnScanKitchen.Bottom + 6,
                Left = btnScanKitchen.Left,
                ForeColor = Color.DarkGreen
            };
            this.Controls.Add(lblKitchenStatus);

            // Logo selection controls (placed after kitchen printer selector)
            btnSelectLogo = new Button
            {
                Text = "Select Header Logo",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Width = 220,
                Height = 40,
                Left = btnScanCounter.Left,
                Top = lblKitchenStatus.Bottom + 12,
                BackColor = Color.LightSteelBlue
            };
            btnSelectLogo.Click += BtnSelectLogo_Click;
            this.Controls.Add(btnSelectLogo);

            lblLogoPath = new Label
            {
                Text = "No logo selected",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                AutoSize = true,
                Left = btnSelectLogo.Right + 12,
                Top = btnSelectLogo.Top + 8,
                ForeColor = Color.Black
            };
            this.Controls.Add(lblLogoPath);

            picLogoPreview = new PictureBox
            {
                Width = 220,
                Height = 80,
                Left = lblCounter.Left,
                Top = btnSelectLogo.Bottom + 12,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(picLogoPreview);
        }

        private List<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
                printers.Add(printer);
            return printers;
        }

        // Resolve printer name: prefer current ComboBox selection, fall back to saved config
        private string ResolvePrinterName(ComboBox combo, string configName)
        {
            string name = combo.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(name))
                return name;
            if (!string.IsNullOrEmpty(configName))
                return configName;
            return string.Empty;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    config = JsonSerializer.Deserialize<PrinterConfig>(json);
                }
                else
                {
                    config = new PrinterConfig();
                }
            }
            catch
            {
                config = new PrinterConfig();
            }
            // Set UI from config
            if (cmbCounterPrinter.Items.Count > 0 && !string.IsNullOrEmpty(config.CounterPrinterName))
            {
                int idx = cmbCounterPrinter.Items.IndexOf(config.CounterPrinterName);
                cmbCounterPrinter.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else if (cmbCounterPrinter.Items.Count > 0)
            {
                cmbCounterPrinter.SelectedIndex = 0;
            }
            if (cmbKitchenPrinter.Items.Count > 0 && !string.IsNullOrEmpty(config.KitchenPrinterName))
            {
                int idx = cmbKitchenPrinter.Items.IndexOf(config.KitchenPrinterName);
                cmbKitchenPrinter.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else if (cmbKitchenPrinter.Items.Count > 0)
            {
                cmbKitchenPrinter.SelectedIndex = 0;
            }

            // Load logo preview if present
            try
            {
                if (!string.IsNullOrEmpty(config.LogoPath) && File.Exists(config.LogoPath))
                {
                    lblLogoPath.Text = config.LogoPath;
                    picLogoPreview.Image = LoadPreviewImage(config.LogoPath);
                }
                else
                {
                    lblLogoPath.Text = "No logo selected";
                    picLogoPreview.Image = null;
                }
            }
            catch { lblLogoPath.Text = "No logo selected"; picLogoPreview.Image = null; }
        }

        private void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving config: " + ex.Message);
            }
        }

        private void BtnSaveCounter_Click(object sender, EventArgs e)
        {
            // Reload config from disk to avoid overwriting kitchen printer
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    config = JsonSerializer.Deserialize<PrinterConfig>(json) ?? new PrinterConfig();
                }
                else
                {
                    config = new PrinterConfig();
                }
            }
            catch
            {
                config = new PrinterConfig();
            }
            config.CounterPrinterName = cmbCounterPrinter.SelectedItem?.ToString() ?? "";
            SaveConfig();
            lblCounterStatus.Text = "Counter printer saved.";
            lblCounterStatus.ForeColor = Color.DarkGreen;
        }

        private void BtnTestCounter_Click(object sender, EventArgs e)
        {
            string printerName = ResolvePrinterName(cmbCounterPrinter, config?.CounterPrinterName ?? "");
            if (string.IsNullOrEmpty(printerName))
            {
                lblCounterStatus.Text = "No counter printer selected.";
                lblCounterStatus.ForeColor = Color.Red;
                return;
            }

            try
            {
                var installed = GetInstalledPrinters();
                if (!installed.Contains(printerName))
                {
                    lblCounterStatus.Text = $"Printer '{printerName}' not found among installed printers.";
                    lblCounterStatus.ForeColor = Color.Red;
                    return;
                }

                var ps = new PrinterSettings { PrinterName = printerName };
                if (!ps.IsValid)
                {
                    lblCounterStatus.Text = $"Printer '{printerName}' is not valid or unavailable.";
                    lblCounterStatus.ForeColor = Color.Red;
                    return;
                }

                var pd = new PrintDocument { PrinterSettings = ps };
                pd.PrintController = new StandardPrintController();
                pd.PrintPage += (s, ev) =>
                {
                    using var f = new Font("Segoe UI", 18F);
                    ev.Graphics.DrawString("Test Bill", f, Brushes.Black, 100, 100);
                };

                pd.Print();
                lblCounterStatus.Text = $"Test Bill job sent to '{printerName}' (spooled).";
                lblCounterStatus.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                lblCounterStatus.Text = "Error sending test bill: " + ex.Message;
                lblCounterStatus.ForeColor = Color.Red;
            }
        }

        private void BtnScanCounter_Click(object sender, EventArgs e)
        {
            bool foundNew;
            RefreshPrinterList(cmbCounterPrinter, lastCounterPrinters, out foundNew);
            if (foundNew)
            {
                lblCounterStatus.Text = "New printers found and list updated.";
                lblCounterStatus.ForeColor = Color.DarkGreen;
            }
            else if (cmbCounterPrinter.Items.Count == 0)
            {
                lblCounterStatus.Text = "No printers found.";
                lblCounterStatus.ForeColor = Color.Red;
            }
            else
            {
                lblCounterStatus.Text = "No new printers detected. List refreshed.";
                lblCounterStatus.ForeColor = Color.Black;
            }
        }

        private void BtnSaveKitchen_Click(object sender, EventArgs e)
        {
            // Reload config from disk to avoid overwriting counter printer
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    config = JsonSerializer.Deserialize<PrinterConfig>(json) ?? new PrinterConfig();
                }
                else
                {
                    config = new PrinterConfig();
                }
            }
            catch
            {
                config = new PrinterConfig();
            }
            config.KitchenPrinterName = cmbKitchenPrinter.SelectedItem?.ToString() ?? "";
            SaveConfig();
            lblKitchenStatus.Text = "Kitchen printer saved.";
            lblKitchenStatus.ForeColor = Color.DarkGreen;
        }

        private void BtnTestKitchen_Click(object sender, EventArgs e)
        {
            string printerName = ResolvePrinterName(cmbKitchenPrinter, config?.KitchenPrinterName ?? "");
            if (string.IsNullOrEmpty(printerName))
            {
                lblKitchenStatus.Text = "No kitchen printer selected.";
                lblKitchenStatus.ForeColor = Color.Red;
                return;
            }

            try
            {
                var installed = GetInstalledPrinters();
                if (!installed.Contains(printerName))
                {
                    lblKitchenStatus.Text = $"Printer '{printerName}' not found among installed printers.";
                    lblKitchenStatus.ForeColor = Color.Red;
                    return;
                }

                var ps = new PrinterSettings { PrinterName = printerName };
                if (!ps.IsValid)
                {
                    lblKitchenStatus.Text = $"Printer '{printerName}' is not valid or unavailable.";
                    lblKitchenStatus.ForeColor = Color.Red;
                    return;
                }

                var pd = new PrintDocument { PrinterSettings = ps };
                pd.PrintController = new StandardPrintController();
                pd.PrintPage += (s, ev) =>
                {
                    using var f = new Font("Segoe UI", 18F);
                    ev.Graphics.DrawString("Test KOT", f, Brushes.Black, 100, 100);
                };

                pd.Print();
                lblKitchenStatus.Text = $"Test KOT job sent to '{printerName}' (spooled).";
                lblKitchenStatus.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                lblKitchenStatus.Text = "Error sending test KOT: " + ex.Message;
                lblKitchenStatus.ForeColor = Color.Red;
            }
        }

        private void BtnScanKitchen_Click(object sender, EventArgs e)
        {
            bool foundNew;
            RefreshPrinterList(cmbKitchenPrinter, lastKitchenPrinters, out foundNew);
            if (foundNew)
            {
                lblKitchenStatus.Text = "New printers found and list updated.";
                lblKitchenStatus.ForeColor = Color.DarkGreen;
            }
            else if (cmbKitchenPrinter.Items.Count == 0)
            {
                lblKitchenStatus.Text = "No printers found.";
                lblKitchenStatus.ForeColor = Color.Red;
            }
            else
            {
                lblKitchenStatus.Text = "No new printers detected. List refreshed.";
                lblKitchenStatus.ForeColor = Color.Black;
            }
        }

        private void BtnSelectLogo_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Header Logo";
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*";
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Store path in config and save immediately
                        config.LogoPath = ofd.FileName;
                        SaveConfig();
                        lblLogoPath.Text = config.LogoPath;

                        // Load preview into PictureBox without locking file
                        picLogoPreview.Image = LoadPreviewImage(config.LogoPath);

                        lblCounterStatus.Text = "Logo selected and saved.";
                        lblCounterStatus.ForeColor = Color.DarkGreen;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load logo: " + ex.Message);
                    }
                }
            }
        }

        private Image LoadPreviewImage(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ms = new MemoryStream();
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    return Image.FromStream(ms);
                }
            }
            catch { return null; }
        }

        private void RefreshPrinterList(ComboBox combo, List<string> lastPrinters, out bool foundNew)
        {
            var currentPrinters = GetInstalledPrinters();
            foundNew = false;
            combo.Items.Clear();
            foreach (var printer in currentPrinters)
                combo.Items.Add(printer);
            // Check for new printers
            var newPrinters = currentPrinters.Except(lastPrinters).ToList();
            foundNew = newPrinters.Count > 0;
            // Try to keep selection
            if (!string.IsNullOrEmpty(config != null ? (combo == cmbCounterPrinter ? config.CounterPrinterName : config.KitchenPrinterName) : ""))
            {
                int idx = combo.Items.IndexOf(combo == cmbCounterPrinter ? config.CounterPrinterName : config.KitchenPrinterName);
                combo.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            // Update lastPrinters
            lastPrinters.Clear();
            lastPrinters.AddRange(currentPrinters);
        }

        private void cmbCounterPrinter_SelectedIndexChanged(object sender, EventArgs e)
        {
            config.CounterPrinterName = cmbCounterPrinter.SelectedItem?.ToString() ?? "";
            SaveConfig();
            lblCounterStatus.Text = $"Counter printer set to: {config.CounterPrinterName}";
            lblCounterStatus.ForeColor = Color.DarkGreen;
        }
        private void cmbKitchenPrinter_SelectedIndexChanged(object sender, EventArgs e)
        {
            config.KitchenPrinterName = cmbKitchenPrinter.SelectedItem?.ToString() ?? "";
            SaveConfig();
            lblKitchenStatus.Text = $"Kitchen printer set to: {config.KitchenPrinterName}";
            lblKitchenStatus.ForeColor = Color.DarkGreen;
        }

        // Handler to clear kitchen printer
        private void BtnClearKitchen_Click(object sender, EventArgs e)
        {
            config.KitchenPrinterName = "";
            SaveConfig();
            cmbKitchenPrinter.SelectedIndex = -1;
            lblKitchenStatus.Text = "Kitchen printer cleared.";
            lblKitchenStatus.ForeColor = Color.DarkRed;
        }

        // Handler to clear counter printer
        private void BtnClearCounter_Click(object sender, EventArgs e)
        {
            config.CounterPrinterName = "";
            SaveConfig();
            cmbCounterPrinter.SelectedIndex = -1;
            lblCounterStatus.Text = "Counter printer cleared.";
            lblCounterStatus.ForeColor = Color.DarkRed;
        }
    }
}
