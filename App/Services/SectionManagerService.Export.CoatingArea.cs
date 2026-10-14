using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static partial class SectionManagerService
    {
        private const string CoatingSurfaceAreaSheetName = "Coating Surface Area";

        private static void WriteCoatingSurfaceAreaWorksheet(
            Excel.Workbook workbook,
            Excel.Worksheet mainWorksheet,
            IEnumerable<SectionStat> stats)
        {
            var steelStats = (stats ?? Enumerable.Empty<SectionStat>())
                .Where(IsSteelForMassBreakdown)
                .ToList();

            var worksheet = (Excel.Worksheet)workbook.Worksheets.Add(After: mainWorksheet);
            worksheet.Name = CoatingSurfaceAreaSheetName;

            int pink = ColorTranslator.ToOle(Color.FromArgb(255, 204, 229));
            int groupFill = ColorTranslator.ToOle(Color.FromArgb(217, 217, 217));
            int areaFill = ColorTranslator.ToOle(Color.Yellow);
            int whiteFill = ColorTranslator.ToOle(Color.White);

            // Hide the default worksheet grid visually. Borders are added only to
            // populated table regions below.
            (worksheet.Range["A1", "AZ500"] as Excel.Range).Interior.Color = whiteFill;

            Excel.Range title = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 4]] as Excel.Range;
            title.Merge();
            title.Value = "COATING SURFACE AREA";
            title.Font.Bold = true;
            title.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            title.Interior.Color = pink;

            WriteMergedHeader(worksheet, 1, 5, 10, "I-SECTION / TAPERED", groupFill);
            WriteMergedHeader(worksheet, 1, 11, 14, "PIPE", groupFill);
            WriteMergedHeader(worksheet, 1, 15, 20, "BOX / TUBE", groupFill);

            string[] headers =
            {
                "ID", "Section Name", "Type", "Total Length (m)",
                "Web Depth (mm)", "Flange Width (mm)", "Web Thickness (mm)", "Flange Thickness (mm)", "Enclosing Perimeter (m)", "Area (m2)",
                "Outside Diameter (mm)", "Thickness (mm)", "Enclosing Perimeter (m)", "Area (m2)",
                "A (mm)", "B (mm)", "Thickness A (mm)", "Thickness B (mm)", "Perimeter (m)", "Area (m2)"
            };

            for (int column = 0; column < headers.Length; column++)
            {
                worksheet.Cells[2, column + 1] = headers[column];
                var headerCell = worksheet.Cells[2, column + 1] as Excel.Range;
                headerCell.Font.Bold = true;
                headerCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                headerCell.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                headerCell.Interior.Color = column == 9 || column == 13 || column == 19
                    ? areaFill
                    : groupFill;
            }

            int row = 3;
            int iSectionFirstRow = 0;
            int iSectionLastRow = 0;
            int pipeFirstRow = 0;
            int pipeLastRow = 0;
            int boxFirstRow = 0;
            int boxLastRow = 0;

            foreach (SectionStat stat in steelStats)
            {
                worksheet.Cells[row, 1] = stat.Id;
                worksheet.Cells[row, 2] = stat.Name;
                worksheet.Cells[row, 3] = stat.Type;
                worksheet.Cells[row, 4] = stat.TotalLength;

                SectionCoatingDimensions dimensions = stat.CoatingDimensions ?? new SectionCoatingDimensions();
                if (IsISectionForCoating(stat) && HasISectionCoatingDimensions(dimensions))
                {
                    worksheet.Cells[row, 5] = RoundDimension(dimensions.WebDepthMm);
                    worksheet.Cells[row, 6] = RoundDimension(dimensions.FlangeWidthMm);
                    worksheet.Cells[row, 7] = RoundDimension(dimensions.WebThicknessMm);
                    worksheet.Cells[row, 8] = RoundDimension(dimensions.FlangeThicknessMm);
                    (worksheet.Cells[row, 9] as Excel.Range).FormulaR1C1 =
                        "=(2*RC[-4]+4*RC[-3]-2*RC[-2])/1000";
                    (worksheet.Cells[row, 10] as Excel.Range).FormulaR1C1 = "=RC[-1]*RC[-6]";
                    if (iSectionFirstRow == 0)
                        iSectionFirstRow = row;
                    iSectionLastRow = row;
                    ApplyContinuousBorder(worksheet, row, 5, row, 10);
                }
                else if (string.Equals(stat.Type, "Pipe", StringComparison.OrdinalIgnoreCase) &&
                         dimensions.PipeOutsideDiameterMm > 0)
                {
                    worksheet.Cells[row, 11] = RoundDimension(dimensions.PipeOutsideDiameterMm);
                    worksheet.Cells[row, 12] = RoundDimension(dimensions.PipeThicknessMm);
                    (worksheet.Cells[row, 13] as Excel.Range).FormulaR1C1 = "=PI()*RC[-2]/1000";
                    (worksheet.Cells[row, 14] as Excel.Range).FormulaR1C1 = "=RC[-1]*RC[-10]";
                    if (pipeFirstRow == 0)
                        pipeFirstRow = row;
                    pipeLastRow = row;
                    ApplyContinuousBorder(worksheet, row, 11, row, 14);
                }
                else if (IsBoxForCoating(stat) && dimensions.BoxAMm > 0 && dimensions.BoxBMm > 0)
                {
                    worksheet.Cells[row, 15] = RoundDimension(dimensions.BoxAMm);
                    worksheet.Cells[row, 16] = RoundDimension(dimensions.BoxBMm);
                    worksheet.Cells[row, 17] = RoundDimension(dimensions.BoxThicknessAMm);
                    worksheet.Cells[row, 18] = RoundDimension(dimensions.BoxThicknessBMm);
                    (worksheet.Cells[row, 19] as Excel.Range).FormulaR1C1 = "=2*(RC[-4]+RC[-3])/1000";
                    (worksheet.Cells[row, 20] as Excel.Range).FormulaR1C1 = "=RC[-1]*RC[-16]";
                    if (boxFirstRow == 0)
                        boxFirstRow = row;
                    boxLastRow = row;
                    ApplyContinuousBorder(worksheet, row, 15, row, 20);
                }

                ApplyContinuousBorder(worksheet, row, 1, row, 4);
                row++;
            }

            int sumRow = row + 1;
            worksheet.Cells[sumRow, 4] = "Sum";
            ApplyContinuousBorder(worksheet, sumRow, 4, sumRow, 4);

            var totalAreaReferences = new List<string>();
            if (iSectionFirstRow > 0)
            {
                (worksheet.Cells[sumRow, 10] as Excel.Range).FormulaR1C1 =
                    $"=SUM(R{iSectionFirstRow}C10:R{iSectionLastRow}C10)";
                totalAreaReferences.Add($"R{sumRow}C10");
                ApplyContinuousBorder(worksheet, sumRow, 10, sumRow, 10);
            }
            if (pipeFirstRow > 0)
            {
                (worksheet.Cells[sumRow, 14] as Excel.Range).FormulaR1C1 =
                    $"=SUM(R{pipeFirstRow}C14:R{pipeLastRow}C14)";
                totalAreaReferences.Add($"R{sumRow}C14");
                ApplyContinuousBorder(worksheet, sumRow, 14, sumRow, 14);
            }
            if (boxFirstRow > 0)
            {
                (worksheet.Cells[sumRow, 20] as Excel.Range).FormulaR1C1 =
                    $"=SUM(R{boxFirstRow}C20:R{boxLastRow}C20)";
                totalAreaReferences.Add($"R{sumRow}C20");
                ApplyContinuousBorder(worksheet, sumRow, 20, sumRow, 20);
            }

            int totalRow = sumRow + 1;
            worksheet.Cells[totalRow, 4] = "Total";
            (worksheet.Cells[totalRow, 20] as Excel.Range).FormulaR1C1 = totalAreaReferences.Count > 0
                ? "=SUM(" + string.Join(",", totalAreaReferences) + ")"
                : "=0";
            ApplyContinuousBorder(worksheet, totalRow, 4, totalRow, 4);
            ApplyContinuousBorder(worksheet, totalRow, 20, totalRow, 20);

            var sumLabels = worksheet.Range[worksheet.Cells[sumRow, 4], worksheet.Cells[totalRow, 4]] as Excel.Range;
            sumLabels.Font.Bold = true;
            foreach (int areaColumn in new[] { 10, 14, 20 })
            {
                var sumCell = worksheet.Cells[sumRow, areaColumn] as Excel.Range;
                bool hasFormula = Convert.ToBoolean(sumCell.HasFormula);
                if (hasFormula)
                {
                    sumCell.Font.Bold = true;
                    sumCell.Interior.Color = areaFill;
                }
            }
            (worksheet.Cells[totalRow, 20] as Excel.Range).Font.Bold = true;
            (worksheet.Cells[totalRow, 20] as Excel.Range).Interior.Color = areaFill;

            int lastDataRow = Math.Max(2, row - 1);
            (worksheet.Range[worksheet.Cells[3, 4], worksheet.Cells[lastDataRow, 4]] as Excel.Range).NumberFormat = "#,##0.0";
            (worksheet.Range[worksheet.Cells[3, 5], worksheet.Cells[lastDataRow, 20]] as Excel.Range).NumberFormat = "#,##0.##";
            ApplyContinuousBorder(worksheet, 1, 1, 2, 20);
            (worksheet.Columns as Excel.Range).AutoFit();
            (worksheet.Columns[2] as Excel.Range).ColumnWidth = Math.Max(28.0, (double)(worksheet.Columns[2] as Excel.Range).ColumnWidth);
            (worksheet.Rows[2] as Excel.Range).WrapText = true;
            (worksheet.Rows[2] as Excel.Range).RowHeight = 32;
            worksheet.Calculate();
        }

        private static void ApplyContinuousBorder(
            Excel.Worksheet worksheet,
            int firstRow,
            int firstColumn,
            int lastRow,
            int lastColumn)
        {
            var range = worksheet.Range[
                worksheet.Cells[firstRow, firstColumn],
                worksheet.Cells[lastRow, lastColumn]] as Excel.Range;
            range.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        private static void WriteMergedHeader(
            Excel.Worksheet worksheet,
            int row,
            int firstColumn,
            int lastColumn,
            string text,
            int fillColor)
        {
            var range = worksheet.Range[worksheet.Cells[row, firstColumn], worksheet.Cells[row, lastColumn]] as Excel.Range;
            range.Merge();
            range.Value = text;
            range.Font.Bold = true;
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range.Interior.Color = fillColor;
        }

        private static bool IsISectionForCoating(SectionStat stat)
        {
            return string.Equals(stat?.Type, "I-Section", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(stat?.Type, "Tapered", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBoxForCoating(SectionStat stat)
        {
            return string.Equals(stat?.Type, "Box/Tube", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(stat?.Type, "Rectangular", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasISectionCoatingDimensions(SectionCoatingDimensions dimensions)
        {
            return dimensions.WebDepthMm > 0 &&
                   dimensions.FlangeWidthMm > 0 &&
                   dimensions.WebThicknessMm > 0;
        }

        private static double GetISectionEnclosingPerimeter(SectionCoatingDimensions dimensions)
        {
            return Math.Max(
                0,
                (2.0 * dimensions.WebDepthMm +
                 4.0 * dimensions.FlangeWidthMm -
                 2.0 * dimensions.WebThicknessMm) / 1000.0);
        }

        private static double RoundDimension(double value)
        {
            return Math.Round(Math.Max(0, value), 2);
        }
    }
}
