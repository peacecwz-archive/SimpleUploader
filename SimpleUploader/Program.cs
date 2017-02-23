using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleUploader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form form;
            if (args.Count() == 1)
                form = new Form1(args[0]);
            else
            {
                Setup();
                return;
            }
            form.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - form.Width,
                   Screen.PrimaryScreen.WorkingArea.Height - form.Height);
            Application.Run(form);
        }

        static void Setup()
        {
            string command1 = @"REG ADD HKCR\*\shell\SimpleUploader /ve /d ""Upload File"" /f",
                command2 = @"REG ADD HKCR\*\shell\SimpleUploader\command /ve /d """ + Application.StartupPath + @"\SimpleUploader.exe" + @" """"%1"""""" /f";
            Process.Start("cmd.exe", "/c " + command1);
            Process mainProc = Process.Start("cmd.exe", "/c " + command2);
            mainProc.WaitForExit();
        }
    }
}
