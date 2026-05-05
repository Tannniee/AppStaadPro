using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class BeamPlanePanel : AdvanceActionPanelBase
    {
        private readonly TextBox _txtDirectionBeam;
        private readonly CheckBox _chkXY;
        private readonly CheckBox _chkXZ;
        private readonly CheckBox _chkYZ;

        public override string ActionText => "Align selected nodes to beam plane";
        public override string ApplyText => "Align To Plane";

        public BeamPlanePanel()
        {
            AddLabel(this, "_lblBeamPlaneDirectionBeam", "Beam", 2, 8);
            _txtDirectionBeam = AddTextBox(this, "_txtBeamPlaneDirectionBeam", 90, 4, 64);
            _txtDirectionBeam.TextChanged += InputChanged;
            AddButton(this, "_btnPickBeamPlaneDirectionBeam", "Pick 1", 246, 3, BtnPickDirectionBeam_Click);

            AddLabel(this, "_lblBeamPlanePlanes", "Plane", 2, 43);
            _chkXY = AddCheck(this, "_chkBeamPlaneXY", "XY", 90, 41, true, InputChanged);
            _chkXZ = AddCheck(this, "_chkBeamPlaneXZ", "XZ", 142, 41, false, InputChanged);
            _chkYZ = AddCheck(this, "_chkBeamPlaneYZ", "YZ", 194, 41, false, InputChanged);

            AddLabel(
                this,
                "_lblBeamPlaneSteps",
                "Selected nodes are moved to the checked global plane(s) through the beam.\r\nXY sets Z, XZ sets Y, YZ sets X.",
                2,
                72,
                320,
                44);
        }

        public override AdvanceActionHint GetHint()
        {
            if (SelectedNodes.Length == 0)
                return Error("Beam-plane mode requires at least 1 selected node.");

            if (!TryReadInputs(showErrors: false, out _, out _, out _, out _))
                return Error("Beam-plane mode requires 1 direction beam and at least 1 plane checkbox.");

            return Info("Moves selected nodes onto the checked global plane(s) passing through the selected beam.");
        }

        public override bool Apply()
        {
            if (SelectedNodes.Length == 0)
            {
                Context.ShowWarning("Please select at least one node in STAAD.");
                return false;
            }

            if (!TryReadInputs(showErrors: true, out int directionBeamId, out bool alignToXY, out bool alignToXZ, out bool alignToYZ))
                return false;

            bool ok = AdvanceService.TryAlignNodesToBeamPlanes(
                SelectedNodes,
                directionBeamId,
                alignToXY,
                alignToXZ,
                alignToYZ,
                out int updatedCount,
                out string message);

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Align completed. Updated nodes: {updatedCount}.\n{message}");
            Context.RefreshSelection();
            return true;
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

        private void InputChanged(object sender, EventArgs e)
        {
            NotifyHintChanged();
        }

        private bool TryReadInputs(bool showErrors, out int directionBeamId, out bool alignToXY, out bool alignToXZ, out bool alignToYZ)
        {
            directionBeamId = 0;
            alignToXY = _chkXY.Checked;
            alignToXZ = _chkXZ.Checked;
            alignToYZ = _chkYZ.Checked;

            bool validBeam = int.TryParse((_txtDirectionBeam?.Text ?? string.Empty).Trim(), out directionBeamId) && directionBeamId > 0;
            bool hasPlane = alignToXY || alignToXZ || alignToYZ;

            if (!validBeam || !hasPlane)
            {
                if (showErrors)
                    Context.ShowWarning("Beam-plane mode requires a valid direction beam and at least one plane checkbox.");
                return false;
            }

            return true;
        }
    }
}
