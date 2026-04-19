namespace SimplePOS
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox listBox1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            listBox1 = new ListBox();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.Font = new Font("Segoe UI", 12F);
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 21;
            listBox1.Location = new Point(12, 400);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(524, 361);
            listBox1.TabIndex = 0;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 800);
            Controls.Add(listBox1);
            Name = "MainForm";
            Text = "SimplePOS";
            ResumeLayout(false);
        }

        // Add this event handler to fix the error
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Add your logic here
        }
    }
}
