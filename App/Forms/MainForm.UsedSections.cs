using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private bool _isRefreshingUsedSections;
        private string _usedSectionsBoundModelKey = string.Empty;
        private StaadConnectionSnapshot _usedSectionsBoundConnection;
        private string _pendingUsedSectionModelKey = string.Empty;

        private void InitializeUsedSectionsHighlightTimer()
        {
            if (_usedSectionsHighlightTimer != null)
                return;

            _usedSectionsHighlightTimer = new Timer { Interval = 120 };
            _usedSectionsHighlightTimer.Tick += UsedSectionsHighlightTimer_Tick;
        }

        private void DisposeUsedSectionsHighlightTimer()
        {
            if (_usedSectionsHighlightTimer == null)
                return;

            _usedSectionsHighlightTimer.Stop();
            _usedSectionsHighlightTimer.Dispose();
            _usedSectionsHighlightTimer = null;
        }

        private void chkUsedSections_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUsedSections.Checked)
            {
                ShowUsedSectionsForm();
            }
            else
            {
                HideUsedSectionsForm();
            }
        }

        private void ShowUsedSectionsForm()
        {
            EnsureUsedSectionsForm();
            if (_usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            if (!_usedSectionsForm.Visible)
            {
                _usedSectionsForm.Show(this);
            }

            _usedSectionsForm.BringToFront();
            RefreshUsedSectionsList();
        }

        private void HideUsedSectionsForm()
        {
            if (_usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            _usedSectionsForm.Hide();
        }

        private void EnsureUsedSectionsForm()
        {
            if (_usedSectionsForm != null && !_usedSectionsForm.IsDisposed)
                return;

            _usedSectionsForm = new UsedSectionsForm();
            _usedSectionsForm.SetWaitingState("Activate a STAAD model...");
            _usedSectionsForm.RestoreLocation(GetDefaultUsedSectionsLocation());
            _usedSectionsForm.SectionActivated += UsedSectionsForm_SectionActivated;
            _usedSectionsForm.FormClosing += UsedSectionsForm_FormClosing;

            if (_usedSectionsForm.Owner != this)
            {
                AddOwnedForm(_usedSectionsForm);
            }
        }

        private void UsedSectionsForm_SectionActivated(object sender, EventArgs e)
        {
            if (_isRefreshingUsedSections || _usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            string expectedModelKey = _usedSectionsBoundModelKey;
            if (!TryGetUsedSectionsBoundOpenStaad(expectedModelKey, out object openStaad, showWarning: true))
                return;

            if (_usedSectionsForm.SelectedEntryCount == 1)
            {
                PopulateUiFromUsedSection(openStaad, _usedSectionsForm.SelectedEntry);
            }

            int[] propertyIds = _usedSectionsForm.SelectedPropertyIds;
            int[] beamIds = _usedSectionsForm.SelectedBeamIds;
            if ((propertyIds == null || propertyIds.Length == 0) &&
                (beamIds == null || beamIds.Length == 0))
                return;

            _pendingUsedSectionPropertyIds = propertyIds;
            _pendingUsedSectionBeamIds = beamIds;
            _pendingUsedSectionModelKey = expectedModelKey;
            _usedSectionsHighlightTimer?.Stop();
            _usedSectionsHighlightTimer?.Start();
        }

        private void UsedSectionsHighlightTimer_Tick(object sender, EventArgs e)
        {
            _usedSectionsHighlightTimer?.Stop();

            int[] beamIds = _pendingUsedSectionBeamIds;
            int[] propertyIds = _pendingUsedSectionPropertyIds;
            string expectedModelKey = _pendingUsedSectionModelKey;
            _pendingUsedSectionBeamIds = null;
            _pendingUsedSectionPropertyIds = null;
            _pendingUsedSectionModelKey = string.Empty;
            if ((beamIds == null || beamIds.Length == 0) &&
                (propertyIds == null || propertyIds.Length == 0))
                return;

            if (_isRefreshingUsedSections || _usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            if (!TryGetUsedSectionsBoundOpenStaad(expectedModelKey, out object openStaad, showWarning: false))
                return;

            if (beamIds != null && beamIds.Length > 0)
            {
                if (SectionService.HighlightBeamIds(openStaad, beamIds, out string beamError))
                    return;

                if (propertyIds == null || propertyIds.Length == 0)
                {
                    TopMostMessageBox.ShowWarning(beamError, "Used Sections");
                    return;
                }
            }

            if (propertyIds != null && propertyIds.Length > 0)
            {
                string propertyError;
                if (!SectionService.HighlightUsedSections(openStaad, propertyIds, out propertyError))
                {
                    TopMostMessageBox.ShowWarning(propertyError, "Used Sections");
                }
            }
        }

        private void PopulateUiFromUsedSection(object openStaad, UsedSectionEntry entry)
        {
            if (entry == null || entry.PropertyId <= 0)
                return;

            try
            {
                var info = SectionService.DetectSectionByPropertyId(openStaad, entry.PropertyId);
                if (info == null)
                    return;

                _isPopulating = true;
                try
                {
                    SetSectionInfoToUI(info, openStaad);
                }
                finally
                {
                    _isPopulating = false;
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowWarning("Could not load used section dimensions: " + ex.Message, "Used Sections");
            }
        }

        private void UsedSectionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _usedSectionsForm.Hide();
                if (chkUsedSections.Checked)
                    chkUsedSections.Checked = false;
            }
        }

        private Point GetDefaultUsedSectionsLocation()
        {
            int x = this.Right - 5;
            int y = this.Top;
            return new Point(x, y);
        }

        private void RefreshUsedSectionsListIfVisible(int selectedPropertyId = 0)
        {
            if (!chkUsedSections.Checked)
                return;

            if (_usedSectionsForm == null || _usedSectionsForm.IsDisposed || !_usedSectionsForm.Visible)
                return;

            RefreshUsedSectionsList(selectedPropertyId);
        }

        private void RefreshUsedSectionsList(int selectedPropertyId = 0)
        {
            if (!chkUsedSections.Checked)
                return;

            EnsureUsedSectionsForm();
            if (_usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionError))
            {
                _usedSectionsBoundConnection = null;
                _usedSectionsBoundModelKey = string.Empty;
                _usedSectionsForm.SetEntries(Array.Empty<UsedSectionEntry>());
                _usedSectionsForm.SetWaitingState("Activate a STAAD model...");
                return;
            }

            object openStaad = connection.OpenStaad;

            int[] selectedPropertyIds = _usedSectionsForm.SelectedPropertyIds;

            _isRefreshingUsedSections = true;
            try
            {
                _usedSectionsForm.SetLoadingState(connection.Identity);
                var items = SectionService.GetUsedSections(openStaad);
                _usedSectionsForm.SetEntries(items, selectedPropertyIds, selectedPropertyId);
                _usedSectionsBoundConnection = connection;
                _usedSectionsBoundModelKey = connection.Identity.CacheKey;
                _usedSectionsForm.SetConnectedState(connection.Identity, items.Count);
            }
            catch (Exception ex)
            {
                _usedSectionsBoundConnection = null;
                _usedSectionsBoundModelKey = string.Empty;
                _usedSectionsForm.SetEntries(Array.Empty<UsedSectionEntry>());
                _usedSectionsForm.SetWaitingState("Could not load used sections.");
                TopMostMessageBox.ShowWarning("Could not refresh used sections list: " + ex.Message, "Used Sections");
            }
            finally
            {
                _isRefreshingUsedSections = false;
            }
        }

        private bool TryGetUsedSectionsBoundOpenStaad(
            string expectedModelKey,
            out object openStaad,
            bool showWarning)
        {
            openStaad = null;
            StaadConnectionSnapshot connection = _usedSectionsBoundConnection;
            if (connection == null)
            {
                if (showWarning)
                    TopMostMessageBox.ShowWarning(
                        "Used Sections is waiting for an active STAAD model.",
                        "Used Sections");
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedModelKey) ||
                !string.Equals(expectedModelKey, connection.Identity.CacheKey, StringComparison.OrdinalIgnoreCase))
            {
                if (showWarning)
                {
                    TopMostMessageBox.ShowWarning(
                        "The active STAAD model changed. Used Sections was refreshed; please select the section again.",
                        "Used Sections");
                }
                return false;
            }

            if (!StaadManager.TryValidateConnectionSnapshot(connection, out string validationError))
            {
                _usedSectionsBoundConnection = null;
                _usedSectionsBoundModelKey = string.Empty;
                _usedSectionsForm?.SetEntries(Array.Empty<UsedSectionEntry>());
                _usedSectionsForm?.SetWaitingState("Activate a STAAD model...");
                if (showWarning)
                    TopMostMessageBox.ShowWarning(validationError, "Used Sections");
                return false;
            }

            openStaad = connection.OpenStaad;
            return true;
        }
    }
}
