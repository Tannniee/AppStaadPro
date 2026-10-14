using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class UsedSectionsForm : Form
    {
        private readonly UsedSectionsListBox _lstSections;
        private readonly GroupBox _grpSections;
        private bool _isBinding;
        private bool _locationInitialized;
        private bool _suppressLocationSave;
        private string _boundModelDisplayName = string.Empty;
        private string _connectionMessage = string.Empty;
        private bool _connectionReady;

        public event EventHandler SectionActivated;

        public UsedSectionEntry SelectedEntry => _lstSections.SelectedItem;

        public int SelectedEntryCount => _lstSections.SelectedItems.Count;

        public int[] SelectedPropertyIds
        {
            get
            {
                return _lstSections.SelectedItems
                    .SelectMany(entry => entry.PropertyIds ?? new[] { entry.PropertyId })
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
            }
        }

        public int[] SelectedBeamIds
        {
            get
            {
                return _lstSections.SelectedItems
                    .SelectMany(entry => entry.BeamIds ?? Array.Empty<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
            }
        }

        public UsedSectionsForm()
        {
            Text = "Used Sections";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(230, 360);

            _grpSections = new GroupBox
            {
                Text = "Used Sections",
                Location = new Point(12, 12),
                Size = new Size(206, 336)
            };

            _lstSections = new UsedSectionsListBox
            {
                Location = new Point(10, 20),
                Size = new Size(186, 306)
            };
            _lstSections.SelectedIndexChanged += (s, e) =>
            {
                if (_isBinding)
                    return;

                SectionActivated?.Invoke(this, EventArgs.Empty);
            };

            _grpSections.Controls.Add(_lstSections);
            Controls.Add(_grpSections);

            Move += (s, e) => SaveLocation();
        }

        public void SetEntries(IReadOnlyList<UsedSectionEntry> entries, int[] selectedPropertyIds = null, int selectedPropertyId = 0)
        {
            var items = entries == null ? new List<UsedSectionEntry>() : entries.ToList();
            var selectedIds = new HashSet<int>((selectedPropertyIds ?? Array.Empty<int>()).Where(id => id > 0));
            if (selectedPropertyId > 0)
            {
                selectedIds.Clear();
                selectedIds.Add(selectedPropertyId);
            }

            _isBinding = true;
            _lstSections.BeginUpdate();
            try
            {
                _lstSections.SetEntries(items);
                UpdateGroupCaption(items.Count);

                var indicesToSelect = new List<int>();
                int activeIndex = -1;
                if (selectedIds.Count > 0)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        int[] propertyIds = items[i].PropertyIds ?? new[] { items[i].PropertyId };
                        if (propertyIds.Any(selectedIds.Contains))
                        {
                            indicesToSelect.Add(i);
                            activeIndex = i;
                        }
                    }
                }

                _lstSections.SetSelection(indicesToSelect, indicesToSelect.Count > 0 ? indicesToSelect[0] : -1, activeIndex, notify: false);
            }
            finally
            {
                _lstSections.EndUpdate();
                _isBinding = false;
            }
        }

        public void SetWaitingState(string message)
        {
            _boundModelDisplayName = string.Empty;
            _connectionMessage = string.IsNullOrWhiteSpace(message)
                ? "Activate a STAAD model..."
                : message.Trim();
            _connectionReady = false;
            Text = "Used Sections - Waiting";
            _lstSections.Enabled = false;
            UpdateGroupCaption(0);
        }

        public void SetLoadingState(StaadModelIdentity identity)
        {
            _boundModelDisplayName = identity?.DisplayName ?? string.Empty;
            _connectionMessage = "Loading used sections...";
            _connectionReady = false;
            Text = string.IsNullOrWhiteSpace(_boundModelDisplayName)
                ? "Used Sections - Loading"
                : "Used Sections - " + _boundModelDisplayName;
            _lstSections.Enabled = false;
            UpdateGroupCaption(0);
        }

        public void SetConnectedState(StaadModelIdentity identity, int itemCount)
        {
            _boundModelDisplayName = identity?.DisplayName ?? string.Empty;
            _connectionMessage = string.Empty;
            _connectionReady = true;
            Text = string.IsNullOrWhiteSpace(_boundModelDisplayName)
                ? "Used Sections"
                : "Used Sections - " + _boundModelDisplayName;
            _lstSections.Enabled = true;
            UpdateGroupCaption(Math.Max(0, itemCount));
        }

        private void UpdateGroupCaption(int itemCount)
        {
            if (_connectionReady)
            {
                _grpSections.Text = $"Used Sections ({Math.Max(0, itemCount)})";
                return;
            }

            _grpSections.Text = string.IsNullOrWhiteSpace(_connectionMessage)
                ? "Activate a STAAD model..."
                : _connectionMessage;
        }

        public void RestoreLocation(Point defaultLocation)
        {
            _suppressLocationSave = true;
            try
            {
                string currentDisplaySignature = WindowPlacementHelper.GetDisplaySignature();
                string savedDisplaySignature = AppSettings.LoadString("UsedSections_DisplaySignature");
                bool hasLocation = string.Equals(AppSettings.LoadString("UsedSections_HasLocation"), "1", StringComparison.Ordinal);

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
                    location = WindowPlacementHelper.EnsureVisibleLocation(AppSettings.Load("UsedSections"), Size);
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

        private void SaveLocation()
        {
            if (!_locationInitialized || _suppressLocationSave)
                return;

            AppSettings.Save("UsedSections", Location);
            AppSettings.SaveString("UsedSections_HasLocation", "1");
            AppSettings.SaveString("UsedSections_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
        }
    }
}
