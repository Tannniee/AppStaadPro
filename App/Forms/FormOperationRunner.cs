using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal static class FormOperationRunner
    {
        internal static async Task RunStaThreadAsync(
            Form owner,
            Func<bool> isBusy,
            Action<bool> setBusy,
            string title,
            string statusText,
            Func<ServiceOperationResult> operation,
            string errorPrefix,
            Action<bool> setControlsEnabled = null)
        {
            if (isBusy != null && isBusy())
                return;

            setBusy?.Invoke(true);
            setControlsEnabled?.Invoke(false);

            Cursor previousCursor = owner?.Cursor ?? Cursors.Default;
            if (owner != null)
                owner.Cursor = Cursors.AppStarting;

            StatusForm.UpdateMessage(statusText, title, Color.Blue, autoHideMs: 0);

            try
            {
                ServiceOperationResult result = await RunStaThreadAsync(operation);
                ServiceOperationResultPresenter.Show(result);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError((errorPrefix ?? "Operation failed: ") + ex.Message, title);
            }
            finally
            {
                if (owner != null)
                    owner.Cursor = previousCursor;

                setControlsEnabled?.Invoke(true);
                setBusy?.Invoke(false);
            }
        }

        private static Task<ServiceOperationResult> RunStaThreadAsync(Func<ServiceOperationResult> operation)
        {
            var completion = new TaskCompletionSource<ServiceOperationResult>();
            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(operation());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }
    }
}
