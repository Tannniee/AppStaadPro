using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class AdvanceAlignForm : Form
    {
        private const string WindowSettingsKey = "Advance";

        private readonly Timer _selectionSyncTimer;
        private readonly List<IAdvanceActionPanel> _actionPanels = new List<IAdvanceActionPanel>();
        private readonly AdvanceActionContext _actionContext;
        private Panel _panelActionHost;
        private Point _actionLocationWithSelection;
        private Point _actionLocationWithoutSelection;
        private Size _actionSizeWithSelection;
        private int[] _selectedNodes = Array.Empty<int>();

        public AdvanceAlignForm()
        {
            InitializeComponent();

            _actionContext = new AdvanceActionContext
            {
                GetSelectedNodes = () => _selectedNodes.ToArray(),
                ReloadSelection = ReloadSelection,
                UpdateHint = UpdateHint
            };

            RegisterActionPanels();
            InitializeActionHost();
            CaptureAdaptiveLayout();
            SelectDefaultAction();

            _selectionSyncTimer = new Timer { Interval = 400 };
            _selectionSyncTimer.Tick += SelectionSyncTimer_Tick;
            Load += AdvanceAlignForm_Load;
            FormClosing += AdvanceAlignForm_FormClosing;
        }

        private void RegisterActionPanels()
        {
            _actionPanels.Clear();
            _actionPanels.Add(new LineAlignPanel());
            _actionPanels.Add(new TemplateAlignPanel());
            _actionPanels.Add(new ConnectNodesPanel());
            _actionPanels.Add(new BeamSegmentPanel());
            _actionPanels.Add(new FrameMovePanel());
            _actionPanels.Add(new FrameRotatePanel());
            _actionPanels.Add(new BeamVectorNodePanel());
            _actionPanels.Add(new BeamPlanePanel());
            _actionPanels.Add(new ConvertUptPanel());
            _actionPanels.Add(new MergeSectionsPanel());
            _actionPanels.Add(new MergeMemberReleasesPanel());
            _actionPanels.Add(new LoadFragmentPreviewPanel());
        }

        private void InitializeActionHost()
        {
            _grpAction.Controls.Clear();
            _panelActionHost = new Panel
            {
                Location = new Point(10, 24),
                Name = "_panelActionHost",
                Size = new Size(328, 160)
            };
            _grpAction.Controls.Add(_panelActionHost);

            _cboAction.Items.Clear();
            foreach (IAdvanceActionPanel panel in _actionPanels)
            {
                panel.AttachContext(_actionContext);
                Control view = panel.View;
                view.Dock = DockStyle.Fill;
                view.Visible = false;
                _panelActionHost.Controls.Add(view);
                _cboAction.Items.Add(panel.ActionText);
            }

        }

        private void SelectDefaultAction()
        {
            if (_cboAction.Items.Count > 0)
                _cboAction.SelectedIndex = 0;
        }

        private void AdvanceAlignForm_Load(object sender, EventArgs e)
        {
            RestoreWindowPosition();
            _actionContext.CoordinateUnitLabel = AdvanceService.GetCoordinateLengthUnitLabel();
            foreach (IAdvanceActionPanel panel in _actionPanels)
                panel.AttachContext(_actionContext);

            ReloadSelection();
            UpdateActionUi();
            _selectionSyncTimer.Start();
        }

        private void AdvanceAlignForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _selectionSyncTimer.Stop();
            SaveWindowPosition();
        }

        private void RestoreWindowPosition()
        {
            try
            {
                if (!string.Equals(AppSettings.LoadString(WindowSettingsKey + "_HasSavedLocation"), "1", StringComparison.Ordinal))
                    return;

                string currentDisplaySignature = WindowPlacementHelper.GetDisplaySignature();
                string savedDisplaySignature = AppSettings.LoadString(WindowSettingsKey + "_DisplaySignature");
                Point restoredLocation = string.Equals(savedDisplaySignature, currentDisplaySignature, StringComparison.Ordinal)
                    ? WindowPlacementHelper.EnsureVisibleLocation(AppSettings.Load(WindowSettingsKey), Size)
                    : WindowPlacementHelper.GetDefaultVisibleLocation(Size);

                StartPosition = FormStartPosition.Manual;
                Location = restoredLocation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Advance position restore failed: {ex}");
            }
        }

        private void SaveWindowPosition()
        {
            try
            {
                AppSettings.Save(WindowSettingsKey, Location);
                AppSettings.SaveString(WindowSettingsKey + "_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
                AppSettings.SaveString(WindowSettingsKey + "_HasSavedLocation", "1");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Advance position save failed: {ex}");
            }
        }

        private void ReloadSelection()
        {
            SyncSelection(showErrors: true, forceRefresh: true);
        }

        private void SyncSelection(bool showErrors, bool forceRefresh = false)
        {
            if (_lblSelectionInfo == null || _lblSelectionIds == null)
                return;

            if (!AdvanceService.TryGetSelectedNodes(out int[] latestSelection, out string error))
            {
                if (showErrors)
                {
                    _selectedNodes = Array.Empty<int>();
                    ApplySelectionToUi();
                    TopMostMessageBox.ShowWarning(error, "Advance");
                }

                return;
            }

            if (!forceRefresh && _selectedNodes.SequenceEqual(latestSelection))
                return;

            _selectedNodes = latestSelection ?? Array.Empty<int>();
            ApplySelectionToUi();
        }

        private void ApplySelectionToUi()
        {
            _lblSelectionInfo.Text = $"OpenSTAAD selected nodes: {_selectedNodes.Length}";
            _lblSelectionIds.Text = _selectedNodes.Length > 0
                ? "IDs: " + string.Join(", ", _selectedNodes.Take(12)) + (_selectedNodes.Length > 12 ? ", ..." : "")
                : "IDs: None";

            foreach (IAdvanceActionPanel panel in _actionPanels)
                panel.OnSelectionChanged(_selectedNodes);

            UpdateHint();
        }

        private void UpdateActionUi()
        {
            IAdvanceActionPanel current = GetCurrentActionPanel();
            if (current == null)
                return;

            foreach (IAdvanceActionPanel panel in _actionPanels)
                panel.View.Visible = ReferenceEquals(panel, current);

            ApplySelectionPanelLayout(current.UsesNodeSelection);
            current.View.BringToFront();
            _btnApply.Text = current.ApplyText;
            UpdateHint();
        }

        private void CaptureAdaptiveLayout()
        {
            _actionLocationWithSelection = _grpAction.Location;
            _actionSizeWithSelection = _grpAction.Size;
            _actionLocationWithoutSelection = _grpSelection.Location;
        }

        private void ApplySelectionPanelLayout(bool usesNodeSelection)
        {
            _grpSelection.Visible = usesNodeSelection;
            _grpAction.Location = usesNodeSelection ? _actionLocationWithSelection : _actionLocationWithoutSelection;
            _grpAction.Size = _actionSizeWithSelection;
            _grpAction.Visible = true;

            if (_panelActionHost != null)
            {
                _panelActionHost.Location = new Point(10, 24);
                _panelActionHost.Size = new Size(_grpAction.ClientSize.Width - 20, _grpAction.ClientSize.Height - 34);
                _panelActionHost.Visible = true;
                _panelActionHost.BringToFront();
            }

            if (usesNodeSelection)
            {
                _grpSelection.BringToFront();
                SyncSelection(showErrors: false, forceRefresh: true);
            }
            else
            {
                _grpAction.BringToFront();
            }
        }

        private void UpdateHint()
        {
            if (_lblHint == null)
                return;

            IAdvanceActionPanel current = GetCurrentActionPanel();
            if (current == null)
            {
                _lblHint.ForeColor = Color.Firebrick;
                _lblHint.Text = "No Advance action selected.";
                return;
            }

            AdvanceActionHint hint = current.GetHint();
            _lblHint.ForeColor = hint.ForeColor;
            _lblHint.Text = hint.Text;
        }

        private IAdvanceActionPanel GetCurrentActionPanel()
        {
            int index = _cboAction?.SelectedIndex ?? -1;
            if (index < 0 || index >= _actionPanels.Count)
                return null;

            return _actionPanels[index];
        }

        private void CboAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionUi();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            IAdvanceActionPanel current = GetCurrentActionPanel();
            if (current == null)
                return;

            current.Apply();
            UpdateHint();
        }

        private void BtnReloadSelection_Click(object sender, EventArgs e)
        {
            ReloadSelection();
        }

        private void SelectionSyncTimer_Tick(object sender, EventArgs e)
        {
            if (!Visible || IsDisposed)
                return;

            IAdvanceActionPanel current = GetCurrentActionPanel();
            if (current != null && !current.UsesNodeSelection)
                return;

            SyncSelection(showErrors: false);
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RootMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void CboRootNode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void TxtRootNodeManual_TextChanged(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void BtnPickRootHighlight_Click(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void ConnectMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void TemplateSource_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHint();
        }

        private void _lblTitle_Click(object sender, EventArgs e)
        {
        }

        private void _lblHint_Click(object sender, EventArgs e)
        {
        }
    }
}
