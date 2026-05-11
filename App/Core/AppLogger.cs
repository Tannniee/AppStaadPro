using System;
using System.Diagnostics;

namespace StaadSteelApp.Core
{
    public static class AppLogger
    {
        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warning(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message)
        {
            Write("ERROR", message, null);
        }

        public static void Exception(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                " [" + level + "] " +
                (message ?? string.Empty);

            Debug.WriteLine(exception == null
                ? line
                : line + Environment.NewLine + exception);
        }
    }
}
