using System;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class FrameMovePanel : AdvanceActionPanelBase
    {
        private int[] _targetNodes = Array.Empty<int>();
        private readonly TextBox _txtTargets;
        private readonly TextBox _txtRoot;
        private readonly RadioButton _rdoToCoordinates;
        private readonly RadioButton _rdoToNode;
        private readonly Label _lblTargetCoords;
        private readonly Label _lblTargetX;
        private readonly TextBox _txtTargetX;
        private readonly Label _lblTargetY;
        private readonly TextBox _txtTargetY;
        private readonly Label _lblTargetZ;
        private readonly TextBox _txtTargetZ;
        private readonly Label _lblTargetNode;
        private readonly TextBox _txtTargetNode;
        private readonly Button _btnPickTargetNode;

        public override string ActionText => "Move captured frame";
        public override string ApplyText => "Move Frame";

        public FrameMovePanel()
        {
            Size = new System.Drawing.Size(328, 160);

            AddLabel(this, "_lblFrameMoveTargets", "Frame", 2, 8);
            _txtTargets = AddTextBox(this, "_txtFrameMoveTargets", 90, 4, 150, readOnly: true);
            AddButton(this, "_btnCaptureFrameMoveTargets", "Capture", 246, 3, BtnCaptureTargets_Click);

            AddLabel(this, "_lblFrameMoveRoot", "Root", 2, 41);
            _txtRoot = AddTextBox(this, "_txtFrameMoveRoot", 90, 37, 64);
            _txtRoot.TextChanged += InputChanged;
            AddButton(this, "_btnPickFrameMoveRoot", "Pick 1", 246, 36, BtnPickRoot_Click);

            AddLabel(this, "_lblFrameMoveTargetMode", "Target by", 2, 70);
            var modeGroup = AddPanel(this, "_panelFrameMoveTargetModeGroup", 90, 66, 206, 23);
            _rdoToCoordinates = AddRadio(modeGroup, "_rdoFrameMoveToCoordinates", "Coordinates", 0, true, TargetModeChanged);
            _rdoToNode = AddRadio(modeGroup, "_rdoFrameMoveToNode", "Target node", 103, false, TargetModeChanged);

            _lblTargetCoords = AddLabel(this, "_lblFrameMoveTargetCoords", "Target coords", 2, 100);
            _lblTargetX = AddLabel(this, "_lblFrameMoveTargetX", "X", 90, 100);
            _txtTargetX = AddTextBox(this, "_txtFrameMoveTargetX", 106, 96, 56);
            _lblTargetY = AddLabel(this, "_lblFrameMoveTargetY", "Y", 164, 100);
            _txtTargetY = AddTextBox(this, "_txtFrameMoveTargetY", 180, 96, 56);
            _lblTargetZ = AddLabel(this, "_lblFrameMoveTargetZ", "Z", 238, 100);
            _txtTargetZ = AddTextBox(this, "_txtFrameMoveTargetZ", 254, 96, 46);

            _lblTargetNode = AddLabel(this, "_lblFrameMoveTargetNode", "Target node", 2, 129);
            _txtTargetNode = AddTextBox(this, "_txtFrameMoveTargetNode", 90, 125, 64);
            _btnPickTargetNode = AddButton(this, "_btnPickFrameMoveTargetNode", "Pick 1", 246, 124, BtnPickTargetNode_Click);

            _txtTargetX.TextChanged += InputChanged;
            _txtTargetY.TextChanged += InputChanged;
            _txtTargetZ.TextChanged += InputChanged;
            _txtTargetNode.TextChanged += InputChanged;
            UpdateTargetModeUi();
            UpdateUi();
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            UpdateTargetModeUi();
        }

        public override AdvanceActionHint GetHint()
        {
            if (_targetNodes.Length == 0)
                return Error("Capture frame first, then pick 1 root node and choose a move target.");

            if (!TryReadInputs(showErrors: false, out _, out bool useTargetNode, out _, out _, out _, out _))
                return Error("Move mode requires 1 root node plus coordinates or 1 target node.");

            return Info(useTargetNode
                ? "Captured frame nodes are translated so the root node lands on the picked target node."
                : "Captured frame nodes are translated so the root node lands on the entered coordinates.");
        }

        public override bool Apply()
        {
            if (_targetNodes.Length == 0)
            {
                Context.ShowWarning("Please capture frame nodes first.");
                return false;
            }

            if (!TryReadInputs(showErrors: true, out int rootNodeId, out bool useTargetNode, out int targetNodeId, out double targetX, out double targetY, out double targetZ))
                return false;

            bool ok;
            int updatedCount;
            string message;
            if (useTargetNode)
            {
                ok = AdvanceService.TryMoveNodesToNode(
                    _targetNodes,
                    rootNodeId,
                    targetNodeId,
                    out updatedCount,
                    out message);
            }
            else
            {
                ok = AdvanceService.TryMoveNodesToCoordinates(
                    _targetNodes,
                    rootNodeId,
                    targetX,
                    targetY,
                    targetZ,
                    out updatedCount,
                    out message);
            }

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Move completed. Updated nodes: {updatedCount}.\n{message}");
            Context.RefreshSelection();
            UpdateUi();
            return true;
        }

        private void BtnCaptureTargets_Click(object sender, EventArgs e)
        {
            if (!AdvanceService.TryGetSelectedFrameNodeIds(out int[] targetNodeIds, out string message))
            {
                Context.ShowWarning(message);
                return;
            }

            _targetNodes = targetNodeIds;
            if (_targetNodes.Length > 0)
            {
                string currentRoot = (_txtRoot?.Text ?? string.Empty).Trim();
                if (!int.TryParse(currentRoot, out int rootNodeId) || !_targetNodes.Contains(rootNodeId))
                    _txtRoot.Text = _targetNodes[0].ToString();
            }

            UpdateUi();
            Context.ShowSuccess(message);
        }

        private void BtnPickRoot_Click(object sender, EventArgs e)
        {
            if (TryPickSingleSelectedNode("root", out int nodeId))
                _txtRoot.Text = nodeId.ToString();
        }

        private void BtnPickTargetNode_Click(object sender, EventArgs e)
        {
            if (TryPickSingleSelectedNode("target", out int nodeId))
                _txtTargetNode.Text = nodeId.ToString();
        }

        private void TargetModeChanged(object sender, EventArgs e)
        {
            UpdateTargetModeUi();
        }

        private void InputChanged(object sender, EventArgs e)
        {
            NotifyHintChanged();
        }

        private void UpdateUi()
        {
            _txtTargets.Text = FormatNodeListPreview(_targetNodes, 5);
            UpdateTargetModeUi();
        }

        private void UpdateTargetModeUi()
        {
            bool useTargetNode = _rdoToNode.Checked;
            bool useCoordinates = !useTargetNode;

            _lblTargetCoords.Enabled = useCoordinates;
            _lblTargetCoords.Text = $"Target coords ({Context?.CoordinateUnitLabel ?? "?"})";
            _lblTargetX.Enabled = useCoordinates;
            _txtTargetX.Enabled = useCoordinates;
            _lblTargetY.Enabled = useCoordinates;
            _txtTargetY.Enabled = useCoordinates;
            _lblTargetZ.Enabled = useCoordinates;
            _txtTargetZ.Enabled = useCoordinates;

            _lblTargetNode.Enabled = useTargetNode;
            _txtTargetNode.Enabled = useTargetNode;
            _btnPickTargetNode.Enabled = useTargetNode;
            NotifyHintChanged();
        }

        private bool TryReadInputs(
            bool showErrors,
            out int rootNodeId,
            out bool useTargetNode,
            out int targetNodeId,
            out double targetX,
            out double targetY,
            out double targetZ)
        {
            rootNodeId = 0;
            targetNodeId = 0;
            targetX = 0;
            targetY = 0;
            targetZ = 0;
            useTargetNode = _rdoToNode.Checked;

            bool validRoot = int.TryParse((_txtRoot?.Text ?? string.Empty).Trim(), out rootNodeId) && rootNodeId > 0;
            if (!validRoot)
            {
                if (showErrors)
                    Context.ShowWarning("Move mode requires a valid root node.");
                return false;
            }

            if (useTargetNode)
            {
                bool validTargetNode = int.TryParse((_txtTargetNode?.Text ?? string.Empty).Trim(), out targetNodeId) && targetNodeId > 0;
                if (!validTargetNode)
                {
                    if (showErrors)
                        Context.ShowWarning("Move mode requires a valid target node.");
                    return false;
                }

                return true;
            }

            if (!TryParseDouble((_txtTargetX?.Text ?? string.Empty).Trim(), out targetX) ||
                !TryParseDouble((_txtTargetY?.Text ?? string.Empty).Trim(), out targetY) ||
                !TryParseDouble((_txtTargetZ?.Text ?? string.Empty).Trim(), out targetZ))
            {
                if (showErrors)
                    Context.ShowWarning("Move mode requires valid X/Y/Z target coordinates.");
                return false;
            }

            return true;
        }
    }
}
