using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class FrameRotatePanel : AdvanceActionPanelBase
    {
        private int[] _targetNodes = Array.Empty<int>();
        private readonly TextBox _txtTargets;
        private readonly TextBox _txtDirectionBeam;
        private readonly TextBox _txtPivot;
        private readonly RadioButton _rdoAxisX;
        private readonly RadioButton _rdoAxisY;
        private readonly RadioButton _rdoAxisZ;

        public override string ActionText => "Rotate captured frame to axis";
        public override string ApplyText => "Rotate Frame";

        public FrameRotatePanel()
        {
            AddLabel(this, "_lblFrameRotateTargets", "Frame", 2, 8);
            _txtTargets = AddTextBox(this, "_txtFrameRotateTargets", 90, 4, 150, readOnly: true);
            AddButton(this, "_btnCaptureFrameRotateTargets", "Capture", 246, 3, BtnCaptureTargets_Click);

            AddLabel(this, "_lblFrameRotateDirectionBeam", "Direction beam", 2, 41);
            _txtDirectionBeam = AddTextBox(this, "_txtFrameRotateDirectionBeam", 90, 37, 64);
            _txtDirectionBeam.TextChanged += InputChanged;
            AddButton(this, "_btnPickFrameRotateDirectionBeam", "Pick 1", 246, 36, BtnPickDirectionBeam_Click);

            AddLabel(this, "_lblFrameRotatePivot", "Pivot", 2, 70);
            _txtPivot = AddTextBox(this, "_txtFrameRotatePivot", 90, 66, 64);
            _txtPivot.TextChanged += InputChanged;
            AddButton(this, "_btnPickFrameRotatePivot", "Pick 1", 246, 65, BtnPickPivot_Click);

            AddLabel(this, "_lblFrameRotateAxis", "Target axis", 2, 100);
            var axisGroup = AddPanel(this, "_panelFrameRotateAxisGroup", 90, 94, 140, 23);
            _rdoAxisX = AddRadio(axisGroup, "_rdoFrameRotateAxisX", "X", 0, false, InputChanged);
            _rdoAxisY = AddRadio(axisGroup, "_rdoFrameRotateAxisY", "Y", 50, true, InputChanged);
            _rdoAxisZ = AddRadio(axisGroup, "_rdoFrameRotateAxisZ", "Z", 100, false, InputChanged);

            AddLabel(
                this,
                "_lblFrameRotateSteps",
                "1. Select frame beams in STAAD then Capture.\r\n2. Pick direction beam, then pick pivot point, then Rotate.",
                2,
                124,
                320,
                34);
            UpdateUi();
        }

        public override AdvanceActionHint GetHint()
        {
            if (_targetNodes.Length == 0)
                return Error("Capture frame first, then pick 1 direction beam and 1 pivot node.");

            if (!TryReadInputs(showErrors: false, out _, out _))
                return Error("Rotate mode requires 1 direction beam and 1 pivot node.");

            return Info("Captured frame nodes are rotated around the pivot node so the picked beam direction aligns to global X, Y, or Z.");
        }

        public override bool Apply()
        {
            if (_targetNodes.Length == 0)
            {
                Context.ShowWarning("Please capture frame nodes first.");
                return false;
            }

            if (!TryReadInputs(showErrors: true, out int directionBeamId, out int pivotNodeId))
                return false;

            AlignKeepAxis targetAxis = _rdoAxisX.Checked
                ? AlignKeepAxis.X
                : (_rdoAxisY.Checked ? AlignKeepAxis.Y : AlignKeepAxis.Z);

            bool ok = AdvanceService.TryRotateNodesToAxis(
                _targetNodes,
                pivotNodeId,
                directionBeamId,
                targetAxis,
                out int updatedCount,
                out string message);

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Rotate completed. Updated nodes: {updatedCount}.\n{message}");
            Context.RefreshSelection();
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
            UpdateUi();
            Context.ShowSuccess(message);
        }

        private void BtnPickDirectionBeam_Click(object sender, EventArgs e)
        {
            if (!AdvanceService.TryGetSingleSelectedBeamId(out int beamId, out string message))
            {
                Context.ShowWarning(message);
                return;
            }

            _txtDirectionBeam.Text = beamId.ToString();
        }

        private void BtnPickPivot_Click(object sender, EventArgs e)
        {
            if (TryPickSingleSelectedNode("pivot", out int nodeId))
                _txtPivot.Text = nodeId.ToString();
        }

        private void InputChanged(object sender, EventArgs e)
        {
            NotifyHintChanged();
        }

        private void UpdateUi()
        {
            _txtTargets.Text = FormatNodeListPreview(_targetNodes, 5);
            NotifyHintChanged();
        }

        private bool TryReadInputs(bool showErrors, out int directionBeamId, out int pivotNodeId)
        {
            directionBeamId = 0;
            pivotNodeId = 0;

            bool validBeam = int.TryParse((_txtDirectionBeam?.Text ?? string.Empty).Trim(), out directionBeamId) && directionBeamId > 0;
            bool validPivot = int.TryParse((_txtPivot?.Text ?? string.Empty).Trim(), out pivotNodeId) && pivotNodeId > 0;

            if (!validBeam || !validPivot)
            {
                if (showErrors)
                    Context.ShowWarning("Rotate mode requires valid direction beam and pivot node.");
                return false;
            }

            return true;
        }
    }
}
