using System;
using System.Windows.Forms;
using System.Threading;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    static class Program
    {
        internal const string ResetPositionEventName = @"Local\StaadSteelApp.ResetPosition";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var resetEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ResetPositionEventName))
            {
                var mainForm = new MainForm();
                using (var listener = new ResetPositionListener(resetEvent, mainForm))
                {
                    Application.Run(mainForm);
                }
            }
        }
    }
}
