using System;
using System.Threading;

namespace StaadSteelApp.Core
{
    internal sealed class ResetPositionListener : IDisposable
    {
        private readonly EventWaitHandle _resetEvent;
        private readonly MainForm _mainForm;
        private readonly Thread _listenerThread;
        private volatile bool _disposed;

        public ResetPositionListener(EventWaitHandle resetEvent, MainForm mainForm)
        {
            _resetEvent = resetEvent;
            _mainForm = mainForm;
            _listenerThread = new Thread(ListenLoop);
            _listenerThread.IsBackground = true;
            _listenerThread.Name = "StaadSteelApp.ResetPositionListener";
            _listenerThread.Start();
        }

        private void ListenLoop()
        {
            while (!_disposed)
            {
                _resetEvent.WaitOne();
                if (_disposed)
                {
                    break;
                }

                try
                {
                    if (_mainForm != null && !_mainForm.IsDisposed && _mainForm.IsHandleCreated)
                    {
                        _mainForm.BeginInvoke((Action)(() => _mainForm.ResetWindowToCenter()));
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception("Reset-position listener failed while dispatching to MainForm.", ex);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _resetEvent.Set();
            }
            catch (Exception ex)
            {
                AppLogger.Exception("Reset-position listener failed while signaling shutdown.", ex);
            }

            try
            {
                if (!_listenerThread.Join(1000))
                {
                    _listenerThread.Interrupt();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Exception("Reset-position listener failed while stopping the background thread.", ex);
            }
        }
    }
}
