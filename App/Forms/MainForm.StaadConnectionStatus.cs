using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private Timer _staadConnectionTimer;
        private StatusStrip _staadStatusStrip;
        private ToolStripStatusLabel _staadStatusLabel;
        private int _baseClientHeight;

        private void InitializeStaadConnectionStatus()
        {
            if (_staadConnectionTimer != null)
                return;

            _baseClientHeight = ClientSize.Height;
            _staadStatusLabel = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "STAAD: Connecting...",
                ToolTipText = "Resolving the active STAAD model."
            };

            _staadStatusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                SizingGrip = false,
                ShowItemToolTips = true
            };
            _staadStatusStrip.Items.Add(_staadStatusLabel);
            _staadStatusStrip.ContextMenuStrip = BuildConnectionModeMenu();
            Controls.Add(_staadStatusStrip);
            ClientSize = new Size(ClientSize.Width, _baseClientHeight + _staadStatusStrip.Height);
            _staadStatusStrip.BringToFront();

            StaadManager.ActiveModelChanged += StaadManager_ActiveModelChanged;
            _staadConnectionTimer = new Timer { Interval = 750 };
            _staadConnectionTimer.Tick += StaadConnectionTimer_Tick;
            _staadConnectionTimer.Start();
            RefreshStaadConnectionStatus();
        }

        private ContextMenuStrip BuildConnectionModeMenu()
        {
            var menu = new ContextMenuStrip();
            var activeItem = new ToolStripMenuItem("Use active STAAD model") { Tag = StaadConnectionMode.ActiveModel };
            var legacyItem = new ToolStripMenuItem("Legacy single-instance mode") { Tag = StaadConnectionMode.LegacySingleInstance };

            activeItem.Click += ConnectionModeMenuItem_Click;
            legacyItem.Click += ConnectionModeMenuItem_Click;
            menu.Items.Add(activeItem);
            menu.Items.Add(legacyItem);
            menu.Opening += (sender, args) =>
            {
                StaadConnectionMode mode = StaadManager.ConnectionMode;
                activeItem.Checked = mode == StaadConnectionMode.ActiveModel;
                legacyItem.Checked = mode == StaadConnectionMode.LegacySingleInstance;
            };
            return menu;
        }

        private void ConnectionModeMenuItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem item) || !(item.Tag is StaadConnectionMode mode))
                return;

            StaadManager.ConnectionMode = mode;
            RefreshStaadConnectionStatus();
        }

        private void StaadConnectionTimer_Tick(object sender, EventArgs e)
        {
            if (!Visible || IsDisposed)
                return;

            RefreshStaadConnectionStatus();
        }

        private void RefreshStaadConnectionStatus()
        {
            if (_staadStatusLabel == null)
                return;

            if (StaadManager.TryGetCurrentIdentity(out StaadModelIdentity identity, out string error))
            {
                UpdateStaadStatus(identity, string.Empty);
                RefreshMaterialListForModel(identity);
            }
            else
            {
                UpdateStaadStatus(null, error);
                RefreshMaterialListForModel(null);
            }
        }

        private void StaadManager_ActiveModelChanged(object sender, StaadActiveModelChangedEventArgs e)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => HandleActiveModelChanged(e)));
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            HandleActiveModelChanged(e);
        }

        private void HandleActiveModelChanged(StaadActiveModelChangedEventArgs e)
        {
            bool connected = e?.Current != null;
            UpdateStaadStatus(e?.Current, connected ? string.Empty : "STAAD connection is not available.");
            RefreshMaterialListForModel(e?.Current);

            _usedSectionsHighlightTimer?.Stop();
            _pendingUsedSectionBeamIds = null;
            _pendingUsedSectionPropertyIds = null;
            _pendingUsedSectionModelKey = string.Empty;
            _usedSectionsBoundConnection = null;
            _usedSectionsBoundModelKey = string.Empty;

            if (_usedSectionsForm != null && !_usedSectionsForm.IsDisposed)
            {
                _usedSectionsForm.SetEntries(Array.Empty<UsedSectionEntry>());
                if (connected)
                    _usedSectionsForm.SetLoadingState(e.Current);
                else
                    _usedSectionsForm.SetWaitingState("Activate a STAAD model...");
                if (connected && _usedSectionsForm.Visible)
                    RefreshUsedSectionsList();
            }

            _groupsForm?.HandleActiveModelChanged(connected);
            _assignLoadForm?.HandleActiveModelChanged(connected);
            _advanceForm?.HandleActiveModelChanged(connected);
        }

        private void UpdateStaadStatus(StaadModelIdentity identity, string error)
        {
            if (_staadStatusLabel == null)
                return;

            if (identity == null)
            {
                Text = "STAAD Tools - Disconnected";
                _staadStatusLabel.Text = "STAAD: Disconnected";
                _staadStatusLabel.ForeColor = Color.Firebrick;
                _staadStatusLabel.ToolTipText = string.IsNullOrWhiteSpace(error)
                    ? "No validated STAAD model is available."
                    : error;
                return;
            }

            string modeSuffix = identity.IsLegacyFallback ? " [Legacy]" : string.Empty;
            Text = "STAAD Tools - " + identity.DisplayName;
            _staadStatusLabel.Text = "STAAD: " + identity.DisplayName + modeSuffix;
            _staadStatusLabel.ForeColor = identity.IsLegacyFallback ? Color.DarkOrange : Color.DarkGreen;
            _staadStatusLabel.ToolTipText =
                (string.IsNullOrWhiteSpace(identity.FullPath) ? identity.DisplayName : identity.FullPath) +
                Environment.NewLine + "PID: " + identity.ProcessId +
                Environment.NewLine + "Mode: " + StaadManager.ConnectionMode;
        }

        private void DisposeStaadConnectionStatus()
        {
            StaadManager.ActiveModelChanged -= StaadManager_ActiveModelChanged;

            if (_staadConnectionTimer != null)
            {
                _staadConnectionTimer.Stop();
                _staadConnectionTimer.Dispose();
                _staadConnectionTimer = null;
            }

            if (_staadStatusStrip != null)
            {
                _staadStatusStrip.Dispose();
                _staadStatusStrip = null;
                _staadStatusLabel = null;
            }
        }
    }
}
