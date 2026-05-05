using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class BeamVectorNodePanel : AdvanceActionPanelBase
    {
        private readonly TextBox _txtReferenceNode;
        private readonly TextBox _txtDirectionBeam;
        private readonly Label _lblDistance;
        private readonly TextBox _txtDistance;

        public override string ActionText => "Create node by beam vector";
        public override string ApplyText => "Create Node";

        public BeamVectorNodePanel()
        {
            AddLabel(this, "_lblBeamVectorReferenceNode", "Reference", 2, 8);
            _txtReferenceNode = AddTextBox(this, "_txtBeamVectorReferenceNode", 90, 4, 64);
            _txtReferenceNode.TextChanged += InputChanged;
            AddButton(this, "_btnPickBeamVectorReferenceNode", "Pick 1", 246, 3, BtnPickReferenceNode_Click);

            AddLabel(this, "_lblBeamVectorDirectionBeam", "Beam", 2, 41);
            _txtDirectionBeam = AddTextBox(this, "_txtBeamVectorDirectionBeam", 90, 37, 64);
            _txtDirectionBeam.TextChanged += InputChanged;
            AddButton(this, "_btnPickBeamVectorDirectionBeam", "Pick 1", 246, 36, BtnPickDirectionBeam_Click);

            _lblDistance = AddLabel(this, "_lblBeamVectorDistance", "Distance", 2, 74);
            _txtDistance = AddTextBox(this, "_txtBeamVectorDistance", 90, 70, 64);
            _txtDistance.TextChanged += InputChanged;

            AddLabel(
                this,
                "_lblBeamVectorSteps",
                "Pick 1 reference node and 1 beam. Distance follows beam start -> end; use negative to reverse.",
                2,
                98,
                320,
                44);
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            _lblDistance.Text = $"Distance ({context.CoordinateUnitLabel})";
        }

        public override AdvanceActionHint GetHint()
        {
            return TryReadInputs(showErrors: false, out _, out _, out _)
                ? Info("Creates a new node from the reference node by moving along the selected beam direction. Use negative distance to reverse.")
                : Error("Vector-node mode requires 1 reference node, 1 direction beam, and a distance.");
        }

        public override bool Apply()
        {
            if (!TryReadInputs(showErrors: true, out int referenceNodeId, out int directionBeamId, out double distance))
                return false;

            bool ok = AdvanceService.TryCreateNodeByBeamVector(
                referenceNodeId,
                directionBeamId,
                distance,
                out int newNodeId,
                out string message);

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Create completed. New node: {newNodeId}.\n{message}");
            Context.RefreshSelection();
            return true;
        }

        private void BtnPickReferenceNode_Click(object sender, EventArgs e)
        {
            if (TryPickSingleSelectedNode("reference", out int nodeId))
                _txtReferenceNode.Text = nodeId.ToString();
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

        private bool TryReadInputs(bool showErrors, out int referenceNodeId, out int directionBeamId, out double distance)
        {
            referenceNodeId = 0;
            directionBeamId = 0;
            distance = 0;

            bool validReference = int.TryParse((_txtReferenceNode?.Text ?? string.Empty).Trim(), out referenceNodeId) && referenceNodeId > 0;
            bool validBeam = int.TryParse((_txtDirectionBeam?.Text ?? string.Empty).Trim(), out directionBeamId) && directionBeamId > 0;
            bool validDistance = TryParseDouble((_txtDistance?.Text ?? string.Empty).Trim(), out distance);

            if (!validReference || !validBeam || !validDistance)
            {
                if (showErrors)
                    Context.ShowWarning("Vector-node mode requires a valid reference node, direction beam, and distance.");
                return false;
            }

            return true;
        }
    }
}
