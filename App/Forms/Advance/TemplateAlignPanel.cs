using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class TemplateAlignPanel : AdvanceActionPanelBase
    {
        private readonly RadioButton _rdoTemplateFromNode;
        private readonly RadioButton _rdoTemplateFromCoordinates;
        private readonly Label _lblTemplateNode;
        private readonly TextBox _txtTemplateNode;
        private readonly Label _lblTargetCoords;
        private readonly Label _lblTargetX;
        private readonly TextBox _txtTargetX;
        private readonly Label _lblTargetY;
        private readonly TextBox _txtTargetY;
        private readonly Label _lblTargetZ;
        private readonly TextBox _txtTargetZ;
        private readonly CheckBox _chkAlignX;
        private readonly CheckBox _chkAlignY;
        private readonly CheckBox _chkAlignZ;
        private readonly RadioButton _rdoAlignNodes;
        private readonly RadioButton _rdoCopyNodes;

        public override string ActionText => "Align / copy selected nodes (template/manual)";
        public override string ApplyText => _rdoCopyNodes != null && _rdoCopyNodes.Checked
            ? "Copy Nodes"
            : "Align Nodes";

        public TemplateAlignPanel()
        {
            AddLabel(this, "_lblTemplateSource", "Source", 2, 8);
            var sourceGroup = AddPanel(this, "_panelTemplateSourceGroup", 90, 2, 220, 23);
            _rdoTemplateFromNode = AddRadio(sourceGroup, "_rdoTemplateFromNode", "Template node", 0, true, TemplateSourceChanged);
            _rdoTemplateFromCoordinates = AddRadio(sourceGroup, "_rdoTemplateFromCoordinates", "Manual XYZ", 116, false, TemplateSourceChanged);

            _lblTemplateNode = AddLabel(this, "_lblTemplateNode", "Template node", 2, 39);
            _txtTemplateNode = AddTextBox(this, "_txtTemplateNode", 90, 35, 232);

            _lblTargetCoords = AddLabel(this, "_lblTargetCoords", "New coords", 2, 72);
            _lblTargetX = AddLabel(this, "_lblTargetX", "X", 90, 72);
            _txtTargetX = AddTextBox(this, "_txtTargetX", 106, 68, 56);
            _lblTargetY = AddLabel(this, "_lblTargetY", "Y", 164, 72);
            _txtTargetY = AddTextBox(this, "_txtTargetY", 180, 68, 56);
            _lblTargetZ = AddLabel(this, "_lblTargetZ", "Z", 238, 72);
            _txtTargetZ = AddTextBox(this, "_txtTargetZ", 254, 68, 46);

            AddLabel(this, "_lblTemplateAxes", "Align axes", 2, 104);
            _chkAlignX = AddCheck(this, "_chkAlignX", "X", 90, 102, true, InputChanged);
            _chkAlignY = AddCheck(this, "_chkAlignY", "Y", 140, 102, false, InputChanged);
            _chkAlignZ = AddCheck(this, "_chkAlignZ", "Z", 190, 102, false, InputChanged);

            AddLabel(this, "_lblResultMode", "Result", 2, 136);
            var resultGroup = AddPanel(this, "_panelResultMode", 90, 130, 232, 25);
            _rdoAlignNodes = AddRadio(resultGroup, "_rdoAlignNodes", "Align (move)", 0, true, CopyModeChanged);
            _rdoCopyNodes = AddRadio(resultGroup, "_rdoCopyNodes", "Copy (new nodes)", 104, false, CopyModeChanged);

            _txtTemplateNode.TextChanged += InputChanged;
            _txtTargetX.TextChanged += InputChanged;
            _txtTargetY.TextChanged += InputChanged;
            _txtTargetZ.TextChanged += InputChanged;
            UpdateTemplateSourceUi();
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            UpdateTemplateSourceUi();
        }

        public override void OnSelectionChanged(int[] selectedNodes)
        {
            base.OnSelectionChanged(selectedNodes);
            if (SelectedNodes.Length == 0)
                _txtTemplateNode.Clear();
            else if (string.IsNullOrWhiteSpace(_txtTemplateNode.Text))
                _txtTemplateNode.Text = SelectedNodes[0].ToString();
        }

        public override AdvanceActionHint GetHint()
        {
            if (SelectedNodes.Length == 0)
                return Error("Template mode requires at least 1 selected node.");

            bool useManualCoordinates = _rdoTemplateFromCoordinates.Checked;
            bool copyNodes = _rdoCopyNodes.Checked;
            if (copyNodes)
            {
                return Info(useManualCoordinates
                    ? "New nodes are copied to entered X/Y/Z values on checked axes; originals stay unchanged."
                    : "New nodes are copied to template node coordinates on checked axes; originals stay unchanged.");
            }

            return Info(useManualCoordinates
                ? "Selected nodes are moved to entered X/Y/Z values on checked axes."
                : "Selected nodes are moved to match template node coordinates on checked axes.");
        }

        public override bool Apply()
        {
            if (SelectedNodes.Length == 0)
            {
                Context.ShowWarning("Please select at least one node in STAAD.");
                return false;
            }

            bool alignX = _chkAlignX.Checked;
            bool alignY = _chkAlignY.Checked;
            bool alignZ = _chkAlignZ.Checked;
            if (!alignX && !alignY && !alignZ)
            {
                Context.ShowWarning("Please choose at least one axis (X/Y/Z).");
                return false;
            }

            bool ok;
            int affectedCount;
            string message;
            bool copyNodes = _rdoCopyNodes.Checked;
            if (_rdoTemplateFromCoordinates.Checked)
            {
                if (!TryReadCoordinateValue(_txtTargetX.Text, alignX, "X", out double targetX) ||
                    !TryReadCoordinateValue(_txtTargetY.Text, alignY, "Y", out double targetY) ||
                    !TryReadCoordinateValue(_txtTargetZ.Text, alignZ, "Z", out double targetZ))
                    return false;

                ok = copyNodes
                    ? AdvanceService.TryCopyNodesToCoordinates(
                        SelectedNodes,
                        alignX,
                        targetX,
                        alignY,
                        targetY,
                        alignZ,
                        targetZ,
                        out affectedCount,
                        out message)
                    : AdvanceService.TryAlignNodesToCoordinates(
                        SelectedNodes,
                        alignX,
                        targetX,
                        alignY,
                        targetY,
                        alignZ,
                        targetZ,
                        out affectedCount,
                        out message);
            }
            else
            {
                if (!int.TryParse(_txtTemplateNode.Text.Trim(), out int templateNodeId) || templateNodeId <= 0)
                {
                    Context.ShowWarning("Template node must be a valid positive integer.");
                    return false;
                }

                ok = copyNodes
                    ? AdvanceService.TryCopyNodesToTemplate(
                        SelectedNodes,
                        templateNodeId,
                        alignX,
                        alignY,
                        alignZ,
                        out affectedCount,
                        out message)
                    : AdvanceService.TryAlignNodesToTemplate(
                        SelectedNodes,
                        templateNodeId,
                        alignX,
                        alignY,
                        alignZ,
                        out affectedCount,
                        out message);
            }

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            string operation = copyNodes ? "Copy" : "Align";
            string countLabel = copyNodes ? "Created nodes" : "Updated nodes";
            Context.ShowSuccess($"{operation} completed. {countLabel}: {affectedCount}.\n{message}");
            Context.RefreshSelection();
            return true;
        }

        private bool TryReadCoordinateValue(string text, bool required, string axisName, out double value)
        {
            value = 0;
            if (!required)
                return true;

            if (TryParseDouble(text, out value))
                return true;

            Context.ShowWarning($"Please enter a valid {axisName} coordinate.");
            return false;
        }

        private void TemplateSourceChanged(object sender, EventArgs e)
        {
            UpdateTemplateSourceUi();
        }

        private void InputChanged(object sender, EventArgs e)
        {
            NotifyHintChanged();
        }

        private void CopyModeChanged(object sender, EventArgs e)
        {
            Context?.RequestActionUiUpdate();
            NotifyHintChanged();
        }

        private void UpdateTemplateSourceUi()
        {
            bool useManualCoordinates = _rdoTemplateFromCoordinates.Checked;
            _lblTemplateNode.Enabled = !useManualCoordinates;
            _txtTemplateNode.Enabled = !useManualCoordinates;

            _lblTargetCoords.Enabled = useManualCoordinates;
            _lblTargetCoords.Text = $"New coords ({Context?.CoordinateUnitLabel ?? "?"})";
            _lblTargetX.Enabled = useManualCoordinates;
            _txtTargetX.Enabled = useManualCoordinates;
            _lblTargetY.Enabled = useManualCoordinates;
            _txtTargetY.Enabled = useManualCoordinates;
            _lblTargetZ.Enabled = useManualCoordinates;
            _txtTargetZ.Enabled = useManualCoordinates;
            NotifyHintChanged();
        }
    }
}
