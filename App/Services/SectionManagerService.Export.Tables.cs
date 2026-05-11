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
            double sumArea = 0;
            double sumZinc = 0;
            double sumLen = 0;
            double sumSteel = 0;

            foreach (var pair in list)
            {
                var vals = pair.Value.ToList();
                var first = vals.First();
                double area = vals.Sum(x => x.Perimeter * x.TotalLength);
                double zinc = vals.Sum(x => x.ZincMass);
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
                ws.Cells[row + 5, c] = Math.Round(zinc, 2);
                ws.Cells[row + 6, c] = Math.Round(len, 2);
                ws.Cells[row + 7, c] = Math.Round(steel, 2);

                sumArea += area;
                sumZinc += zinc;
                sumLen += len;
                sumSteel += steel;
                c++;
            }

            ws.Cells[row, c] = "Sum";
            var sumHeader = ws.Cells[row, c] as Excel.Range;
            sumHeader.Font.Bold = true;
            sumHeader.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(198, 224, 180));
            ws.Cells[row + 4, c] = Math.Round(sumArea, 2);
            ws.Cells[row + 5, c] = Math.Round(sumZinc, 2);
            ws.Cells[row + 6, c] = Math.Round(sumLen, 2);
            ws.Cells[row + 7, c] = Math.Round(sumSteel, 2);

            var zincCell = ws.Cells[row + 5, c] as Excel.Range;
            zincCell.Font.Bold = true;
            zincCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
            zincCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);

            (ws.Range[ws.Cells[row, col], ws.Cells[row + 6, c]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            return row + 9;
        }

        private static int WriteMassTables(Excel.Worksheet ws, List<SectionStat> stats, int row, int col)
        {
            var steel = stats.Where(x => x.Density >= 3000).ToList();
            var plateTypes = new[] { "I-Section", "Tapered" };

            var otherGroups = steel.Where(s => !plateTypes.Contains(s.Type) && s.Type != "Circle")
                .GroupBy(s => s.Name)
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<string, IEnumerable<SectionStat>>(g.Key, g.AsEnumerable()))
                .ToList();

            var plateGroups = steel.Where(s => plateTypes.Contains(s.Type) && s.Thickness > 0.1)
                .GroupBy(s => (int)Math.Round(s.Thickness))
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<string, IEnumerable<SectionStat>>(g.Key + " mm", g.AsEnumerable()))
                .ToList();

            row = WriteMassTableByName(ws, otherGroups, "Mass Item", row, col);
            if (plateGroups.Count > 0)
                row = WriteMassTableByName(ws, plateGroups, "Plate Thickness", row + 1, col);

            return row;
        }

        private static int WriteMassTableByName(
            Excel.Worksheet ws,
            IEnumerable<KeyValuePair<string, IEnumerable<SectionStat>>> groups,
            string title,
            int row,
            int col)
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

            ws.Cells[row + 1, col] = "Count";
            ws.Cells[row + 2, col] = "Total Length (m)";
            ws.Cells[row + 3, col] = "Unit Weight (kg/m)";
            ws.Cells[row + 4, col] = "Steel Mass (kg)";
            var rowTitles = ws.Range[ws.Cells[row + 1, col], ws.Cells[row + 4, col]] as Excel.Range;
            rowTitles.Font.Bold = true;
            rowTitles.Interior.Color = plum60;

            int c = col + 1;
            double sumCount = 0;
            double sumLen = 0;
            double sumSteel = 0;
            foreach (var pair in list)
            {
                var vals = pair.Value.ToList();
                double count = vals.Sum(x => x.Count);
                double len = vals.Sum(x => x.TotalLength);
                double steel = vals.Sum(x => x.TotalWeight);
                double unit = len > 1e-9 ? steel / len : 0;

                ws.Cells[row, c] = pair.Key;
                var h = ws.Cells[row, c] as Excel.Range;
                h.Font.Bold = true;
                h.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                h.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 153));
                h.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

                ws.Cells[row + 1, c] = Math.Round(count, 0);
                ws.Cells[row + 2, c] = Math.Round(len, 2);
                ws.Cells[row + 3, c] = Math.Round(unit, 2);
                ws.Cells[row + 4, c] = Math.Round(steel, 2);

                sumCount += count;
                sumLen += len;
                sumSteel += steel;
                c++;
            }

            ws.Cells[row, c] = "Sum";
            var sumHeader = ws.Cells[row, c] as Excel.Range;
            sumHeader.Font.Bold = true;
            sumHeader.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 153));
            ws.Cells[row + 1, c] = Math.Round(sumCount, 0);
            ws.Cells[row + 2, c] = Math.Round(sumLen, 2);
            ws.Cells[row + 3, c] = sumLen > 1e-9 ? Math.Round(sumSteel / sumLen, 2) : 0;
            ws.Cells[row + 4, c] = Math.Round(sumSteel, 2);

            var massCell = ws.Cells[row + 4, c] as Excel.Range;
            massCell.Font.Bold = true;
            massCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Black);
            massCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);

            (ws.Range[ws.Cells[row, col], ws.Cells[row + 4, c]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            return row + 6;
        }
    }
}
