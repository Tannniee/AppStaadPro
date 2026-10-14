using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class BeamVectorNodePanel : AdvanceActionPanelBase
    {
        private int[] _capturedReferenceNodes = Array.Empty<int>();
        private readonly TextBox _txtReferenceNode;
        private readonly TextBox _txtCapturedReferences;
        private readonly Button _btnCaptureReferences;
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
            AddButton(this, "_btnPickBeamVectorReferenceNode", "Pick/Capt", 246, 3, BtnPickReferenceNode_Click);

            AddLabel(this, "_lblBeamVectorCapturedReferences", "Captured", 2, 39);
            _txtCapturedReferences = AddTextBox(this, "_txtBeamVectorCapturedReferences", 90, 35, 150, readOnly: true);
            _btnCaptureReferences = AddButton(this, "_btnCaptureBeamVectorReferences", "Capture", 246, 34, BtnCaptureReferences_Click);

            AddLabel(this, "_lblBeamVectorDirectionBeam", "Beam", 2, 70);
            _txtDirectionBeam = AddTextBox(this, "_txtBeamVectorDirectionBeam", 90, 66, 64);
            _txtDirectionBeam.TextChanged += InputChanged;
            AddButton(this, "_btnPickBeamVectorDirectionBeam", "Pick 1", 246, 65, BtnPickDirectionBeam_Click);

            _lblDistance = AddLabel(this, "_lblBeamVectorDistance", "Distance", 2, 101);
            _txtDistance = AddTextBox(this, "_txtBeamVectorDistance", 90, 97, 64);
            _txtDistance.TextChanged += InputChanged;

            AddLabel(
                this,
                "_lblBeamVectorSteps",
                "Without Capture, creates 1 node from Reference. With Capture, creates 1 new node per captured node.",
                2,
                126,
                320,
                30);
            UpdateCaptureUi();
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            _lblDistance.Text = $"Distance ({context.CoordinateUnitLabel})";
        }

        public override AdvanceActionHint GetHint()
        {
            if (_capturedReferenceNodes.Length > 0)
            {
                return TryReadBatchInputs(showErrors: false, out _, out _)
                    ? Info("Creates one new node per captured reference node along the selected beam direction. Use negative distance to reverse.")
                    : Error("Vector-node batch mode requires captured nodes, 1 direction beam, and a distance.");
            }

            return TryReadSingleInputs(showErrors: false, out _, out _, out _)
                ? Info("Creates a new node from the reference node along the selected beam direction. Use negative distance to reverse.")
                : Error("Vector-node mode requires 1 reference node, 1 direction beam, and a distance.");
        }

        public override bool Apply()
        {
            int directionBeamId;
            double distance;
            bool ok;
            string message;

            if (_capturedReferenceNodes.Length > 0)
            {
                if (!TryReadBatchInputs(showErrors: true, out directionBeamId, out distance))
                    return false;

                ok = AdvanceService.TryCreateNodesByBeamVector(
                    _capturedReferenceNodes,
                    directionBeamId,
                    distance,
                    out int createdCount,
                    out message);

                if (!ok)
                {
                    Context.ShowWarning(message);
                    return false;
                }

                Context.ShowSuccess($"Create completed. New nodes: {createdCount}.\n{message}");
                Context.RefreshSelection();
                return true;
            }

            if (!TryReadSingleInputs(showErrors: true, out int referenceNodeId, out directionBeamId, out distance))
                return false;

            ok = AdvanceService.TryCreateNodeByBeamVector(
                referenceNodeId,
                directionBeamId,
                distance,
                out int newNodeId,
                out message);

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
            if (!AdvanceService.TryGetSelectedNodes(out int[] picked, out string error))
            {
                Context.ShowWarning(error);
                return;
            }

            if (picked.Length == 1)
            {
                _capturedReferenceNodes = Array.Empty<int>();
                _txtReferenceNode.Text = picked[0].ToString();
                UpdateCaptureUi();
                return;
            }

            if (picked.Length > 1)
            {
                _capturedReferenceNodes = picked;
                UpdateCaptureUi();
                Context.ShowSuccess($"Captured {_capturedReferenceNodes.Length} selected node(s).");
                return;
            }

            Context.ShowWarning("Please highlight 1 reference node, or highlight multiple nodes to capture.");
        }

        private void BtnCaptureReferences_Click(object sender, EventArgs e)
        {
            if (_capturedReferenceNodes.Length > 0)
            {
                _capturedReferenceNodes = Array.Empty<int>();
                UpdateCaptureUi();
                return;
            }

            if (!AdvanceService.TryGetSelectedFrameNodeIds(out int[] referenceNodeIds, out string message))
            {
                Context.ShowWarning(message);
                return;
            }

            _capturedReferenceNodes = referenceNodeIds;
            UpdateCaptureUi();
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

        private void InputChanged(object sender, EventArgs e)
        {
            NotifyHintChanged();
        }

        private void UpdateCaptureUi()
        {
            _txtCapturedReferences.Text = FormatNodeListPreview(_capturedReferenceNodes, 5);
            _btnCaptureReferences.Text = _capturedReferenceNodes.Length > 0 ? "Clear" : "Capture";
            NotifyHintChanged();
        }

        private bool TryReadBatchInputs(bool showErrors, out int directionBeamId, out double distance)
        {
            directionBeamId = 0;
            distance = 0;

            bool validBeam = int.TryParse((_txtDirectionBeam?.Text ?? string.Empty).Trim(), out directionBeamId) && directionBeamId > 0;
            bool validDistance = TryParseDouble((_txtDistance?.Text ?? string.Empty).Trim(), out distance);

            if (!validBeam || !validDistance)
            {
                if (showErrors)
                    Context.ShowWarning("Vector-node batch mode requires a valid direction beam and distance.");
                return false;
            }

            return true;
        }

        private bool TryReadSingleInputs(bool showErrors, out int referenceNodeId, out int directionBeamId, out double distance)
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
