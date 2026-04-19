using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SimplePOS
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, e) =>
            {
                try
                {
                    MessageBox.Show($"An unexpected error occurred:\n{e.Exception}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    Debug.WriteLine($"ThreadException: {e.Exception}");
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    Exception ex = e.ExceptionObject as Exception;
                    MessageBox.Show($"A fatal error occurred:\n{ex}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    Debug.WriteLine($"UnhandledException: {e.ExceptionObject}");
                }
            };

            DBHelper.EnsureDatabase();
            Application.Run(new MainForm());
        }
    }
}