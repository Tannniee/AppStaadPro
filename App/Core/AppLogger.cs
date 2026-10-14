using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StaadSteelApp.Core
{
    public static class AppLogger
    {
        private static readonly object LogLock = new object();
        private const long MaxLogBytes = 2L * 1024L * 1024L;

        public static string LogFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StaadSteelApp",
            "Logs",
            "StaadSteelApp.log");

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

            try
            {
                lock (LogLock)
                {
                    string path = LogFilePath;
                    string directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    RotateLogIfNeeded(path);
                    string output = exception == null
                        ? line
                        : line + Environment.NewLine + exception;
                    File.AppendAllText(path, output + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never interrupt a STAAD operation.
            }
        }

        private static void RotateLogIfNeeded(string path)
        {
            if (!File.Exists(path))
                return;

            var info = new FileInfo(path);
            if (info.Length < MaxLogBytes)
                return;

            string previousPath = path + ".previous";
            if (File.Exists(previousPath))
                File.Delete(previousPath);
            File.Move(path, previousPath);
        }
    }
}
