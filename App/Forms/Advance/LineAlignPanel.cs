using System;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class LineAlignPanel : AdvanceActionPanelBase
    {
        private readonly RadioButton _rdoRootFromSelection;
        private readonly RadioButton _rdoRootManual;
        private readonly ComboBox _cboRootNode;
        private readonly TextBox _txtRootNodeManual;
        private readonly Button _btnPickRoot;
        private readonly RadioButton _rdoAxisX;
        private readonly RadioButton _rdoAxisY;
        private readonly RadioButton _rdoAxisZ;
        private readonly TextBox _txtLineNodes;

        public override string ActionText => "Align node on line (3 points)";
        public override string ApplyText => "Create Node";

        public LineAlignPanel()
        {
            AddLabel(this, "_lblRootNode", "Root node", 2, 8);
            var rootMode = AddPanel(this, "_panelRootModeGroup", 90, 4, 232, 23);
            _rdoRootFromSelection = AddRadio(rootMode, "_rdoRootFromSelection", "From selected", 0, true, RootModeChanged);
            _rdoRootManual = AddRadio(rootMode, "_rdoRootManual", "Manual/Highlight", 104, false, RootModeChanged);

            _cboRootNode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(90, 28),
                Size = new System.Drawing.Size(120, 23)
            };
            _cboRootNode.SelectedIndexChanged += (s, e) => UpdateLineNodesPreview();
            Controls.Add(_cboRootNode);

            _txtRootNodeManual = AddTextBox(this, "_txtRootNodeManual", 216, 28, 50);
            _txtRootNodeManual.Enabled = false;
            _txtRootNodeManual.TextChanged += (s, e) => UpdateLineNodesPreview();
            _btnPickRoot = AddButton(this, "_btnPickRootHighlight", "Pick", 272, 27, BtnPickRoot_Click);
            _btnPickRoot.Size = new System.Drawing.Size(50, 25);
            _btnPickRoot.Enabled = false;

            AddLabel(this, "_lblKeepAxis", "Keep axis", 2, 58);
            var axisGroup = AddPanel(this, "_panelKeepAxisGroup", 90, 52, 140, 23);
            _rdoAxisX = AddRadio(axisGroup, "_rdoAxisX", "X", 0, false);
            _rdoAxisY = AddRadio(axisGroup, "_rdoAxisY", "Y", 50, true);
            _rdoAxisZ = AddRadio(axisGroup, "_rdoAxisZ", "Z", 100, false);

            AddLabel(this, "_lblLineNodesTitle", "Line nodes", 2, 89);
            _txtLineNodes = AddTextBox(this, "_txtLineNodes", 90, 85, 140, readOnly: true);
        }

        public override void OnSelectionChanged(int[] selectedNodes)
        {
            base.OnSelectionChanged(selectedNodes);

            object previousRoot = _cboRootNode.SelectedItem;
            _cboRootNode.Items.Clear();
            foreach (int nodeNo in SelectedNodes.OrderBy(x => x))
                _cboRootNode.Items.Add(nodeNo);

            if (previousRoot != null && _cboRootNode.Items.Contains(previousRoot))
                _cboRootNode.SelectedItem = previousRoot;
            else if (_cboRootNode.Items.Count > 0)
                _cboRootNode.SelectedIndex = 0;

            if (SelectedNodes.Length == 0)
                _txtRootNodeManual.Clear();
            else if (_rdoRootManual.Checked && string.IsNullOrWhiteSpace(_txtRootNodeManual.Text))
                _txtRootNodeManual.Text = SelectedNodes[0].ToString();

            UpdateLineNodesPreview();
        }

        public override AdvanceActionHint GetHint()
        {
            return SelectedNodes.Length == 3
                ? Info("Choose 1 root node, then create a node constrained to the line of the other 2 nodes.")
                : Error("Line mode requires exactly 3 selected nodes.");
        }

        public override bool Apply()
        {
            if (SelectedNodes.Length != 3)
            {
                Context.ShowWarning($"Line action requires exactly 3 selected nodes. Current: {SelectedNodes.Length}.");
                return false;
            }

            if (!TryReadRootNode(out int rootNode))
                return false;

            if (!SelectedNodes.Contains(rootNode))
            {
                Context.ShowWarning($"Root node {rootNode} must be one of the 3 selected nodes.");
                return false;
            }

            int[] lineNodes = SelectedNodes.Where(n => n != rootNode).Take(2).ToArray();
            if (lineNodes.Length != 2)
            {
                Context.ShowWarning("Could not identify the 2 line nodes.");
                return false;
            }

            AlignKeepAxis keepAxis = _rdoAxisX.Checked
                ? AlignKeepAxis.X
                : (_rdoAxisY.Checked ? AlignKeepAxis.Y : AlignKeepAxis.Z);

            bool ok = AdvanceService.TryCreateAlignedNodeFromThreeNodes(
                lineNodes[0],
                lineNodes[1],
                rootNode,
                keepAxis,
                out int newNodeId,
                out string message);

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Align completed. New node: {newNodeId}.\n{message}");
            Context.RefreshSelection();
            return true;
        }

        private void RootModeChanged(object sender, EventArgs e)
        {
            bool manual = _rdoRootManual.Checked;
            _cboRootNode.Enabled = !manual;
            _txtRootNodeManual.Enabled = manual;
            _btnPickRoot.Enabled = manual;

            if (manual && string.IsNullOrWhiteSpace(_txtRootNodeManual.Text) && _cboRootNode.SelectedItem != null)
                _txtRootNodeManual.Text = _cboRootNode.SelectedItem.ToString();

            UpdateLineNodesPreview();
        }

        private void BtnPickRoot_Click(object sender, EventArgs e)
        {
            if (TryPickSingleSelectedNode("root", out int nodeId))
                _txtRootNodeManual.Text = nodeId.ToString();
        }

        private bool TryReadRootNode(out int rootNode)
        {
            rootNode = 0;
            if (_rdoRootManual.Checked)
            {
                if (int.TryParse(_txtRootNodeManual.Text.Trim(), out rootNode) && rootNode > 0)
                    return true;

                Context.ShowWarning("Please enter a valid manual root node.");
                return false;
            }

            if (_cboRootNode.SelectedItem == null)
            {
                Context.ShowWarning("Please choose a root node.");
                return false;
            }

            rootNode = Convert.ToInt32(_cboRootNode.SelectedItem);
            return true;
        }

        private void UpdateLineNodesPreview()
        {
            if (SelectedNodes.Length == 0 || !TryReadRootNodePreview(out int rootNode))
            {
                _txtLineNodes.Text = "None";
                NotifyHintChanged();
                return;
            }

            int[] lineNodes = SelectedNodes.Where(n => n != rootNode).Take(2).ToArray();
            _txtLineNodes.Text = lineNodes.Length == 2
                ? $"{lineNodes[0]} -> {lineNodes[1]}"
                : "None";
            NotifyHintChanged();
        }

        private bool TryReadRootNodePreview(out int rootNode)
        {
            rootNode = 0;
            if (_rdoRootManual.Checked)
                return int.TryParse(_txtRootNodeManual.Text.Trim(), out rootNode) && rootNode > 0;

            if (_cboRootNode.SelectedItem == null)
                return false;

            rootNode = Convert.ToInt32(_cboRootNode.SelectedItem);
            return true;
        }
    }
}
