using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class GroupsForm : Form
    {
        private readonly GroupBox _grpGroups;
        private readonly ListBox _lstGroups;
        private readonly Label _lblName;
        private readonly TextBox _txtName;
        private readonly Button _btnRefresh;
        private readonly Button _btnCreate;
        private readonly Button _btnDelete;
        private readonly Button _btnAddSelected;
        private readonly Button _btnRemoveSelected;
        private readonly Timer _highlightTimer;
        private string[] _pendingHighlightGroupNames = Array.Empty<string>();
        private string _pendingHighlightModelKey = string.Empty;
        private string _boundModelKey = string.Empty;
        private bool _isBinding;
        private bool _locationInitialized;
        private bool _suppressLocationSave;

        public GroupsForm()
        {
            Text = "Groups";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(250, 390);

            _grpGroups = new GroupBox
            {
                Text = "Member Groups",
                Location = new Point(12, 12),
                Size = new Size(226, 246)
            };

            _lstGroups = new ListBox
            {
                Location = new Point(10, 20),
                Size = new Size(206, 216),
                HorizontalScrollbar = true,
                IntegralHeight = false,
                SelectionMode = SelectionMode.MultiExtended
            };
            _lstGroups.SelectedIndexChanged += lstGroups_SelectedIndexChanged;

            _highlightTimer = new Timer { Interval = 120 };
            _highlightTimer.Tick += HighlightTimer_Tick;

            _grpGroups.Controls.Add(_lstGroups);
            Controls.Add(_grpGroups);

            _lblName = new Label
            {
                Text = "Name",
                Location = new Point(14, 270),
                AutoSize = true
            };
            Controls.Add(_lblName);

            _txtName = new TextBox
            {
                Location = new Point(55, 267),
                Size = new Size(183, 20)
            };
            Controls.Add(_txtName);

            _btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(12, 298),
                Size = new Size(70, 28)
            };
            _btnRefresh.Click += (s, e) => RefreshGroups(SelectedGroupNames);
            Controls.Add(_btnRefresh);

            _btnCreate = new Button
            {
                Text = "Create",
                Location = new Point(90, 298),
                Size = new Size(70, 28)
            };
            _btnCreate.Click += btnCreate_Click;
            Controls.Add(_btnCreate);

            _btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(168, 298),
                Size = new Size(70, 28)
            };
            _btnDelete.Click += btnDelete_Click;
            Controls.Add(_btnDelete);

            _btnAddSelected = new Button
            {
                Text = "Add Selected",
                Location = new Point(12, 338),
                Size = new Size(108, 30)
            };
            _btnAddSelected.Click += btnAddSelected_Click;
            Controls.Add(_btnAddSelected);

            _btnRemoveSelected = new Button
            {
                Text = "Remove Selected",
                Location = new Point(130, 338),
                Size = new Size(108, 30)
            };
            _btnRemoveSelected.Click += btnRemoveSelected_Click;
            Controls.Add(_btnRemoveSelected);

            Move += (s, e) => SaveLocation();
        }

        private string[] SelectedGroupNames => _lstGroups.SelectedItems
            .OfType<GroupEntry>()
            .Select(entry => entry.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshGroups();
        }

        public void RestoreLocation(Point defaultLocation)
        {
            _suppressLocationSave = true;
            try
            {
                string currentDisplaySignature = WindowPlacementHelper.GetDisplaySignature();
                string savedDisplaySignature = AppSettings.LoadString("Groups_DisplaySignature");
                bool hasLocation = string.Equals(AppSettings.LoadString("Groups_HasLocation"), "1", StringComparison.Ordinal);

                Point location;
                if (!hasLocation)
                {
                    location = WindowPlacementHelper.EnsureVisibleLocation(defaultLocation, Size);
                }
                else if (!string.IsNullOrWhiteSpace(savedDisplaySignature) &&
                         !string.Equals(savedDisplaySignature, currentDisplaySignature, StringComparison.Ordinal))
                {
                    location = WindowPlacementHelper.GetDefaultVisibleLocation(Size);
                }
                else
                {
                    location = WindowPlacementHelper.EnsureVisibleLocation(AppSettings.Load("Groups"), Size);
                }

                StartPosition = FormStartPosition.Manual;
                Location = location;
                _locationInitialized = true;
            }
            finally
            {
                _suppressLocationSave = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _highlightTimer.Stop();
            SaveLocation();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _highlightTimer.Dispose();
            base.OnFormClosed(e);
        }

        internal void HandleActiveModelChanged(bool connected)
        {
            _highlightTimer.Stop();
            _pendingHighlightGroupNames = Array.Empty<string>();
            _pendingHighlightModelKey = string.Empty;
            _boundModelKey = string.Empty;
            _isBinding = true;
            try
            {
                _lstGroups.Items.Clear();
                _txtName.Clear();
                _grpGroups.Text = "Member Groups (0)";
            }
            finally
            {
                _isBinding = false;
            }

            if (connected && Visible)
                RefreshGroups(Array.Empty<string>());
        }

        private void RefreshGroups(IEnumerable<string> selectedGroupNames = null)
        {
            _boundModelKey = string.Empty;
            try
            {
                if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionError))
                    throw new InvalidOperationException(connectionError);

                IReadOnlyList<GroupEntry> groups = GroupService.GetMemberGroups(connection.OpenStaad);
                string[] selectedNames = (selectedGroupNames ?? SelectedGroupNames)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                _isBinding = true;
                _lstGroups.BeginUpdate();
                try
                {
                    _lstGroups.Items.Clear();
                    foreach (GroupEntry group in groups)
                    {
                        _lstGroups.Items.Add(group);
                    }

                    _grpGroups.Text = "Member Groups (" + groups.Count + ")";
                    SelectGroupsByName(selectedNames);
                    _boundModelKey = connection.Identity.CacheKey;
                }
                finally
                {
                    _lstGroups.EndUpdate();
                    _isBinding = false;
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowWarning("Could not refresh groups: " + ex.Message, "Groups");
            }
        }

        private void SelectGroupsByName(IEnumerable<string> groupNames)
        {
            var names = new HashSet<string>(
                (groupNames ?? Enumerable.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.CurrentCultureIgnoreCase);
            if (names.Count == 0)
                return;

            GroupEntry activeEntry = null;
            for (int i = 0; i < _lstGroups.Items.Count; i++)
            {
                if (_lstGroups.Items[i] is GroupEntry entry &&
                    names.Contains(entry.Name))
                {
                    _lstGroups.SetSelected(i, true);
                    activeEntry = entry;
                }
            }

            if (names.Count == 1 && activeEntry != null)
                _txtName.Text = activeEntry.Name;
        }

        private void lstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isBinding)
                return;

            string[] selectedNames = SelectedGroupNames;
            _grpGroups.Text = selectedNames.Length > 1
                ? $"Member Groups ({_lstGroups.Items.Count}) - Selected {selectedNames.Length}"
                : $"Member Groups ({_lstGroups.Items.Count})";

            if (selectedNames.Length == 1)
                _txtName.Text = selectedNames[0];
            else if (selectedNames.Length > 1)
                _txtName.Clear();

            _pendingHighlightGroupNames = selectedNames;
            _pendingHighlightModelKey = _boundModelKey;
            _highlightTimer.Stop();
            if (selectedNames.Length > 0)
                _highlightTimer.Start();
        }

        private void HighlightTimer_Tick(object sender, EventArgs e)
        {
            _highlightTimer.Stop();
            string[] selectedNames = _pendingHighlightGroupNames;
            string expectedModelKey = _pendingHighlightModelKey;
            _pendingHighlightGroupNames = Array.Empty<string>();
            _pendingHighlightModelKey = string.Empty;
            if (selectedNames.Length == 0 || _isBinding)
                return;

            try
            {
                if (!TryGetBoundOpenStaad(expectedModelKey, out object openStaad, showWarning: false))
                    return;

                GroupService.HighlightMemberGroups(openStaad, selectedNames);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowWarning(ex.Message, "Groups");
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (!TryGetBoundOpenStaad(_boundModelKey, out object openStaad, showWarning: true))
                return;

            string groupName = (_txtName.Text ?? string.Empty).Trim();
            try
            {
                string createdGroupName = GroupService.CreateMemberGroupFromSelection(openStaad, groupName);
                _txtName.Text = createdGroupName;
                RefreshGroups(new[] { createdGroupName });
                TopMostMessageBox.ShowSuccess("Group created.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!TryGetBoundOpenStaad(_boundModelKey, out object openStaad, showWarning: true))
                return;

            if (!TryGetSingleSelectedGroupName(out string groupName))
                return;

            DialogResult confirm = TopMostMessageBox.Show(
                this,
                "Delete group '" + groupName + "'?",
                "Groups",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            try
            {
                GroupService.DeleteGroup(openStaad, groupName);
                _txtName.Clear();
                RefreshGroups();
                TopMostMessageBox.ShowSuccess("Group deleted.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private void btnAddSelected_Click(object sender, EventArgs e)
        {
            if (!TryGetBoundOpenStaad(_boundModelKey, out object openStaad, showWarning: true))
                return;

            if (!TryGetSingleSelectedGroupName(out string groupName))
                return;

            try
            {
                GroupService.AddSelectedMembersToGroup(openStaad, groupName);
                RefreshGroups(new[] { groupName });
                TopMostMessageBox.ShowSuccess("Selected members added.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (!TryGetBoundOpenStaad(_boundModelKey, out object openStaad, showWarning: true))
                return;

            if (!TryGetSingleSelectedGroupName(out string groupName))
                return;

            try
            {
                GroupService.RemoveSelectedMembersFromGroup(openStaad, groupName);
                RefreshGroups(new[] { groupName });
                TopMostMessageBox.ShowSuccess("Selected members removed.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private bool TryGetSingleSelectedGroupName(out string groupName)
        {
            string[] selectedNames = SelectedGroupNames;
            if (selectedNames.Length == 1)
            {
                groupName = selectedNames[0];
                return true;
            }

            groupName = string.Empty;
            TopMostMessageBox.ShowWarning(
                selectedNames.Length == 0
                    ? "Please select a group."
                    : "Please select only one group for this operation.",
                "Groups");
            return false;
        }

        private bool TryGetBoundOpenStaad(string expectedModelKey, out object openStaad, bool showWarning)
        {
            openStaad = null;
            if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string error))
            {
                if (showWarning)
                    TopMostMessageBox.ShowWarning(error, "Groups");
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedModelKey) ||
                !string.Equals(expectedModelKey, connection.Identity.CacheKey, StringComparison.OrdinalIgnoreCase))
            {
                if (showWarning)
                {
                    TopMostMessageBox.ShowWarning(
                        "The active STAAD model changed. The group list was refreshed; please review the selection and try again.",
                        "Groups");
                }
                return false;
            }

            openStaad = connection.OpenStaad;
            return true;
        }

        private void SaveLocation()
        {
            if (!_locationInitialized || _suppressLocationSave)
                return;

            AppSettings.Save("Groups", Location);
            AppSettings.SaveString("Groups_HasLocation", "1");
            AppSettings.SaveString("Groups_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
        }
    }
}
