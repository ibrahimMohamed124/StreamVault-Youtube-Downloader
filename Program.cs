using System;
using System.Windows.Forms;

namespace StreamVaultWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // .NET 8 WinForms
            Application.Run(new MainForm());
        }
    }
}
