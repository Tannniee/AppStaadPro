using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        public static void ExportToExcel(dynamic os)
        {
            ExportToExcel(os, CoatingExportMode.Galvanizing);
        }

        public static void ExportToExcel(dynamic os, CoatingExportMode mode)
        {
            if (os == null) return;

            var stats = ScanModel((object)os, out int scanErrorCount, out List<string> scanErrorSamples)
                .OrderBy(x => GetSectionSortPriority(x))
                .ThenByDescending(x => x.Count)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var excel = new Excel.Application();
            excel.Visible = true;
            var wb = excel.Workbooks.Add();
            var ws = (Excel.Worksheet)wb.Worksheets[1];
            ws.Name = "Section Manager";
            (ws.Range["A1", "AZ500"] as Excel.Range).Interior.Color =
                System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
            int mainStartCol = 2; // Start main export table at column B

            var title = ws.Range[ws.Cells[1, mainStartCol], ws.Cells[2, mainStartCol + 9]] as Excel.Range;
            title.Merge();
            title.Value = "SECTION MANAGER & WEIGHT ESTIMATION";
            title.Font.Bold = true;
            title.Font.Size = 14;
            title.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            title.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            title.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 229));
            title.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            int sc = 13;
            var statsTitle = ws.Range[ws.Cells[1, sc], ws.Cells[2, sc + 1]] as Excel.Range;
            statsTitle.Merge();
            statsTitle.Value = "STATISTICS";
            statsTitle.Font.Bold = true;
            statsTitle.Font.Size = 12;
            statsTitle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            statsTitle.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            statsTitle.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 204, 229));
            statsTitle.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            int r = 3;
            ws.Cells[r, mainStartCol + 0] = "ID";
            ws.Cells[r, mainStartCol + 1] = "Section Name";
            ws.Cells[r, mainStartCol + 2] = "Type";
            ws.Cells[r, mainStartCol + 3] = "Material";
            ws.Cells[r, mainStartCol + 4] = "Count";
            ws.Cells[r, mainStartCol + 5] = "Total Length (m)";
            ws.Cells[r, mainStartCol + 6] = "Area (cm2)";
            ws.Cells[r, mainStartCol + 7] = "Density (kg/m3)";
            ws.Cells[r, mainStartCol + 8] = "Unit Weight (kg/m)";
            ws.Cells[r, mainStartCol + 9] = "Total Weight (kg)";
            var header = ws.Range[ws.Cells[r, mainStartCol], ws.Cells[r, mainStartCol + 9]] as Excel.Range;
            header.Font.Bold = true;
            header.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(200, 200, 200));

            var statusRange = ws.Range[ws.Cells[3, sc], ws.Cells[3, sc + 1]] as Excel.Range;
            statusRange.Merge();
            statusRange.Value = "Status: Updated " + DateTime.Now.ToString("HH:mm:ss") +
                                (scanErrorCount > 0 ? $" (warnings: {scanErrorCount})" : "");
            statusRange.Font.Bold = true;
            statusRange.Font.Color = System.Drawing.ColorTranslator.ToOle(
                scanErrorCount > 0 ? System.Drawing.Color.DarkOrange : System.Drawing.Color.Green);
            statusRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            statusRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            r = 4;
            int id = 1;
            foreach (var s in stats)
            {
                s.Id = id++;
                ws.Cells[r, mainStartCol + 0] = s.Id;
                ws.Cells[r, mainStartCol + 1] = s.Name;
                ws.Cells[r, mainStartCol + 2] = s.Type;
                ws.Cells[r, mainStartCol + 3] = s.Material;
                ws.Cells[r, mainStartCol + 4] = s.Count;
                ws.Cells[r, mainStartCol + 5] = Math.Round(s.TotalLength, 1);
                ws.Cells[r, mainStartCol + 6] = Math.Round(s.Area * 10000.0, 1);
                ws.Cells[r, mainStartCol + 7] = Math.Round(s.Density, 1);
                ws.Cells[r, mainStartCol + 8] = Math.Round(s.Area * s.Density, 1);
                ws.Cells[r, mainStartCol + 9] = Math.Round(s.TotalWeight, 1);

                var rowRange = ws.Range[ws.Cells[r, mainStartCol], ws.Cells[r, mainStartCol + 9]] as Excel.Range;
                bool even = ((r - 4) % 2 == 0);
                rowRange.Interior.Color = even
                    ? System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White)
                    : System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(220, 220, 220));
                r++;
            }

            double totalLen = stats.Sum(x => x.TotalLength);
            double totalSteel = stats.Where(x => x.Density >= 3000).Sum(x => x.TotalWeight);
            double totalConcrete = stats.Where(x => x.Density < 3000).Sum(x => x.TotalWeight);
            int totalCount = stats.Sum(x => x.Count);

            var statHeader = ws.Range[ws.Cells[4, sc], ws.Cells[4, sc + 1]] as Excel.Range;
            statHeader.Merge();
            statHeader.Value = "GRAND TOTALS";
            statHeader.Font.Bold = true;
            statHeader.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            ws.Cells[5, sc] = "Total Count:";
            ws.Cells[5, sc + 1] = totalCount;
            ws.Cells[6, sc] = "Total Length (m):";
            ws.Cells[6, sc + 1] = Math.Round(totalLen, 1);
            ws.Cells[7, sc] = "Total Weight Concrete (Ton):";
            ws.Cells[7, sc + 1] = Math.Round(totalConcrete / 1000.0, 2);
            ws.Cells[8, sc] = "Total Weight Steel (Ton):";
            var steelTonCell = ws.Cells[8, sc + 1] as Excel.Range;
            steelTonCell.Value = Math.Round(totalSteel / 1000.0, 2);
            steelTonCell.Font.Bold = true;
            steelTonCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
            steelTonCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
            ws.Cells[9, sc] = "Total Weight Steel (kg):";
            ws.Cells[9, sc + 1] = Math.Round(totalSteel, 1);
            (ws.Range[ws.Cells[4, sc], ws.Cells[9, sc + 1]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            int startRow = 11;
            if (mode == CoatingExportMode.Galvanizing)
            {
                startRow = WriteGalvanizingTables(ws, stats, startRow, sc);
            }
            else
            {
                startRow = WriteMassTables(ws, stats, startRow, sc);
            }

            (ws.Columns as Excel.Range).AutoFit();
            (ws.Range[ws.Cells[3, mainStartCol], ws.Cells[Math.Max(r - 1, 3), mainStartCol + 9]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            if (scanErrorCount > 0)
            {
                StaadSteelApp.TopMostMessageBox.ShowWarning(
                    $"Section Manager export completed with {scanErrorCount} warning(s). " +
                    "Some section/material properties could not be read from STAAD, so results may be inaccurate. " +
                    "See the 'Errors' worksheet for details.",
                    "STAAD Read Warning");

                try
                {
                    var wsErr = (Excel.Worksheet)wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]);
                    wsErr.Name = "Errors";
                    wsErr.Cells[1, 1] = "Total warnings:";
                    wsErr.Cells[1, 2] = scanErrorCount;
                    wsErr.Cells[2, 1] = $"Sample (first {MaxScanErrorSamples}):";

                    int er = 4;
                    if (scanErrorSamples != null)
                    {
                        foreach (string msg in scanErrorSamples)
                        {
                            wsErr.Cells[er++, 1] = msg;
                        }
                    }

                    (wsErr.Columns as Excel.Range).AutoFit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to create Errors worksheet: " + ex);
                }
            }
        }

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
            if (list.Count == 0) return row;

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
            double sumArea = 0, sumZinc = 0, sumLen = 0, sumSteel = 0;

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
            if (list.Count == 0) return row;
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
            double sumCount = 0, sumLen = 0, sumSteel = 0;
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
            ws.Cells[row + 3, c] = (sumLen > 1e-9) ? Math.Round(sumSteel / sumLen, 2) : 0;
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
