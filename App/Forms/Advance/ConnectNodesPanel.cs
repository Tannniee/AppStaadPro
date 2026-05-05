using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class ConnectNodesPanel : AdvanceActionPanelBase
    {
        private readonly RadioButton _rdoConnectSingleChain;
        private readonly RadioButton _rdoConnectSplitByGap;
        private readonly RadioButton _rdoConnectAxisX;
        private readonly RadioButton _rdoConnectAxisY;
        private readonly RadioButton _rdoConnectAxisZ;
        private readonly Label _lblGapAxis;
        private readonly Panel _panelGapAxisGroup;
        private readonly RadioButton _rdoGapAxisX;
        private readonly RadioButton _rdoGapAxisY;
        private readonly RadioButton _rdoGapAxisZ;
        private readonly Label _lblGapLimit;
        private readonly TextBox _txtGapLimit;
        private readonly Label _lblGapLimitUnit;

        public override string ActionText => "Connect selected nodes with beams";
        public override string ApplyText => "Create Beams";

        public ConnectNodesPanel()
        {
            AddLabel(this, "_lblConnectMode", "Mode", 2, 8);
            var modeGroup = AddPanel(this, "_panelConnectModeGroup", 90, 2, 206, 23);
            _rdoConnectSingleChain = AddRadio(modeGroup, "_rdoConnectSingleChain", "Single Chain", 0, true, ConnectModeChanged);
            _rdoConnectSplitByGap = AddRadio(modeGroup, "_rdoConnectSplitByGap", "Split by Gap", 105, false, ConnectModeChanged);

            AddLabel(this, "_lblConnectAxes", "Sort axis", 2, 37);
            var sortGroup = AddPanel(this, "_panelConnectAxisGroup", 90, 31, 140, 23);
            _rdoConnectAxisX = AddRadio(sortGroup, "_rdoConnectAxisX", "X", 0, true);
            _rdoConnectAxisY = AddRadio(sortGroup, "_rdoConnectAxisY", "Y", 50, false);
            _rdoConnectAxisZ = AddRadio(sortGroup, "_rdoConnectAxisZ", "Z", 100, false);

            _lblGapAxis = AddLabel(this, "_lblGapAxis", "Gap axis", 2, 66);
            _panelGapAxisGroup = AddPanel(this, "_panelGapAxisGroup", 90, 60, 140, 23);
            _rdoGapAxisX = AddRadio(_panelGapAxisGroup, "_rdoGapAxisX", "X", 0, true);
            _rdoGapAxisY = AddRadio(_panelGapAxisGroup, "_rdoGapAxisY", "Y", 50, false);
            _rdoGapAxisZ = AddRadio(_panelGapAxisGroup, "_rdoGapAxisZ", "Z", 100, false);

            _lblGapLimit = AddLabel(this, "_lblGapLimit", "Gap limit", 2, 95);
            _txtGapLimit = AddTextBox(this, "_txtGapLimit", 90, 91, 80);
            _txtGapLimit.Text = "8";
            _txtGapLimit.TextChanged += (s, e) => NotifyHintChanged();
            _lblGapLimitUnit = AddLabel(this, "_lblGapLimitUnit", "model units", 176, 95);

            UpdateConnectModeUi();
        }

        public override AdvanceActionHint GetHint()
        {
            if (SelectedNodes.Length < 2)
                return Error("Connect mode requires at least 2 selected nodes.");

            return Info(_rdoConnectSplitByGap.Checked
                ? "Split by Gap groups selected nodes first, then sorts each group by the chosen axis."
                : "Selected nodes are sorted by X, Y, or Z only, then connected in that order.");
        }

        public override bool Apply()
        {
            if (SelectedNodes.Length < 2)
            {
                Context.ShowWarning("Please select at least 2 nodes in STAAD.");
                return false;
            }

            AlignKeepAxis sortAxis = _rdoConnectAxisY.Checked
                ? AlignKeepAxis.Y
                : (_rdoConnectAxisZ.Checked ? AlignKeepAxis.Z : AlignKeepAxis.X);

            bool ok;
            int createdCount;
            string message;
            if (_rdoConnectSplitByGap.Checked)
            {
                if (!TryParsePositiveDouble(_txtGapLimit.Text, out double gapLimit))
                {
                    Context.ShowWarning("Gap limit must be a valid positive number.");
                    return false;
                }

                AlignKeepAxis gapAxis = _rdoGapAxisY.Checked
                    ? AlignKeepAxis.Y
                    : (_rdoGapAxisZ.Checked ? AlignKeepAxis.Z : AlignKeepAxis.X);

                ok = AdvanceService.TryConnectSelectedNodesByGap(
                    SelectedNodes,
                    gapAxis,
                    gapLimit,
                    sortAxis,
                    out createdCount,
                    out message);
            }
            else
            {
                ok = AdvanceService.TryConnectSelectedNodes(
                    SelectedNodes,
                    sortAxis,
                    out createdCount,
                    out message);
            }

            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess($"Connect completed. New beams: {createdCount}.\n{message}");
            Context.RefreshSelection();
            return true;
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            _lblGapLimitUnit.Text = context.CoordinateUnitLabel;
        }

        private void ConnectModeChanged(object sender, EventArgs e)
        {
            UpdateConnectModeUi();
        }

        private void UpdateConnectModeUi()
        {
            bool splitByGap = _rdoConnectSplitByGap.Checked;
            _lblGapAxis.Visible = splitByGap;
            _panelGapAxisGroup.Visible = splitByGap;
            _lblGapLimit.Visible = splitByGap;
            _txtGapLimit.Visible = splitByGap;
            _lblGapLimitUnit.Visible = splitByGap;
            NotifyHintChanged();
        }
    }
}
