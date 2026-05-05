using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class BeamSegmentPanel : AdvanceActionPanelBase
    {
        private int[] _targetNodes = Array.Empty<int>();
        private int[] _segmentNodes = Array.Empty<int>();
        private readonly TextBox _txtTargets;
        private readonly TextBox _txtSegmentNodeA;
        private readonly TextBox _txtSegmentNodeB;

        public override string ActionText => "Align captured nodes to beam segment";
        public override string ApplyText => "Align To Beam";

        public BeamSegmentPanel()
        {
            AddLabel(this, "_lblBeamTargets", "Targets", 2, 8);
            _txtTargets = AddTextBox(this, "_txtBeamTargets", 90, 4, 150, readOnly: true);
            AddButton(this, "_btnCaptureBeamTargets", "Capture", 246, 3, BtnCaptureTargets_Click);

            AddLabel(this, "_lblBeamSegment", "Segment", 2, 41);
            _txtSegmentNodeA = AddTextBox(this, "_txtBeamSegmentNodeA", 90, 37, 64);
            AddLabel(this, "_lblBeamSegmentArrow", "->", 162, 41);
            _txtSegmentNodeB = AddTextBox(this, "_txtBeamSegmentNodeB", 184, 37, 56);
            _txtSegmentNodeA.TextChanged += InputChanged;
            _txtSegmentNodeB.TextChanged += InputChanged;
            AddButton(this, "_btnPickBeamSegment", "Pick 2", 246, 36, BtnPickSegment_Click);

            AddLabel(
                this,
                "_lblBeamSegmentSteps",
                "1. Capture current selected target nodes.\r\n2. Highlight 2 beam nodes in STAAD, then click Pick 2.\r\n3. Apply to project targets onto that segment.",
                2,
                71,
                320,
                45);
            UpdateUi();
        }

        public override AdvanceActionHint GetHint()
        {
            if (_targetNodes.Length == 0)
                return Error("Capture target nodes first, then pick 2 nodes that define the beam segment.");

            if (!TryReadBeamSegmentNodes(showErrors: false, out _, out _))
                return Error("Beam segment mode requires 2 different segment nodes.");

            return Info("Captured target nodes are projected to the nearest point on the selected beam segment only.");
        }

        public override bool Apply()
        {
            if (_targetNodes.Length == 0)
            {
                Context.ShowWarning("Please capture target nodes first.");
                return false;
            }

            if (!TryReadBeamSegmentNodes(showErrors: true, out int segmentNodeA, out int segmentNodeB))
                return false;

            bool ok = AdvanceService.TryAlignNodesToSegment(
                _targetNodes,
                segmentNodeA,
                segmentNodeB,
                out int updatedCount,
                out string message);

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            _segmentNodes = new[] { segmentNodeA, segmentNodeB };
            Context.ShowSuccess($"Align completed. Updated nodes: {updatedCount}.\n{message}");
            Context.RefreshSelection();
            UpdateUi();
            return true;
        }

        private void BtnCaptureTargets_Click(object sender, EventArgs e)
        {
            int[] selected = Context.CurrentSelection;
            if (selected.Length == 0)
            {
                Context.ShowWarning("Please select at least 1 target node in STAAD first.");
                return;
            }

            _targetNodes = selected;
            UpdateUi();
        }

        private void BtnPickSegment_Click(object sender, EventArgs e)
        {
            if (!TryPickTwoSelectedNodes("beam segment", out int nodeA, out int nodeB))
                return;

            _segmentNodes = new[] { nodeA, nodeB };
            SetBeamSegmentInputs(nodeA, nodeB);
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

        private void SetBeamSegmentInputs(int nodeA, int nodeB)
        {
            _txtSegmentNodeA.Text = nodeA.ToString();
            _txtSegmentNodeB.Text = nodeB.ToString();
            NotifyHintChanged();
        }

        private bool TryReadBeamSegmentNodes(bool showErrors, out int nodeA, out int nodeB)
        {
            string rawNodeA = (_txtSegmentNodeA?.Text ?? string.Empty).Trim();
            string rawNodeB = (_txtSegmentNodeB?.Text ?? string.Empty).Trim();
            bool validA = int.TryParse(rawNodeA, out nodeA) && nodeA > 0;
            bool validB = int.TryParse(rawNodeB, out nodeB) && nodeB > 0;

            if (!validA || !validB)
            {
                if (showErrors)
                    Context.ShowWarning("Beam segment nodes must be valid positive integers.");
                return false;
            }

            if (nodeA == nodeB)
            {
                if (showErrors)
                    Context.ShowWarning("Beam segment requires 2 different nodes.");
                return false;
            }

            return true;
        }
    }
}
