using System;
using System.Windows.Forms;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Advance;

namespace StaadSteelApp
{
    internal sealed class ElevationVectorAlignPanel : AdvanceActionPanelBase
    {
        private int[] _targetNodes = Array.Empty<int>();
        private int[] _vectorNodes = Array.Empty<int>();
        private readonly TextBox _txtTargets;
        private readonly TextBox _txtVector;

        public override string ActionText => "Align captured nodes to vector elevation (Y only)";
        public override string ApplyText => "Preview / Apply";
        public override bool UsesNodeSelection => false;

        public ElevationVectorAlignPanel()
        {
            AddLabel(this, "_lblElevationTargets", "Target nodes", 2, 8);
            _txtTargets = AddTextBox(this, "_txtElevationTargets", 90, 4, 150, readOnly: true);
            AddButton(this, "_btnCaptureElevationTargets", "Capture", 246, 3, BtnCaptureTargets_Click);

            AddLabel(this, "_lblElevationVector", "Vector nodes", 2, 41);
            _txtVector = AddTextBox(this, "_txtElevationVector", 90, 37, 150, readOnly: true);
            AddButton(this, "_btnCaptureElevationVector", "Capture 2", 246, 36, BtnCaptureVector_Click);

            AddLabel(this, "_lblElevationMode", "Mode", 2, 74);
            AddLabel(this, "_lblElevationModeValue", "Y only — X and Z stay unchanged", 90, 74, 232, 18);

            AddLabel(
                this,
                "_lblElevationSteps",
                "1. Highlight target nodes, Capture.\r\n2. Highlight 2 vector nodes, Capture 2.\r\n3. Preview / Apply, then verify Old Y and New Y.",
                2,
                99,
                320,
                56);

            UpdateUi();
        }

        public override AdvanceActionHint GetHint()
        {
            if (_targetNodes.Length == 0)
                return Error("Capture at least one target node first.");

            if (_vectorNodes.Length != 2)
                return Error("Capture exactly two vector nodes after capturing targets.");

            return Info("Ready to preview. Only Y may change; X/Z remain unchanged and the vector is treated as an infinite line.");
        }

        public override bool Apply()
        {
            if (_targetNodes.Length == 0 || _vectorNodes.Length != 2)
            {
                Context.ShowWarning("Capture target nodes and exactly two vector nodes before previewing.");
                return false;
            }

            if (!AdvanceElevationAlignService.TryBuildPreview(
                _targetNodes,
                _vectorNodes,
                out AdvanceElevationAlignPreviewRow[] rows,
                out string message))
            {
                Context.ShowWarning(message);
                return false;
            }

            using (var preview = new ElevationAlignPreviewForm(
                _targetNodes,
                _vectorNodes,
                rows,
                message,
                Context.CoordinateUnitLabel))
            {
                if (preview.ShowDialog(FindForm()) != DialogResult.OK)
                    return false;

                Context.ShowSuccess(preview.ResultMessage);
            }

            return true;
        }

        private void BtnCaptureTargets_Click(object sender, EventArgs e)
        {
            if (!AdvanceService.TryGetSelectedNodes(out int[] selected, out string error))
            {
                Context.ShowWarning(error);
                return;
            }

            if (selected.Length == 0)
            {
                Context.ShowWarning("Highlight at least one target node in STAAD, then Capture again.");
                return;
            }

            _targetNodes = selected;
            UpdateUi();
        }

        private void BtnCaptureVector_Click(object sender, EventArgs e)
        {
            if (!TryPickTwoSelectedNodes("elevation vector", out int vectorA, out int vectorB))
                return;

            _vectorNodes = new[] { vectorA, vectorB };
            UpdateUi();
        }

        private void UpdateUi()
        {
            _txtTargets.Text = _targetNodes.Length == 0
                ? "None"
                : $"{_targetNodes.Length}: {FormatNodeListPreview(_targetNodes, 5)}";
            _txtVector.Text = _vectorNodes.Length == 2
                ? $"{_vectorNodes[0]} -> {_vectorNodes[1]}"
                : "None";
            NotifyHintChanged();
        }
    }
}
