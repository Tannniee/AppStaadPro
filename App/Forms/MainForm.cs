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
        // Suppresses name recalculation while controls are populated programmatically.
        private bool _isPopulating = false;
        private string _materialListModelKey = string.Empty;
        private TypeSpecForm _sidePanel;
        private StatusForm _statusPanel;
        private MiscToolsForm _miscPanel;
        private UsedSectionsForm _usedSectionsForm;
        private AdvanceAlignForm _advanceForm;
        private AssignLoadForm _assignLoadForm;
        private GroupsForm _groupsForm;
        private Timer _usedSectionsHighlightTimer;
        private int[] _pendingUsedSectionPropertyIds;
        private int[] _pendingUsedSectionBeamIds;

        public MainForm()
        {
            InitializeComponent();
            
            // Skip runtime wiring while the WinForms designer is loading the form.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // Runtime startup wiring.
            this.Load += (s,e) => 
            {
                InitializeLegacyService();
                LoadLegacyZones();

                InitializeStatusPanel();
                InitializeStaadConnectionStatus();

                InitializeUsedSectionsHighlightTimer();

                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed && chkUsedSections != null && !chkUsedSections.Checked)
                        chkUsedSections.Checked = true;
                }));
            };

            // Keep owned tool panels aligned with the main form.
            this.Move += (s, e) => { UpdateStatusPanelPos(); UpdateMiscPanelLocation(); };
            this.Resize += (s, e) => { UpdateStatusPanelPos(); UpdateMiscPanelLocation(); };

            // Code shows source (Prismatic/Legacy), Name is auto-generated from dimensions
            txtName.ReadOnly = true;
            txtCode.ReadOnly = true;

            // Recalculate dimensions and generated names when the section type changes.
            cboType.SelectedIndexChanged += (s, e) => { UpdateVisibility(); UpdateName(); };
            
            EventHandler textChanged = (s, e) => UpdateName();
            txtH1.TextChanged += textChanged;
            txtH2.TextChanged += textChanged;
            txtTw.TextChanged += textChanged;
            txtBf.TextChanged += textChanged;
            txtTf.TextChanged += textChanged;
            txtBfb.TextChanged += textChanged;
            txtTfb.TextChanged += textChanged;
            
            // Distance parameter used by supported prismatic section types.
            txtDis.TextChanged += textChanged;
            
            // Apply shared input-field behavior.
            SetupInputFields();

            // Initial section type.
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
            _groupsForm?.Close();
            DisposeUsedSectionsHighlightTimer();
            DisposeStaadConnectionStatus();
            CloseStatusPanel();
            base.OnFormClosing(e);
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
            
            // Disable name updates while the type list is being rebuilt.
            _isPopulating = true;
            cboType.Items.Clear();

            if (chkUPT.Checked)
            {
                // UPT section types supported through OpenSTAAD.
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
                // Default app section types.
                cboType.Items.AddRange(new object[] {
                    "Tapered I",
                    "Circle",
                    "Rectangle",
                    "Tee",
                     "Trapezoidal",
                     "General",
                     "Tapered Pipe",
                     "Tapered Tube",
                     "Legacy"
                });
            }

            // Preserve the current type when the rebuilt list contains an equivalent entry.
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

            // Fall back to the first available type when the previous type is not available.
            if (cboType.SelectedIndex == -1 && cboType.Items.Count > 0)
            {
                cboType.SelectedIndex = 0;
            }

            _isPopulating = false;
            
            // Refresh dependent UI after the type list changes.
            UpdateVisibility();
            UpdateName();

        }
    }
}
