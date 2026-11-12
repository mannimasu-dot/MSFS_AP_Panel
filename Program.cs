using System;
using System.Threading;
using System.Windows.Forms;

namespace MSFS_AP_Panel
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Single-instance mittels benanntem Mutex: verhindert mehrfaches Starten
            bool createdNew = false;
            using (var mutex = new Mutex(true, "MSFS_AP_Panel_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Die Anwendung läuft bereits.", "MSFS_AP_Panel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
        }
    }
}