using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class MassBreakdownPreviewForm : Form
    {
        private readonly BindingList<Row> _rows;
        private readonly DataGridView _grid;
        private readonly Button _btnExport;
        private readonly Button _btnCancel;

        public MassBreakdownPreviewForm(IEnumerable<SectionStat> stats)
        {
            _rows = new BindingList<Row>((stats ?? Enumerable.Empty<SectionStat>())
                .Where(SectionManagerService.IsSteelForMassBreakdown)
                .Select(Row.FromStat)
                .ToList());

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                DataSource = _rows
            };

            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(Row.Rolled),
                HeaderText = "Rolled",
                Width = 62,
                FlatStyle = FlatStyle.Standard
            });
            AddTextColumn(nameof(Row.Id), "ID", 48, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(Row.Name), "Section Name", 190, DataGridViewContentAlignment.MiddleLeft);
            AddTextColumn(nameof(Row.Type), "Type", 90, DataGridViewContentAlignment.MiddleLeft);
            AddTextColumn(nameof(Row.Material), "Material", 80, DataGridViewContentAlignment.MiddleLeft);
            AddTextColumn(nameof(Row.Count), "Count", 70, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(Row.TotalLength), "Total Length (m)", 110, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(Row.Area), "Area (cm2)", 90, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(Row.UnitWeight), "Unit Weight (kg/m)", 120, DataGridViewContentAlignment.MiddleRight);
            AddTextColumn(nameof(Row.TotalWeight), "Total Weight (kg)", 120, DataGridViewContentAlignment.MiddleRight);

            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            _grid.RowPrePaint += Grid_RowPrePaint;

            _btnExport = new Button
            {
                Text = "Export",
                DialogResult = DialogResult.OK,
                Width = 90,
                Height = 30,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnExport.Click += BtnExport_Click;

            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 90,
                Height = 30,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                BackColor = SystemColors.Control
            };
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnExport);

            Text = "Mass Breakdown Preview";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            Width = 1060;
            Height = 580;
            AcceptButton = _btnExport;
            CancelButton = _btnCancel;
            Controls.Add(_grid);
            Controls.Add(buttons);
        }

        public SectionMassBreakdownOptions CreateOptions()
        {
            return new SectionMassBreakdownOptions(_rows.Where(x => x.Rolled).Select(x => x.Id));
        }

        private void AddTextColumn(string propertyName, string headerText, int width, DataGridViewContentAlignment alignment)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                Width = width,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = alignment }
            };
            _grid.Columns.Add(column);
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                _grid.InvalidateRow(e.RowIndex);
        }

        private void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;

            var rowStyle = _grid.Rows[e.RowIndex].DefaultCellStyle;
            rowStyle.BackColor = _rows[e.RowIndex].Rolled
                ? Color.FromArgb(252, 228, 214)
                : Color.White;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            _grid.EndEdit();
        }

        private sealed class Row
        {
            public bool Rolled { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Material { get; set; }
            public int Count { get; set; }
            public double TotalLength { get; set; }
            public double Area { get; set; }
            public double UnitWeight { get; set; }
            public double TotalWeight { get; set; }

            public static Row FromStat(SectionStat stat)
            {
                return new Row
                {
                    Id = stat.Id,
                    Name = stat.Name,
                    Type = stat.Type,
                    Material = stat.Material,
                    Count = stat.Count,
                    TotalLength = Math.Round(stat.TotalLength, 1),
                    Area = Math.Round(stat.Area * 10000.0, 1),
                    UnitWeight = Math.Round(stat.Area * stat.Density, 1),
                    TotalWeight = Math.Round(stat.TotalWeight, 1)
                };
            }
        }
    }
}
