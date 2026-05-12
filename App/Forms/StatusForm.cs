using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StaadSteelApp
{
    // Persistent Status Form (Always Visible)
    internal sealed class StatusForm : Form
    {
        private const int MinPanelWidth = 300;
        private const int MaxPanelWidth = 520;
        private const int MinPanelHeight = 80;
        private const int MaxPanelHeight = 360;

        private static StatusForm _instance;
        private Label _lblTitle;
        private Label _lblBody;
        private readonly Timer _hideTimer;

        internal static StatusForm Instance => _instance;
        
        // P/Invoke for Rounded Corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public StatusForm()
        {
            _instance = this;
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(MinPanelWidth, 110);
            this.MinimumSize = new Size(MinPanelWidth, MinPanelHeight);
            this.MaximumSize = new Size(MaxPanelWidth, MaxPanelHeight);
            this.BackColor = SystemColors.Control;
            this.Text = "Status Panel";
            this.Padding = new Padding(3);

            // Structure: GroupBox -> [Title Label] + [Body Label]
            var grp = new GroupBox();
            grp.Dock = DockStyle.Fill;
            grp.Text = "Notification"; // Small caption on border
            grp.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);

            // 1. Title Label (Top, Bold, Centered)
            _lblTitle = new Label();
            _lblTitle.Text = "";
            _lblTitle.ForeColor = Color.Black;
            _lblTitle.Dock = DockStyle.Top;
            _lblTitle.Height = 25;
            _lblTitle.TextAlign = ContentAlignment.BottomCenter; // Bottom aligned to sit just above body
            _lblTitle.Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold);
            _lblTitle.BackColor = Color.Transparent;

            // 2. Body Label (Fill, Regular, Left or Center depending on content)
            _lblBody = new Label();
            _lblBody.Text = "Ready";
            _lblBody.ForeColor = Color.Black;
            _lblBody.Dock = DockStyle.Fill;
            _lblBody.TextAlign = ContentAlignment.TopCenter; // Default to center for simple messages
            _lblBody.Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular);
            _lblBody.BackColor = Color.Transparent;
            _lblBody.Padding = new Padding(14, 6, 8, 8);

            grp.Controls.Add(_lblBody);
            grp.Controls.Add(_lblTitle); // Add Title last to dock to top correctly (inverted z-order in some designers, but strict code order: add last = top of z-order? No, Dock order matters. Dock=Top added last stays at top if no other Dock=Top? Actually safest is simple order)
            // Dock logic: Last added = First in dock order (closest to edge). 
            // So we add Body (Fill) first, then Title (Top). The Title will push the Body down.
            
            this.Controls.Add(grp);

            this.Load += (s, e) => ApplyRounding();

            _hideTimer = new Timer();
            _hideTimer.Interval = 1500;
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                if (!IsDisposed && Visible)
                {
                    Hide();
                }
            };
        }

        private void ApplyRounding()
        {
            IntPtr hrgn = CreateRoundRectRgn(0, 0, Width, Height, 8, 8);
            try
            {
                var newRegion = Region.FromHrgn(hrgn);
                var oldRegion = this.Region;
                this.Region = newRegion;
                oldRegion?.Dispose();
            }
            finally
            {
                if (hrgn != IntPtr.Zero)
                {
                    DeleteObject(hrgn);
                }
            }
        }

        public void SetMessage(string msg, string title = null, Color? titleColor = null, int autoHideMs = 1500)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string, Color?, int>(SetMessage), msg, title, titleColor, autoHideMs);
                return;
            }

            if (!Visible)
            {
                Show();
            }

            // Logic: 
            // If Title is provided: Show Title (Centered), Show Body (Left Aligned for lists)
            
            if (!string.IsNullOrEmpty(title))
            {
                _lblTitle.Text = title;
                _lblTitle.Visible = true;
                _lblTitle.ForeColor = titleColor ?? Color.Black;
                
                _lblBody.Text = msg;
                _lblBody.TextAlign = ContentAlignment.TopLeft; // Left align for lists
            }
            else
            {
                _lblTitle.Text = "";
                _lblTitle.Visible = false;
                
                _lblBody.Text = msg;
                _lblBody.TextAlign = ContentAlignment.MiddleCenter; // Revert to center for simple toasts
            }

            ResizeToFitMessage(msg, !string.IsNullOrEmpty(title));
            ApplyRounding();
            KeepWithinWorkingArea();

            // Restart auto-hide timer on every message update.
            _hideTimer.Stop();
            if (autoHideMs > 0)
            {
                _hideTimer.Interval = autoHideMs;
                _hideTimer.Start();
            }
        }

        private void ResizeToFitMessage(string msg, bool hasTitle)
        {
            string body = string.IsNullOrEmpty(msg) ? " " : msg;

            int textMaxWidth = MaxPanelWidth - 34;
            Size measured = TextRenderer.MeasureText(
                body,
                _lblBody.Font,
                new Size(textMaxWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            int desiredWidth = Math.Max(MinPanelWidth, Math.Min(MaxPanelWidth, measured.Width + 44));
            int bodyWidth = Math.Max(120, desiredWidth - 34);

            measured = TextRenderer.MeasureText(
                body,
                _lblBody.Font,
                new Size(bodyWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            int titleHeight = hasTitle ? 25 : 0;
            int desiredHeight = measured.Height + titleHeight + 46;
            desiredHeight = Math.Max(MinPanelHeight, Math.Min(MaxPanelHeight, desiredHeight));

            Size = new Size(desiredWidth, desiredHeight);
        }

        private void KeepWithinWorkingArea()
        {
            Rectangle workArea = Owner == null
                ? Screen.FromControl(this).WorkingArea
                : Screen.FromControl(Owner).WorkingArea;

            int x = Left;
            int y = Top;

            if (Right > workArea.Right)
                x = workArea.Right - Width - 4;
            if (x < workArea.Left)
                x = workArea.Left + 4;

            if (Bottom > workArea.Bottom)
                y = workArea.Bottom - Height - 4;
            if (y < workArea.Top)
                y = workArea.Top + 4;

            Location = new Point(x, y);
        }

        // Static helper
        public static void UpdateMessage(string msg, string title = null, Color? titleColor = null, int autoHideMs = 1500)
        {
            var owner = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (owner != null && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => UpdateMessage(msg, title, titleColor, autoHideMs)));
                return;
            }

            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new StatusForm();
                if (owner != null && !owner.IsDisposed)
                {
                    _instance.Show(owner);
                    _instance.Location = new Point(owner.Right - 5, owner.Top);
                }
                else
                {
                    _instance.Show();
                }
            }

            if (!_instance.IsDisposed)
            {
                _instance.SetMessage(msg, title, titleColor, autoHideMs);
            }
        }

        protected override bool ShowWithoutActivation => true;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;
                param.ExStyle |= WS_EX_TOPMOST;
                param.ExStyle |= WS_EX_NOACTIVATE;
                return param;
            }
        }
    }
}
