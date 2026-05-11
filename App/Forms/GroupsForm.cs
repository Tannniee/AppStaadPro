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
                IntegralHeight = false
            };
            _lstGroups.SelectedIndexChanged += lstGroups_SelectedIndexChanged;

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
            _btnRefresh.Click += (s, e) => RefreshGroups(SelectedGroupName);
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

        private string SelectedGroupName
        {
            get
            {
                if (_lstGroups.SelectedItem is GroupEntry entry)
                    return entry.Name;

                return (_txtName.Text ?? string.Empty).Trim();
            }
        }

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
            SaveLocation();
            base.OnFormClosing(e);
        }

        private void RefreshGroups(string selectedGroupName = null)
        {
            try
            {
                IReadOnlyList<GroupEntry> groups = GroupService.GetMemberGroups();
                string selectedName = string.IsNullOrWhiteSpace(selectedGroupName) ? SelectedGroupName : selectedGroupName;

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
                    SelectGroupByName(selectedName);
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

        private void SelectGroupByName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            for (int i = 0; i < _lstGroups.Items.Count; i++)
            {
                if (_lstGroups.Items[i] is GroupEntry entry &&
                    string.Equals(entry.Name, groupName, StringComparison.CurrentCultureIgnoreCase))
                {
                    _lstGroups.SelectedIndex = i;
                    _txtName.Text = entry.Name;
                    return;
                }
            }
        }

        private void lstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(_lstGroups.SelectedItem is GroupEntry entry))
                return;

            _txtName.Text = entry.Name;
            if (_isBinding)
                return;

            try
            {
                GroupService.HighlightMemberGroup(entry.Name);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowWarning(ex.Message, "Groups");
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string groupName = (_txtName.Text ?? string.Empty).Trim();
            try
            {
                string createdGroupName = GroupService.CreateMemberGroupFromSelection(groupName);
                _txtName.Text = createdGroupName;
                RefreshGroups(createdGroupName);
                TopMostMessageBox.ShowSuccess("Group created.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            string groupName = SelectedGroupName;
            if (string.IsNullOrWhiteSpace(groupName))
            {
                TopMostMessageBox.ShowWarning("Please select a group.", "Groups");
                return;
            }

            DialogResult confirm = MessageBox.Show(
                this,
                "Delete group '" + groupName + "'?",
                "Groups",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            try
            {
                GroupService.DeleteGroup(groupName);
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
            string groupName = SelectedGroupName;
            try
            {
                GroupService.AddSelectedMembersToGroup(groupName);
                RefreshGroups(groupName);
                TopMostMessageBox.ShowSuccess("Selected members added.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            string groupName = SelectedGroupName;
            try
            {
                GroupService.RemoveSelectedMembersFromGroup(groupName);
                RefreshGroups(groupName);
                TopMostMessageBox.ShowSuccess("Selected members removed.", "Groups");
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Groups");
            }
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
