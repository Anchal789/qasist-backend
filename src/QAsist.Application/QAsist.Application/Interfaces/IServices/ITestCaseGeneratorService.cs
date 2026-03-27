using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    //public interface ITestCaseGeneratorService
    //{
    //    Task<TestCaseGenerationResultDto> GenerateFromOpenApiAsync(
    //        GenerateTestCasesRequestDto request,
    //        CancellationToken cancellationToken = default);
    //}

    /// <summary>
    /// AI test case generator service
    /// </summary>
    public interface ITestCaseGeneratorService
    {
        /// <summary>
        /// Generate test cases from OpenAPI spec for a specific project
        /// </summary>
        Task<TestCaseGenerationResultDto> GenerateFromOpenApiAsync(
            GenerateTestCasesRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
