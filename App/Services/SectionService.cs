using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Diagnostics;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static readonly HashSet<string> _warnOnceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static void WarnOnce(string key, string message, Exception ex = null)
        {
            if (!string.IsNullOrWhiteSpace(key) && !_warnOnceKeys.Add(key))
                return;

            Debug.WriteLine(ex == null ? message : (message + " | " + ex));
            StaadSteelApp.TopMostMessageBox.ShowWarning(ex == null ? message : (message + "\n" + ex.Message), "STAAD Warning");
        }

    }
}
