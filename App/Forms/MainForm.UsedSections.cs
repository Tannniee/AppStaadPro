using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private bool _isRefreshingUsedSections;

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

            if (_usedSectionsForm.SelectedEntryCount == 1)
            {
                PopulateUiFromUsedSection(_usedSectionsForm.SelectedEntry);
            }

            int[] propertyIds = _usedSectionsForm.SelectedPropertyIds;
            int[] beamIds = _usedSectionsForm.SelectedBeamIds;
            if ((propertyIds == null || propertyIds.Length == 0) &&
                (beamIds == null || beamIds.Length == 0))
                return;

            _pendingUsedSectionPropertyIds = propertyIds;
            _pendingUsedSectionBeamIds = beamIds;
            _usedSectionsHighlightTimer?.Stop();
            _usedSectionsHighlightTimer?.Start();
        }

        private void UsedSectionsHighlightTimer_Tick(object sender, EventArgs e)
        {
            _usedSectionsHighlightTimer?.Stop();

            int[] beamIds = _pendingUsedSectionBeamIds;
            int[] propertyIds = _pendingUsedSectionPropertyIds;
            _pendingUsedSectionBeamIds = null;
            _pendingUsedSectionPropertyIds = null;
            if ((beamIds == null || beamIds.Length == 0) &&
                (propertyIds == null || propertyIds.Length == 0))
                return;

            if (_isRefreshingUsedSections || _usedSectionsForm == null || _usedSectionsForm.IsDisposed)
                return;

            if (!TryGetMainOpenStaad(out object openStaad)) return;

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

        private void PopulateUiFromUsedSection(UsedSectionEntry entry)
        {
            if (entry == null || entry.PropertyId <= 0)
                return;

            if (!TryGetMainOpenStaad(out object openStaad)) return;

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

            if (!TryGetMainOpenStaad(out object openStaad)) return;

            int[] selectedPropertyIds = _usedSectionsForm.SelectedPropertyIds;

            _isRefreshingUsedSections = true;
            try
            {
                var items = SectionService.GetUsedSections(openStaad);
                _usedSectionsForm.SetEntries(items, selectedPropertyIds, selectedPropertyId);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowWarning("Could not refresh used sections list: " + ex.Message, "Used Sections");
            }
            finally
            {
                _isRefreshingUsedSections = false;
            }
        }
    }
}
