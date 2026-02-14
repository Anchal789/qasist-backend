using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;
using System.Text.Json;

namespace QAsist.Infrastructure.Services
{
    public class MockAiService : IAiService
    {
        private readonly ILogger<MockAiService> _logger;

        public MockAiService(ILogger<MockAiService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateCompletionAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MockAiService: Generating completion");
            await Task.Delay(1000, cancellationToken);
            return "Mock completion response";
        }

        public async Task<T> GenerateJsonResponseAsync<T>(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogInformation("MockAiService: Generating JSON response");

            await Task.Delay(1500, cancellationToken);

            var endpoints = ExtractEndpointsSafely(userPrompt);
            var testCases = GenerateMockTestCases(endpoints);

            var response = new
            {
                testCases // ⚠ must be camelCase to match JSON expectation
            };

            var json = JsonSerializer.Serialize(response);

            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation(
                "MockAiService: Generated {Count} mock test cases",
                testCases.Count
            );

            return result ?? throw new InvalidOperationException("Mock AI response deserialization failed");
        }

        // --------------------------------------------------

        private List<(string Method, string Path)> ExtractEndpointsSafely(string prompt)
        {
            var result = new List<(string, string)>();

            try
            {
                var jsonStart = prompt.IndexOf('{');
                if (jsonStart < 0) throw new Exception("No JSON found");

                var json = prompt.Substring(jsonStart);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("paths", out var paths))
                    throw new Exception("No paths found");

                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        result.Add((method.Name.ToUpperInvariant(), path.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OpenAPI parsing failed, using fallback endpoints. Reason: {Message}", ex.Message);

                result.AddRange(new[]
                {
                    ("POST", "/api/Auth/login"),
                    ("POST", "/api/Auth/refresh"),
                    ("GET", "/api/Projects"),
                    ("POST", "/api/Projects"),
                    ("PUT", "/api/Projects/{id}"),
                    ("DELETE", "/api/Projects/{id}")
                });
            }

            return result;
        }

        // --------------------------------------------------

        private List<GeneratedTestCaseDto> GenerateMockTestCases(
            List<(string Method, string Path)> endpoints)
        {
            var list = new List<GeneratedTestCaseDto>();

            foreach (var (method, path) in endpoints)
            {
                // Positive
                list.Add(new GeneratedTestCaseDto
                {
                    Endpoint = path,
                    Method = method,
                    Title = $"{method} {path} with valid data",
                    Steps = new()
                    {
                        $"Send {method} request to {path} with valid payload"
                    },
                    ExpectedResult = GetExpectedResult(method),
                    Priority = "High"
                });

                // Validation
                if (method is "POST" or "PUT")
                {
                    list.Add(new GeneratedTestCaseDto
                    {
                        Endpoint = path,
                        Method = method,
                        Title = $"{method} {path} with missing required fields",
                        Steps = new()
                        {
                            $"Send {method} request with invalid payload"
                        },
                        ExpectedResult = "400 Bad Request",
                        Priority = "Medium"
                    });
                }

                // Auth
                list.Add(new GeneratedTestCaseDto
                {
                    Endpoint = path,
                    Method = method,
                    Title = $"{method} {path} without authentication",
                    Steps = new()
                    {
                        $"Send {method} request without Authorization header"
                    },
                    ExpectedResult = "401 Unauthorized",
                    Priority = "High"
                });
            }

            return list;
        }

        private static string GetExpectedResult(string method) =>
            method switch
            {
                "POST" => "201 Created",
                "DELETE" => "204 No Content",
                _ => "200 OK"
            };
    }
}
