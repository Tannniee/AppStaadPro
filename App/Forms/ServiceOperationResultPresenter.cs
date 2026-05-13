using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal static class ServiceOperationResultPresenter
    {
        internal static void Show(IOperationResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Message))
                return;

            switch (result.Kind)
            {
                case ServiceOperationResultKind.Success:
                    TopMostMessageBox.ShowSuccess(result.Message, result.Title);
                    break;
                case ServiceOperationResultKind.Warning:
                    TopMostMessageBox.ShowWarning(result.Message, result.Title);
                    break;
                case ServiceOperationResultKind.Info:
                    TopMostMessageBox.ShowInfo(result.Message, result.Title);
                    break;
                default:
                    TopMostMessageBox.ShowError(result.Message, result.Title);
                    break;
            }
        }
    }
}
