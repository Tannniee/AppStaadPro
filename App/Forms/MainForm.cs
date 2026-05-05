using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using StaadSteelApp.Core;
using System.Diagnostics;


namespace StaadSteelApp
{
    public partial class MainForm : Form
    {
        // Flag to prevent UpdateName during programmatic population
        private bool _isPopulating = false;
        private bool _materialListLoaded = false;
        private TypeSpecForm _sidePanel;
        private StatusForm _statusPanel;
        private MiscToolsForm _miscPanel;
        private UsedSectionsForm _usedSectionsForm;
        private AdvanceAlignForm _advanceForm;
        private AssignLoadForm _assignLoadForm;
        private Timer _usedSectionsHighlightTimer;
        private int[] _pendingUsedSectionPropertyIds;
        private int[] _pendingUsedSectionBeamIds;

        public MainForm()
        {
            InitializeComponent();
            
            // Check for Design Mode (Robust)
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // Setup events
            this.Load += (s,e) => 
            {
                InitializeLegacyService();
                LoadLegacyZones();  // Initial legacy load

                // Initialize Status Panel (Persistent)
                _statusPanel = StatusForm.Instance;
                if (_statusPanel == null || _statusPanel.IsDisposed)
                {
                    _statusPanel = new StatusForm();
                }

                if (_statusPanel.Owner != this)
                {
                    this.AddOwnedForm(_statusPanel);
                }
                UpdateStatusPanelPos();
                _statusPanel.Hide(); // Default hidden; it will auto-show on messages

                if (_usedSectionsHighlightTimer == null)
                {
                    _usedSectionsHighlightTimer = new Timer { Interval = 120 };
                    _usedSectionsHighlightTimer.Tick += UsedSectionsHighlightTimer_Tick;
                }

                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed && chkUsedSections != null && !chkUsedSections.Checked)
                        chkUsedSections.Checked = true;
                }));
            };

            // Keep status panel aligned
            this.Move += (s, e) => { UpdateStatusPanelPos(); UpdateMiscPanelLocation(); };
            this.Resize += (s, e) => { UpdateStatusPanelPos(); UpdateMiscPanelLocation(); };




        // --- Clipboard ---
        // Moved to SectionClipboard.cs and MainForm.Clipboard.cs
           
            // Code shows source (Prismatic/Legacy), Name is auto-generated from dimensions
            txtName.ReadOnly = true;
            txtCode.ReadOnly = true;

            // Wire ComboBox
            cboType.SelectedIndexChanged += (s, e) => { UpdateVisibility(); UpdateName(); };
            
            EventHandler textChanged = (s, e) => UpdateName();
            txtH1.TextChanged += textChanged;
            txtH2.TextChanged += textChanged;
            txtTw.TextChanged += textChanged;
            txtBf.TextChanged += textChanged;
            txtTf.TextChanged += textChanged;
            txtBfb.TextChanged += textChanged;
            txtTfb.TextChanged += textChanged;
            
            // New field Dis
            txtDis.TextChanged += textChanged;
            
            // Setup behaviors for input fields
            SetupInputFields();

            // Default
            if (cboType.Items.Count > 0) cboType.SelectedIndex = 0;
            UpdateVisibility();
            
            try
            {
                this.StartPosition = FormStartPosition.Manual;

                string resetToCenterFlag = AppSettings.LoadString("Main_ResetToCenter");
                string currentDisplaySignature = WindowPlacementHelper.GetDisplaySignature();
                string savedDisplaySignature = AppSettings.LoadString("Main_DisplaySignature");
                bool displayLayoutChanged = !string.IsNullOrWhiteSpace(savedDisplaySignature) &&
                    !string.Equals(savedDisplaySignature, currentDisplaySignature, StringComparison.Ordinal);

                Point restoredLocation;
                if (string.Equals(resetToCenterFlag, "1", StringComparison.Ordinal))
                {
                    restoredLocation = WindowPlacementHelper.GetCenteredLocation(this.Size);
                }
                else if (displayLayoutChanged)
                {
                    restoredLocation = WindowPlacementHelper.GetDefaultVisibleLocation(this.Size);
                }
                else
                {
                    restoredLocation = WindowPlacementHelper.EnsureVisibleLocation(AppSettings.Load("Main"), this.Size);
                }

                this.Location = restoredLocation;
                AppSettings.Save("Main", restoredLocation);
                AppSettings.SaveString("Main_DisplaySignature", currentDisplaySignature);
                AppSettings.SaveString("Main_ResetToCenter", "0");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainForm startup position restore failed: {ex}");
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            AppSettings.Save("Main", this.Location);
            AppSettings.SaveString("Main_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
            AppSettings.SaveString("Main_ResetToCenter", "0");
            _miscPanel?.Close();
            _usedSectionsForm?.Close();
            _advanceForm?.Close();
            _assignLoadForm?.Close();
            _usedSectionsHighlightTimer?.Stop();
            _usedSectionsHighlightTimer?.Dispose();
            _statusPanel?.Close();
            base.OnFormClosing(e);
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

            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return;

            if (beamIds != null && beamIds.Length > 0)
            {
                if (SectionService.HighlightBeamIds(os, beamIds, out string beamError))
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
                if (!SectionService.HighlightUsedSections(os, propertyIds, out propertyError))
                {
                    TopMostMessageBox.ShowWarning(propertyError, "Used Sections");
                }
            }
        }

        internal void ResetWindowToCenter()
        {
            StartPosition = FormStartPosition.Manual;
            Location = WindowPlacementHelper.GetCenteredLocation(Size);

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            AppSettings.Save("Main", Location);
            AppSettings.SaveString("Main_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
            AppSettings.SaveString("Main_ResetToCenter", "0");

            UpdateSidePanelLocation();
            UpdateStatusPanelPos();
            UpdateMiscPanelLocation();
            Activate();
            BringToFront();
            Focus();
        }

        private void UpdateStatusPanelPos()
        {
            if (_statusPanel != null && !_statusPanel.IsDisposed && !this.Disposing)
            {
                // Position: Right side of MainForm, Top aligned
                // Overlap -5px to match TypeSpec side panel style
                _statusPanel.Location = new System.Drawing.Point(this.Right - 5, this.Top);
            }
        }

        private void UpdateMiscPanelLocation()
        {
            if (_miscPanel == null || _miscPanel.IsDisposed || !_miscPanel.Visible) return;

            int top = this.Top + 195;
            if (_sidePanel != null && _sidePanel.Visible)
            {
                top += _sidePanel.Height + 5;
            }

            _miscPanel.Location = new System.Drawing.Point(this.Right - 5, top);
        }

        private void chkUPT_CheckedChanged(object sender, EventArgs e)
        {
            string currentSelection = cboType.SelectedItem?.ToString();
            
            // Tat cap nhat ten de tranh loi thay doi Text lien tuc
            _isPopulating = true;
            cboType.Items.Clear();

            if (chkUPT.Checked)
            {
                // Cac loai tiet dien UPT theo OpenSTAAD
                cboType.Items.AddRange(new object[] {
                    "WIDE FLANGE",
                    "CHANNEL",
                    "ANGLE",
                    "DOUBLE ANGLE",
                    "TEE",
                    "PIPE",
                    "TUBE",
                    "GENERAL",
                    "ISECTION",
                    "PRISMATIC"
                });
            }
            else
            {
                // Cac loai tiet dien mac dinh cua app
                cboType.Items.AddRange(new object[] {
                    "Tapered I",
                    "Circle",
                    "Rectangle",
                    "Tee",
                    "Trapezoidal",
                    "General",
                    "Tapered Tube",
                    "Legacy"
                });
            }

            // Thu giu lai lua chon cu neu co trong danh sach moi (vi du: "Tee" <-> "TEE")
            if (!string.IsNullOrEmpty(currentSelection))
            {
                for (int i = 0; i < cboType.Items.Count; i++)
                {
                    if (cboType.Items[i].ToString().Equals(currentSelection, StringComparison.OrdinalIgnoreCase))
                    {
                        cboType.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Neu khong match duoc, chon phan tu dau tien
            if (cboType.SelectedIndex == -1 && cboType.Items.Count > 0)
            {
                cboType.SelectedIndex = 0;
            }

            _isPopulating = false;
            
            // Kich hoat cap nhat UI
            UpdateVisibility();
            UpdateName();

            if (chkUPT.Checked)
            {
                try
                {
                    var os = StaadManager.GetOpenSTAAD();
                    string preferredMaterial = string.IsNullOrWhiteSpace(cboMaterial.Text) ? "None" : cboMaterial.Text;
                    EnsureMaterialListLoaded(os, preferredMaterial);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Load material list for UPT failed: " + ex.Message);
                }
            }
        }
    }
}
