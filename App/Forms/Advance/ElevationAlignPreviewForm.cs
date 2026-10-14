using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core.Advance;

namespace StaadSteelApp
{
    internal sealed class ElevationAlignPreviewForm : Form
    {
        private readonly int[] _targetNodeIds;
        private readonly int[] _vectorNodeIds;
        private readonly AdvanceElevationAlignPreviewRow[] _previewRows;
        private readonly DataGridView _grid;
        private readonly Button _btnApply;

        internal ElevationAlignPreviewForm(
            int[] targetNodeIds,
            int[] vectorNodeIds,
            AdvanceElevationAlignPreviewRow[] previewRows,
            string summary,
            string coordinateUnitLabel)
        {
            _targetNodeIds = (targetNodeIds ?? Array.Empty<int>()).ToArray();
            _vectorNodeIds = (vectorNodeIds ?? Array.Empty<int>()).ToArray();
            _previewRows = (previewRows ?? Array.Empty<AdvanceElevationAlignPreviewRow>()).ToArray();

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(10, 8, 10, 4),
                Text =
                    $"Vector: {FormatVector(_vectorNodeIds)}    Coordinate unit: {coordinateUnitLabel ?? "?"}\r\n" +
                    (summary ?? string.Empty)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                DataSource = new BindingList<AdvanceElevationAlignPreviewRow>(_previewRows.ToList())
            };

            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.NodeId), "Node", 58, null, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.X), "X (kept)", 96, "0.######", DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.OldY), "Old Y", 96, "0.######", DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.NewY), "New Y", 96, "0.######", DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.DeltaY), "Delta Y", 96, "0.######", DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.Z), "Z (kept)", 96, "0.######", DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(AdvanceElevationAlignPreviewRow.Status), "Status", 260, null, DataGridViewContentAlignment.MiddleLeft);
            _grid.RowPrePaint += Grid_RowPrePaint;

            _btnApply = new Button
            {
                Text = "Apply Y Changes",
                Width = 112,
                Height = 30,
                Enabled = _previewRows.Any(row => row.RequiresUpdate)
            };
            _btnApply.Click += BtnApply_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 90,
                Height = 30
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                BackColor = SystemColors.Control
            };
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(_btnApply);

            Text = "Y-only Elevation Alignment Preview";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            Width = 930;
            Height = 520;
            TopMost = true;
            AcceptButton = _btnApply;
            CancelButton = btnCancel;
            Controls.Add(_grid);
            Controls.Add(header);
            Controls.Add(buttons);
        }

        internal string ResultMessage { get; private set; } = string.Empty;

        private void AddTextColumn(
            string propertyName,
            string headerText,
            int width,
            string format,
            DataGridViewContentAlignment alignment)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = alignment }
            };
            if (!string.IsNullOrWhiteSpace(format))
                column.DefaultCellStyle.Format = format;
            _grid.Columns.Add(column);
        }

        private void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _previewRows.Length)
                return;

            AdvanceElevationAlignDisposition disposition = _previewRows[e.RowIndex].Disposition;
            _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = disposition == AdvanceElevationAlignDisposition.Ready
                ? Color.FromArgb(255, 249, 214)
                : (disposition == AdvanceElevationAlignDisposition.SkippedVerticalOutside
                    ? Color.FromArgb(238, 238, 238)
                    : Color.White);
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            bool ok = AdvanceElevationAlignService.TryApplyPreview(
                _targetNodeIds,
                _vectorNodeIds,
                _previewRows,
                out int updatedCount,
                out int unchangedCount,
                out int skippedCount,
                out int precisionSkippedCount,
                out string message);

            if (!ok)
            {
                TopMostMessageBox.ShowWarning(message, "Advance");
                return;
            }

            ResultMessage =
                $"Y-only alignment completed. Updated: {updatedCount}; unchanged: {unchangedCount}; " +
                $"vertical skipped: {skippedCount}; within STAAD precision: {precisionSkippedCount}.\n" +
                message;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string FormatVector(int[] vectorNodeIds)
        {
            return vectorNodeIds != null && vectorNodeIds.Length == 2
                ? $"{vectorNodeIds[0]} -> {vectorNodeIds[1]}"
                : "-";
        }
    }
}
