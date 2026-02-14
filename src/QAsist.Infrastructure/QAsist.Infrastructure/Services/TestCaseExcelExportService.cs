using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;

namespace QAsist.Infrastructure.Services
{
    public class TestCaseExcelExportService : ITestCaseExportService
    {
        private readonly ILogger<TestCaseExcelExportService> _logger;

        public TestCaseExcelExportService(ILogger<TestCaseExcelExportService> logger)
        {
            _logger = logger;
        }

        public async Task<(byte[] FileBytes, string FileName)> ExportToExcelAsync(
            Guid projectId,
            string projectName,
            List<GeneratedTestCaseDto> testCases,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                _logger.LogInformation(
                    "Exporting {Count} test cases to Excel for Project: {ProjectName}",
                    testCases.Count,
                    projectName);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Test Cases");

                // Apply professional styling
                ApplyWorksheetStyling(worksheet);

                // Add headers
                AddHeaders(worksheet);

                // Add data rows
                AddDataRows(worksheet, testCases, projectName);

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Generate file
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var fileBytes = stream.ToArray();

                var fileName = GenerateFileName(projectName);

                _logger.LogInformation(
                    "Excel export completed. File: {FileName}, Size: {Size} bytes",
                    fileName,
                    fileBytes.Length);

                return (fileBytes, fileName);
            }, cancellationToken);
        }

        private void ApplyWorksheetStyling(IXLWorksheet worksheet)
        {
            // Set default font
            worksheet.Style.Font.FontName = "Calibri";
            worksheet.Style.Font.FontSize = 11;

            // Freeze header row
            worksheet.SheetView.FreezeRows(1);
        }

        private void AddHeaders(IXLWorksheet worksheet)
        {
            var headers = new[]
            {
            "Test Case ID",
            "Project",
            "Endpoint",
            "Method",
            "Title",
            "Steps",
            "Expected Result",
            "Priority"
        };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        private void AddDataRows(
            IXLWorksheet worksheet,
            List<GeneratedTestCaseDto> testCases,
            string projectName)
        {
            int row = 2;

            foreach (var testCase in testCases)
            {
                worksheet.Cell(row, 1).Value = $"TC-{row - 1:D3}";
                worksheet.Cell(row, 2).Value = projectName;
                worksheet.Cell(row, 3).Value = testCase.Endpoint;
                worksheet.Cell(row, 4).Value = testCase.Method;
                worksheet.Cell(row, 5).Value = testCase.Title;
                worksheet.Cell(row, 6).Value = string.Join("\n", testCase.Steps);
                worksheet.Cell(row, 7).Value = testCase.ExpectedResult;
                worksheet.Cell(row, 8).Value = testCase.Priority;

                // Apply row styling
                var rowRange = worksheet.Range(row, 1, row, 8);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Alternating row colors
                if (row % 2 == 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                }

                // Method color coding
                var methodCell = worksheet.Cell(row, 4);
                methodCell.Style.Font.Bold = true;
                methodCell.Style.Font.FontColor = GetMethodColor(testCase.Method);

                // Priority color coding
                var priorityCell = worksheet.Cell(row, 8);
                priorityCell.Style.Font.Bold = true;
                priorityCell.Style.Font.FontColor = GetPriorityColor(testCase.Priority);

                // Wrap text for Steps column
                worksheet.Cell(row, 6).Style.Alignment.WrapText = true;

                row++;
            }

            // Set column widths
            worksheet.Column(1).Width = 12;  // Test Case ID
            worksheet.Column(2).Width = 20;  // Project
            worksheet.Column(3).Width = 30;  // Endpoint
            worksheet.Column(4).Width = 10;  // Method
            worksheet.Column(5).Width = 40;  // Title
            worksheet.Column(6).Width = 50;  // Steps
            worksheet.Column(7).Width = 40;  // Expected Result
            worksheet.Column(8).Width = 12;  // Priority
        }

        private XLColor GetMethodColor(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => XLColor.FromArgb(46, 125, 50),      // Green
                "POST" => XLColor.FromArgb(25, 118, 210),    // Blue
                "PUT" => XLColor.FromArgb(237, 108, 2),      // Orange
                "PATCH" => XLColor.FromArgb(156, 39, 176),   // Purple
                "DELETE" => XLColor.FromArgb(211, 47, 47),   // Red
                _ => XLColor.Black
            };
        }

        private XLColor GetPriorityColor(string priority)
        {
            return priority.ToLowerInvariant() switch
            {
                "critical" => XLColor.FromArgb(183, 28, 28),  // Dark Red
                "high" => XLColor.FromArgb(237, 108, 2),      // Orange
                "medium" => XLColor.FromArgb(25, 118, 210),   // Blue
                "low" => XLColor.FromArgb(117, 117, 117),     // Gray
                _ => XLColor.Black
            };
        }

        private string GenerateFileName(string projectName)
        {
            var sanitizedProjectName = SanitizeFileName(projectName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return $"QAsist_{sanitizedProjectName}_AI_TestCases_{timestamp}.xlsx";
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(sanitized) ? "Project" : sanitized;
        }
    }
}
