using System;
using System.Collections.Generic;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        private static int WriteGalvanizingTables(Excel.Worksheet ws, List<SectionStat> stats, int startRow, int startCol)
        {
            var steel = stats.Where(x => x.Density >= 3000).ToList();
            var plateGroups = steel.Where(s => s.Type == "I-Section" || s.Type == "Tapered")
                                  .GroupBy(s => (int)Math.Round(s.Thickness))
                                  .OrderBy(g => g.Key)
                                  .ToList();
            var otherGroups = steel.Where(s => s.Type != "I-Section" && s.Type != "Tapered" && s.Type != "Circle")
                                   .GroupBy(s => s.Name)
                                   .OrderBy(g => g.Key)
                                   .ToList();

            var namedOthers = otherGroups
                .Select(g => new KeyValuePair<string, IEnumerable<SectionStat>>(g.Key, g.AsEnumerable()))
                .ToList();
            startRow = WriteCoatingTableByName(ws, namedOthers, "Galvanizing Item", startRow, startCol);
            if (plateGroups.Count > 0)
            {
                var named = plateGroups.Select(g => new KeyValuePair<string, IEnumerable<SectionStat>>(g.Key + " mm", g.AsEnumerable())).ToList();
                startRow = WriteCoatingTableByName(ws, named, "Plate Thickness", startRow + 1, startCol);
            }

            return startRow;
        }

        private static int WriteCoatingTableByName(Excel.Worksheet ws, IEnumerable<KeyValuePair<string, IEnumerable<SectionStat>>> groups, string title, int row, int col)
        {
            var list = groups.ToList();
            if (list.Count == 0)
                return row;

            ws.Cells[row, col] = title;
            var leftHeader = ws.Cells[row, col] as Excel.Range;
            leftHeader.Font.Bold = true;
            leftHeader.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(244, 176, 132));
            leftHeader.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            ws.Cells[row + 1, col] = "ASTM A123 Category";
            ws.Cells[row + 2, col] = "Coating Grade";
            ws.Cells[row + 3, col] = "Coating (g/m2)";
            ws.Cells[row + 4, col] = "Galv Area (m2)";
            ws.Cells[row + 5, col] = "Zinc Mass (kg)";
            ws.Cells[row + 6, col] = "Length (m)";
            ws.Cells[row + 7, col] = "Steel Mass (kg)";
            var rowTitles = ws.Range[ws.Cells[row + 1, col], ws.Cells[row + 6, col]] as Excel.Range;
            rowTitles.Font.Bold = true;
            rowTitles.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(244, 176, 132));

            int c = col + 1;

            foreach (var pair in list)
            {
                var vals = pair.Value.ToList();
                var first = vals.First();
                double area = vals.Sum(x => x.Perimeter * x.TotalLength);
                double len = vals.Sum(x => x.TotalLength);
                double steel = vals.Sum(x => x.TotalWeight);

                ws.Cells[row, c] = pair.Key;
                var h = ws.Cells[row, c] as Excel.Range;
                h.Font.Bold = true;
                h.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                h.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(198, 224, 180));
                h.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                ws.Cells[row + 1, c] = first.ASTMA123Category;
                ws.Cells[row + 2, c] = first.CoatingGrade;
                ws.Cells[row + 3, c] = first.CoatingWeight;
                ws.Cells[row + 4, c] = Math.Round(area, 2);
                (ws.Cells[row + 5, c] as Excel.Range).FormulaR1C1 = "=R[-1]C*R[-2]C/1000";
                ws.Cells[row + 6, c] = Math.Round(len, 2);
                ws.Cells[row + 7, c] = Math.Round(steel, 2);
                c++;
            }

            ws.Cells[row, c] = "Sum";
            var sumHeader = ws.Cells[row, c] as Excel.Range;
            sumHeader.Font.Bold = true;
            sumHeader.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(198, 224, 180));
            int firstGroupColumn = col + 1;
            int lastGroupColumn = c - 1;
            (ws.Cells[row + 4, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 4}C{firstGroupColumn}:R{row + 4}C{lastGroupColumn})";
            (ws.Cells[row + 5, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 5}C{firstGroupColumn}:R{row + 5}C{lastGroupColumn})";
            (ws.Cells[row + 6, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 6}C{firstGroupColumn}:R{row + 6}C{lastGroupColumn})";
            (ws.Cells[row + 7, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 7}C{firstGroupColumn}:R{row + 7}C{lastGroupColumn})";

            var zincCell = ws.Cells[row + 5, c] as Excel.Range;
            zincCell.Font.Bold = true;
            zincCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
            zincCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);

            (ws.Range[ws.Cells[row, col], ws.Cells[row + 6, c]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            return row + 9;
        }

        private static int WriteMassTables(
            Excel.Worksheet ws,
            List<SectionStat> stats,
            int row,
            int col,
            SectionMassBreakdownOptions options)
        {
            var steel = stats.Where(IsSteelForMassBreakdown).ToList();
            options = options ?? SectionMassBreakdownOptions.Empty;

            var massItemGroups = steel.Where(options.IsRolled)
                .GroupBy(s => s.Name)
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<string, IEnumerable<MassTableItem>>(
                    g.Key,
                    g.Select(ToMassTableItem).ToList()))
                .ToList();

            var plateGroups = steel.Where(s => !options.IsRolled(s))
                .SelectMany(GetPlateMassTableItems)
                .GroupBy(x => (int)Math.Round(x.Thickness))
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<string, IEnumerable<MassTableItem>>(
                    g.Key + " mm",
                    g.Select(x => x.Item).ToList()))
                .ToList();

            row = WriteMassTableByName(ws, massItemGroups, "Mass Item", row, col);
            if (plateGroups.Count > 0)
                row = WriteMassTableByName(ws, plateGroups, "Plate Thickness", row + 1, col, "Piece Count");

            return row;
        }

        private sealed class MassTableItem
        {
            public int Count { get; set; }
            public double TotalLength { get; set; }
            public double SteelMass { get; set; }
        }

        private sealed class PlateMassTableItem
        {
            public double Thickness { get; set; }
            public MassTableItem Item { get; set; }
        }

        private static MassTableItem ToMassTableItem(SectionStat stat)
        {
            return new MassTableItem
            {
                Count = stat.Count,
                TotalLength = stat.TotalLength,
                SteelMass = stat.TotalWeight
            };
        }

        private static IEnumerable<PlateMassTableItem> GetPlateMassTableItems(SectionStat stat)
        {
            if (stat == null)
                yield break;

            if (stat.PlateMasses != null && stat.PlateMasses.Count > 0)
            {
                foreach (var plate in stat.PlateMasses)
                {
                    if (plate.Thickness <= 0 || plate.Mass <= 0)
                        continue;

                    yield return new PlateMassTableItem
                    {
                        Thickness = plate.Thickness,
                        Item = new MassTableItem
                        {
                            Count = plate.Count,
                            TotalLength = plate.TotalLength,
                            SteelMass = plate.Mass
                        }
                    };
                }
                yield break;
            }

            if (stat.Thickness > 0.1)
            {
                yield return new PlateMassTableItem
                {
                    Thickness = stat.Thickness,
                    Item = ToMassTableItem(stat)
                };
            }
        }

        private static int WriteMassTableByName(
            Excel.Worksheet ws,
            IEnumerable<KeyValuePair<string, IEnumerable<MassTableItem>>> groups,
            string title,
            int row,
            int col,
            string countLabel = "Count")
        {
            var list = groups.ToList();
            if (list.Count == 0)
                return row;

            int plum60 = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(241, 217, 241));

            ws.Cells[row, col] = title;
            var leftHeader = ws.Cells[row, col] as Excel.Range;
            leftHeader.Font.Bold = true;
            leftHeader.Interior.Color = plum60;
            leftHeader.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            ws.Cells[row + 1, col] = countLabel;
            ws.Cells[row + 2, col] = "Total Length (m)";
            ws.Cells[row + 3, col] = "Unit Weight (kg/m)";
            ws.Cells[row + 4, col] = "Steel Mass (kg)";
            var rowTitles = ws.Range[ws.Cells[row + 1, col], ws.Cells[row + 4, col]] as Excel.Range;
            rowTitles.Font.Bold = true;
            rowTitles.Interior.Color = plum60;

            int c = col + 1;
            foreach (var pair in list)
            {
                var vals = pair.Value.ToList();
                double count = vals.Sum(x => x.Count);
                double len = vals.Sum(x => x.TotalLength);
                double steel = vals.Sum(x => x.SteelMass);

                ws.Cells[row, c] = pair.Key;
                var h = ws.Cells[row, c] as Excel.Range;
                h.Font.Bold = true;
                h.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                h.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 153));
                h.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

                ws.Cells[row + 1, c] = Math.Round(count, 0);
                ws.Cells[row + 2, c] = Math.Round(len, 2);
                (ws.Cells[row + 3, c] as Excel.Range).FormulaR1C1 = "=IFERROR(R[1]C/R[-1]C,0)";
                ws.Cells[row + 4, c] = Math.Round(steel, 2);
                c++;
            }

            ws.Cells[row, c] = "Sum";
            var sumHeader = ws.Cells[row, c] as Excel.Range;
            sumHeader.Font.Bold = true;
            sumHeader.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 153));
            int firstGroupColumn = col + 1;
            int lastGroupColumn = c - 1;
            (ws.Cells[row + 1, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 1}C{firstGroupColumn}:R{row + 1}C{lastGroupColumn})";
            (ws.Cells[row + 2, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 2}C{firstGroupColumn}:R{row + 2}C{lastGroupColumn})";
            (ws.Cells[row + 3, c] as Excel.Range).FormulaR1C1 = "=IFERROR(R[1]C/R[-1]C,0)";
            (ws.Cells[row + 4, c] as Excel.Range).FormulaR1C1 =
                $"=SUM(R{row + 4}C{firstGroupColumn}:R{row + 4}C{lastGroupColumn})";

            var massCell = ws.Cells[row + 4, c] as Excel.Range;
            massCell.Font.Bold = true;
            massCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Black);
            massCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);

            (ws.Range[ws.Cells[row, col], ws.Cells[row + 4, c]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            return row + 6;
        }
    }
}
