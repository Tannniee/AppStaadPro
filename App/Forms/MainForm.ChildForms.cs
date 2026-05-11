using System;
using System.Windows.Forms;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void ShowOwnedToolForm<T>(
            ref T form,
            Func<T> createForm,
            Action clearReference,
            Action<T> beforeBringToFront = null,
            bool focus = false)
            where T : Form
        {
            if (form == null || form.IsDisposed)
            {
                form = createForm();
                form.FormClosed += (s, args) => clearReference?.Invoke();
            }

            if (form.Owner != this)
            {
                AddOwnedForm(form);
            }

            if (!form.Visible)
            {
                form.Show(this);
            }

            beforeBringToFront?.Invoke(form);
            form.BringToFront();

            if (focus)
                form.Focus();
        }
    }
}
