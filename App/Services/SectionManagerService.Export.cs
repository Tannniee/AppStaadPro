using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using StaadSteelApp.Core.Interop;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        public static ServiceOperationResult ExportToExcel(object os)
        {
            return ExportToExcel(os, CoatingExportMode.Galvanizing);
        }

        public static ServiceOperationResult ExportToExcel(object os, CoatingExportMode mode)
        {
            if (os == null)
                return ServiceOperationResult.Error("Export", "Not connected to STAAD.");

            SectionManagerExportSnapshot export = BuildExportSnapshot(os);
            return ExportToExcel(export, mode, SectionMassBreakdownOptions.Empty);
        }

        internal static ServiceOperationResult ExportToExcel(
            SectionManagerExportSnapshot export,
            CoatingExportMode mode,
            SectionMassBreakdownOptions massOptions)
        {
            if (export == null)
                return ServiceOperationResult.Error("Export", "No Section Manager data to export.");

            var stats = export.Stats;
            massOptions = massOptions ?? SectionMassBreakdownOptions.Empty;

            var excel = new Excel.Application();
            excel.Visible = true;
            var wb = excel.Workbooks.Add();
            var ws = (Excel.Worksheet)wb.Worksheets[1];
            ws.Name = "Section Manager";
            (ws.Range["A1", "AZ500"] as Excel.Range).Interior.Color =
                System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
            int mainStartCol = 2; // Start main export table at column B
            int rolledFill = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(252, 228, 214));
            int nonSteelFill = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(255, 255, 153));

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
                                (export.WarningCount > 0 ? $" (warnings: {export.WarningCount})" : "");
            statusRange.Font.Bold = true;
            statusRange.Font.Color = System.Drawing.ColorTranslator.ToOle(
                export.WarningCount > 0 ? System.Drawing.Color.DarkOrange : System.Drawing.Color.Green);
            statusRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            statusRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            r = 4;
            int id = 1;
            var rowByStat = new Dictionary<SectionStat, int>();
            foreach (var s in stats)
            {
                rowByStat[s] = r;
                s.Id = id++;
                ws.Cells[r, mainStartCol + 0] = s.Id;
                ws.Cells[r, mainStartCol + 1] = s.Name;
                ws.Cells[r, mainStartCol + 2] = s.Type;
                ws.Cells[r, mainStartCol + 3] = s.Material;
                ws.Cells[r, mainStartCol + 4] = s.Count;
                ws.Cells[r, mainStartCol + 5] = s.TotalLength;
                ws.Cells[r, mainStartCol + 6] = s.Area * 10000.0;
                ws.Cells[r, mainStartCol + 7] = s.Density;
                (ws.Cells[r, mainStartCol + 8] as Excel.Range).FormulaR1C1 = "=RC[-2]*RC[-1]/10000";
                (ws.Cells[r, mainStartCol + 9] as Excel.Range).FormulaR1C1 = "=RC[-4]*RC[-1]";

                var rowRange = ws.Range[ws.Cells[r, mainStartCol], ws.Cells[r, mainStartCol + 9]] as Excel.Range;
                if (!IsSteelForMassBreakdown(s))
                {
                    rowRange.Interior.Color = nonSteelFill;
                }
                else if (mode == CoatingExportMode.MassBreakdown && massOptions.IsRolled(s))
                {
                    ws.Cells[r, 1] = "Rolled";
                    var rolledCell = ws.Cells[r, 1] as Excel.Range;
                    rolledCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    rowRange.Interior.Color = rolledFill;
                }
                else
                {
                    bool even = ((r - 4) % 2 == 0);
                    rowRange.Interior.Color = even
                        ? System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White)
                        : System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(220, 220, 220));
                }
                r++;
            }

            if (r > 4)
            {
                (ws.Range[ws.Cells[4, mainStartCol + 5], ws.Cells[r - 1, mainStartCol + 8]] as Excel.Range)
                    .NumberFormat = "#,##0.0";
                (ws.Range[ws.Cells[4, mainStartCol + 9], ws.Cells[r - 1, mainStartCol + 9]] as Excel.Range)
                    .NumberFormat = "#,##0";
            }

            var steelStats = stats.Where(IsSteelForMassBreakdown).ToList();
            var summaryStats = mode == CoatingExportMode.MassBreakdown ? steelStats : stats;
            var nonSteelStats = stats.Where(x => !IsSteelForMassBreakdown(x)).ToList();

            var statHeader = ws.Range[ws.Cells[4, sc], ws.Cells[4, sc + 1]] as Excel.Range;
            statHeader.Merge();
            statHeader.Value = "GRAND TOTALS";
            statHeader.Font.Bold = true;
            statHeader.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            ws.Cells[5, sc] = mode == CoatingExportMode.MassBreakdown ? "Total Steel Count:" : "Total Count:";
            (ws.Cells[5, sc + 1] as Excel.Range).FormulaR1C1 =
                BuildR1C1SumFormula(summaryStats, rowByStat, mainStartCol + 4);
            ws.Cells[6, sc] = "Total Length (m):";
            (ws.Cells[6, sc + 1] as Excel.Range).FormulaR1C1 =
                BuildR1C1SumFormula(summaryStats, rowByStat, mainStartCol + 5);
            ws.Cells[7, sc] = "Total Weight Concrete (Ton):";
            (ws.Cells[7, sc + 1] as Excel.Range).FormulaR1C1 =
                BuildR1C1SumFormula(nonSteelStats, rowByStat, mainStartCol + 9, 1000.0);
            ws.Cells[8, sc] = "Total Weight Steel (Ton):";
            var steelTonCell = ws.Cells[8, sc + 1] as Excel.Range;
            steelTonCell.FormulaR1C1 =
                BuildR1C1SumFormula(steelStats, rowByStat, mainStartCol + 9, 1000.0);
            steelTonCell.Font.Bold = true;
            steelTonCell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
            steelTonCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
            ws.Cells[9, sc] = "Total Weight Steel (kg):";
            (ws.Cells[9, sc + 1] as Excel.Range).FormulaR1C1 =
                BuildR1C1SumFormula(steelStats, rowByStat, mainStartCol + 9);
            (ws.Cells[5, sc + 1] as Excel.Range).NumberFormat = "#,##0";
            (ws.Cells[6, sc + 1] as Excel.Range).NumberFormat = "#,##0.0";
            (ws.Range[ws.Cells[7, sc + 1], ws.Cells[8, sc + 1]] as Excel.Range).NumberFormat = "#,##0.00";
            (ws.Cells[9, sc + 1] as Excel.Range).NumberFormat = "#,##0";
            (ws.Range[ws.Cells[4, sc], ws.Cells[9, sc + 1]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            int startRow = 11;
            if (mode == CoatingExportMode.Galvanizing)
            {
                startRow = WriteGalvanizingTables(ws, stats, startRow, sc);
            }
            else
            {
                startRow = WriteMassTables(ws, stats, startRow, sc, massOptions);
            }

            WriteCoatingSurfaceAreaWorksheet(wb, ws, stats);

            (ws.Columns as Excel.Range).AutoFit();
            (ws.Range[ws.Cells[3, 1], ws.Cells[Math.Max(r - 1, 3), 1]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            (ws.Range[ws.Cells[3, mainStartCol], ws.Cells[Math.Max(r - 1, 3), mainStartCol + 9]] as Excel.Range).Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

            if (export.WarningCount > 0)
            {
                try
                {
                    var wsErr = (Excel.Worksheet)wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]);
                    wsErr.Name = "Errors";
                    wsErr.Cells[1, 1] = "Total warnings:";
                    wsErr.Cells[1, 2] = export.WarningCount;
                    wsErr.Cells[2, 1] = $"Sample (first {MaxScanErrorSamples}):";

                    int er = 4;
                    if (export.WarningSamples != null)
                    {
                        foreach (string msg in export.WarningSamples)
                        {
                            wsErr.Cells[er++, 1] = msg;
                        }
                    }

                    (wsErr.Columns as Excel.Range).AutoFit();
                }
                catch (Exception ex)
                {
                    AppLogger.Exception("Failed to create Section Manager Errors worksheet.", ex);
                    Debug.WriteLine("Failed to create Errors worksheet: " + ex);
                }

                return ServiceOperationResult.Warning(
                    "STAAD Read Warning",
                    $"Section Manager export completed with {export.WarningCount} warning(s). " +
                    "Some section/material properties could not be read from STAAD, so results may be inaccurate. " +
                    "See the 'Errors' worksheet for details.");
            }

            return ServiceOperationResult.Success();
        }

        private static string BuildR1C1SumFormula(
            IEnumerable<SectionStat> source,
            IDictionary<SectionStat, int> rowByStat,
            int column,
            double divisor = 1.0)
        {
            var rows = (source ?? Enumerable.Empty<SectionStat>())
                .Where(rowByStat.ContainsKey)
                .Select(stat => rowByStat[stat])
                .Distinct()
                .OrderBy(row => row)
                .ToList();

            if (rows.Count == 0)
                return "=0";

            var references = new List<string>();
            int firstRow = rows[0];
            int lastRow = rows[0];
            for (int index = 1; index <= rows.Count; index++)
            {
                if (index < rows.Count && rows[index] == lastRow + 1)
                {
                    lastRow = rows[index];
                    continue;
                }

                references.Add(firstRow == lastRow
                    ? $"R{firstRow}C{column}"
                    : $"R{firstRow}C{column}:R{lastRow}C{column}");

                if (index < rows.Count)
                {
                    firstRow = rows[index];
                    lastRow = rows[index];
                }
            }

            string formula = "=SUM(" + string.Join(",", references) + ")";

            if (Math.Abs(divisor - 1.0) > 1e-9)
                formula += "/" + divisor.ToString(CultureInfo.InvariantCulture);

            return formula;
        }

        internal static bool TryBuildMassBreakdownSnapshot(
            object os,
            out SectionManagerExportSnapshot export,
            out string error)
        {
            export = null;
            error = string.Empty;
            if (os == null)
            {
                error = "Not connected to STAAD.";
                return false;
            }

            if (!OpenStaadSelectionAdapter.TryGetMassBreakdownBeamIds(
                    os, out int[] selectedBeamIds, out bool hasSelection, out error))
                return false;

            export = BuildExportSnapshot(os, hasSelection ? selectedBeamIds : null);
            return true;
        }

        internal static SectionManagerExportSnapshot BuildExportSnapshot(object os)
        {
            return BuildExportSnapshot(os, null);
        }

        private static SectionManagerExportSnapshot BuildExportSnapshot(object os, int[] selectedBeamIds)
        {
            var stats = ScanModel(os, selectedBeamIds, out int scanErrorCount, out List<string> scanErrorSamples)
                .OrderBy(x => IsSteelForMassBreakdown(x) ? 0 : 1)
                .ThenBy(x => GetSectionSortPriority(x))
                .ThenByDescending(x => x.TotalWeight)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Material, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < stats.Count; i++)
                stats[i].Id = i + 1;

            return new SectionManagerExportSnapshot(stats, scanErrorCount, scanErrorSamples, selectedBeamIds?.Length ?? 0);
        }

    }

    internal sealed class SectionManagerExportSnapshot
    {
        public SectionManagerExportSnapshot(List<SectionStat> stats, int warningCount, List<string> warningSamples, int selectedBeamCount)
        {
            Stats = stats ?? new List<SectionStat>();
            WarningCount = warningCount;
            WarningSamples = warningSamples ?? new List<string>();
            SelectedBeamCount = selectedBeamCount;
        }

        public List<SectionStat> Stats { get; }
        public int WarningCount { get; }
        public List<string> WarningSamples { get; }
        public int SelectedBeamCount { get; }
    }
}
