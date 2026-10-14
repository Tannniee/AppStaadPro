using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static class StaadForegroundTracker
    {
        private const uint EventSystemForeground = 0x0003;
        private const uint WineventOutOfContext = 0x0000;
        private const uint GaRoot = 2;

        private delegate void WinEventDelegate(
            IntPtr hook,
            uint eventType,
            IntPtr windowHandle,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime);

        private static readonly object Sync = new object();
        private static readonly int CurrentApplicationProcessId = Process.GetCurrentProcess().Id;
        private static WinEventDelegate _callback;
        private static IntPtr _hook;
        private static int _lastStaadProcessId;
        private static long _lastStaadWindowHandle;
        private static DateTime _processCacheCreatedUtc = DateTime.MinValue;
        private static int[] _runningProcessIdCache = Array.Empty<int>();

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr eventHookModule,
            WinEventDelegate callback,
            uint processId,
            uint threadId,
            uint flags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

        internal static int LastStaadProcessId => _lastStaadProcessId;
        internal static long LastStaadWindowHandle => _lastStaadWindowHandle;

        internal static void RefreshFromForegroundWindow()
        {
            CaptureIfStaad(GetForegroundWindow());
        }

        internal static void Start()
        {
            lock (Sync)
            {
                if (_hook != IntPtr.Zero)
                    return;

                _callback = OnForegroundChanged;
                _hook = SetWinEventHook(
                    EventSystemForeground,
                    EventSystemForeground,
                    IntPtr.Zero,
                    _callback,
                    0,
                    0,
                    WineventOutOfContext);

                CaptureIfStaad(GetForegroundWindow());
                if (_hook == IntPtr.Zero)
                    AppLogger.Warning("Could not install the STAAD foreground-window tracker.");
            }
        }

        internal static void Stop()
        {
            lock (Sync)
            {
                if (_hook != IntPtr.Zero)
                {
                    UnhookWinEvent(_hook);
                    _hook = IntPtr.Zero;
                }

                _callback = null;
            }
        }

        internal static void RememberProcess(int processId, long windowHandle)
        {
            if (processId <= 0)
                return;

            _lastStaadProcessId = processId;
            if (windowHandle > 0)
                _lastStaadWindowHandle = windowHandle;
        }

        internal static int[] GetRunningMainProcessIds()
        {
            lock (Sync)
            {
                if (DateTime.UtcNow - _processCacheCreatedUtc <= TimeSpan.FromSeconds(5))
                    return _runningProcessIdCache.ToArray();

                Process[] processes = Process.GetProcesses();
                try
                {
                    _runningProcessIdCache = processes
                        .Where(IsStaadMainProcess)
                        .Select(process => process.Id)
                        .Distinct()
                        .OrderBy(processId => processId)
                        .ToArray();
                    _processCacheCreatedUtc = DateTime.UtcNow;
                    return _runningProcessIdCache.ToArray();
                }
                finally
                {
                    foreach (Process process in processes)
                        process.Dispose();
                }
            }
        }

        internal static bool IsMainProcessRunning(int processId)
        {
            return processId > 0 && IsStaadMainProcess(processId);
        }

        private static void OnForegroundChanged(
            IntPtr hook,
            uint eventType,
            IntPtr windowHandle,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime)
        {
            CaptureIfStaad(windowHandle);
        }

        private static void CaptureIfStaad(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return;

            IntPtr rootWindow = GetAncestor(windowHandle, GaRoot);
            if (rootWindow == IntPtr.Zero)
                rootWindow = windowHandle;

            GetWindowThreadProcessId(rootWindow, out uint rawProcessId);
            if (rawProcessId == 0 || rawProcessId > int.MaxValue)
                return;

            int processId = (int)rawProcessId;
            if (!IsStaadMainProcess(processId))
                return;

            if (_lastStaadProcessId != processId)
            {
                OpenStaadRotResolver.InvalidateCache();
                lock (Sync)
                    _processCacheCreatedUtc = DateTime.MinValue;
            }

            _lastStaadProcessId = processId;
            _lastStaadWindowHandle = rootWindow.ToInt64();
        }

        private static bool IsStaadMainProcess(int processId)
        {
            if (processId <= 0 || processId == CurrentApplicationProcessId)
                return false;

            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    return IsStaadMainProcess(process);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool IsStaadMainProcess(Process process)
        {
            try
            {
                string name = process?.ProcessName ?? string.Empty;
                if (process == null || process.Id == CurrentApplicationProcessId)
                    return false;
                if (string.Equals(name, "StaadSteelApp", StringComparison.OrdinalIgnoreCase))
                    return false;
                if (name.IndexOf("staad", StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

                return name.IndexOf("editor", StringComparison.OrdinalIgnoreCase) < 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
