using System;
using System.Drawing;
using System.Windows.Forms;

namespace StaadSteelApp
{
    internal sealed class AdvanceActionContext
    {
        public string CoordinateUnitLabel { get; set; } = "?";
        public Func<int[]> GetSelectedNodes { get; set; }
        public Action ReloadSelection { get; set; }
        public Action UpdateHint { get; set; }

        public int[] CurrentSelection => GetSelectedNodes == null
            ? Array.Empty<int>()
            : (GetSelectedNodes() ?? Array.Empty<int>());

        public void ShowWarning(string message)
        {
            TopMostMessageBox.ShowWarning(message, "Advance");
        }

        public void ShowSuccess(string message)
        {
            TopMostMessageBox.ShowSuccess(message, "Advance");
        }

        public void RefreshSelection()
        {
            ReloadSelection?.Invoke();
        }

        public void RequestHintUpdate()
        {
            UpdateHint?.Invoke();
        }
    }

    internal sealed class AdvanceActionHint
    {
        public AdvanceActionHint(bool isError, string text)
        {
            IsError = isError;
            Text = text ?? string.Empty;
        }

        public bool IsError { get; }
        public string Text { get; }
        public Color ForeColor => IsError ? Color.Firebrick : Color.DimGray;
    }

    internal interface IAdvanceActionPanel
    {
        string ActionText { get; }
        string ApplyText { get; }
        Control View { get; }

        void AttachContext(AdvanceActionContext context);
        void OnSelectionChanged(int[] selectedNodes);
        AdvanceActionHint GetHint();
        bool Apply();
    }
}
