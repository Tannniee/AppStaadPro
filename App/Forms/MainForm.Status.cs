using System.Drawing;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void InitializeStatusPanel()
        {
            _statusPanel = StatusForm.Instance;
            if (_statusPanel == null || _statusPanel.IsDisposed)
            {
                _statusPanel = new StatusForm();
            }

            if (_statusPanel.Owner != this)
            {
                AddOwnedForm(_statusPanel);
            }

            UpdateStatusPanelPos();
            _statusPanel.Hide();
        }

        private void CloseStatusPanel()
        {
            _statusPanel?.Close();
        }

        private void UpdateStatusPanelPos()
        {
            if (_statusPanel != null && !_statusPanel.IsDisposed && !Disposing)
            {
                _statusPanel.Location = new Point(Right - 5, Top);
            }
        }
    }
}
