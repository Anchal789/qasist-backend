using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    /// <summary>
    /// Excel export service for test cases
    /// </summary>
    public interface ITestCaseExportService
    {
        /// <summary>
        /// Export test cases to Excel (.xlsx)
        /// Returns file bytes and filename
        /// </summary>
        Task<(byte[] FileBytes, string FileName)> ExportToExcelAsync(
            Guid projectId,
            string projectName,
            List<GeneratedTestCaseDto> testCases,
            CancellationToken cancellationToken = default);
    }
}
