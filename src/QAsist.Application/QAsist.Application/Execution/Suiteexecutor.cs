using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QAsist.Application.Execution
{
    public class SuiteExecutor : ISuiteExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SuiteExecutor> _logger;

        public SuiteExecutor(
            IHttpClientFactory httpClientFactory,
            ILogger<SuiteExecutor> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SuiteExecutionResult> ExecuteAsync(
            TestSuite suite,
            ExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Suite Execution {SuiteId}", suite.Id);

            var sw = Stopwatch.StartNew();

            var context = new Dictionary<string, string>(
                options.EnvironmentVariables ?? new());

            var stepResults = new List<StepExecutionResult>();

            var testCases = suite.TestCases?
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.OrderIndex)
                .ToList() ?? new();

            foreach (var testCase in testCases)
            {
                foreach (var step in testCase.Steps
                    .Where(s => s.IsEnabled)
                    .OrderBy(s => s.OrderIndex))
                {
                    var result = await ExecuteStepAsync(step, context, cancellationToken);

                    stepResults.Add(result);

                    // Merge extracted variables
                    foreach (var kv in result.ExtractedVariables)
                        context[kv.Key] = kv.Value;

                    if (options.FailFast &&
                        (result.Status == StepStatus.Failed ||
                         result.Status == StepStatus.Error))
                    {
                        _logger.LogWarning("FailFast triggered");
                        break;
                    }
                }
            }

            sw.Stop();

            var summary = new SuiteExecutionResult
            {
                BatchId = options.BatchId,
                SuiteId = suite.Id,
                TotalSteps = stepResults.Count,
                PassedSteps = stepResults.Count(x => x.Status == StepStatus.Passed),
                FailedSteps = stepResults.Count(x => x.Status == StepStatus.Failed),
                SkippedSteps = stepResults.Count(x => x.Status == StepStatus.Skipped),
                ErrorSteps = stepResults.Count(x => x.Status == StepStatus.Error),
                TotalDurationMs = sw.ElapsedMilliseconds,
                StepResults = stepResults
            };

            _logger.LogInformation("Suite Completed Pass%={PassPercentage}",
                summary.PassPercentage);

            return summary;
        }

        // ─────────────────────────────────────────────
        // STEP EXECUTION
        // ─────────────────────────────────────────────
        private async Task<StepExecutionResult> ExecuteStepAsync(
            TestStep step,
            Dictionary<string, string> context,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var url = ResolveVariables(step.Url, context);
                var body = step.RequestBody != null
                    ? ResolveVariables(step.RequestBody, context)
                    : null;

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(step.TimeoutMs);

                var request = new HttpRequestMessage(
new System.Net.Http.HttpMethod(step.Method.ToString()),
                    url);

                foreach (var h in step.RequestHeaders)
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);

                if (body != null && step.Method != QAsist.Domain.Enums.HttpMethod.GET)
                {
                    request.Content = new StringContent(
                        body, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                var assertions = RunAssertions(step, response, responseBody, sw.ElapsedMilliseconds);
                var extracted = ExtractVariables(step, response, responseBody);

                return new StepExecutionResult
                {
                    TestCaseId = step.TestCaseId,
                    TestStepId = step.Id,
                    Status = assertions.All(a => a.Passed)
                        ? StepStatus.Passed
                        : StepStatus.Failed,
                    DurationMs = sw.ElapsedMilliseconds,
                    AssertionResults = assertions,
                    ExtractedVariables = extracted,
                    RequestLog = new RequestLog
                    {
                        Method = step.Method.ToString(),
                        Url = url,
                        Headers = step.RequestHeaders,
                        Body = body
                    },
                    ResponseLog = new ResponseLog
                    {
                        StatusCode = (int)response.StatusCode,
                        Body = responseBody,
                        DurationMs = sw.ElapsedMilliseconds
                    }
                };
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(ex, "Step failed");

                return new StepExecutionResult
                {
                    TestStepId = step.Id,
                    Status = StepStatus.Error,
                    DurationMs = sw.ElapsedMilliseconds,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ─────────────────────────────────────────────
        // ASSERTIONS (FIXED FOR YOUR ENUM)
        // ─────────────────────────────────────────────
        private List<AssertionResult> RunAssertions(
            TestStep step,
            HttpResponseMessage response,
            string body,
            long duration)
        {
            var results = new List<AssertionResult>();

            foreach (var a in step.Assertions)
            {
                switch (a.AssertionType)
                {
                    case AssertionType.StatusCodeEquals:
                        results.Add(
                            ((int)response.StatusCode).ToString() == a.ExpectedValue
                            ? AssertionResult.Success(Guid.NewGuid(), a.AssertionType, "Status OK")
                            : AssertionResult.Failure(Guid.NewGuid(), a.AssertionType, "Status mismatch")
                        );
                        break;

                    case AssertionType.ResponseTimeLessThan:
                        if (long.TryParse(a.ExpectedValue, out var max))
                        {
                            results.Add(duration <= max
                                ? AssertionResult.Success(Guid.NewGuid(), a.AssertionType, "Time OK")
                                : AssertionResult.Failure(Guid.NewGuid(), a.AssertionType, "Time exceeded"));
                        }
                        break;

                    case AssertionType.BodyContains:
                        results.Add(body.Contains(a.ExpectedValue ?? "")
                            ? AssertionResult.Success(Guid.NewGuid(), a.AssertionType, "Body contains OK")
                            : AssertionResult.Failure(Guid.NewGuid(), a.AssertionType, "Body missing value"));
                        break;
                }
            }

            if (!results.Any())
            {
                results.Add(AssertionResult.Success(
                    Guid.NewGuid(),
                    AssertionType.StatusCodeEquals,
                    "No assertions → treated as pass"));
            }

            return results;
        }

        // ─────────────────────────────────────────────
        // VARIABLE EXTRACTION
        // ─────────────────────────────────────────────
        private Dictionary<string, string> ExtractVariables(
     TestStep step,
     HttpResponseMessage response,
     string responseBody)
        {
            var result = new Dictionary<string, string>();

            if (step.Extractions == null || !step.Extractions.Any())
                return result;

            JsonDocument? doc = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(responseBody))
                    doc = JsonDocument.Parse(responseBody);
            }
            catch
            {
                // ignore invalid JSON
            }

            foreach (var e in step.Extractions)
            {
                string value = string.Empty;

                try
                {
                    switch (e.Source)
                    {
                        // ───────── BODY EXTRACTION ─────────
                        case ExtractionSource.Body:
                            if (doc != null && !string.IsNullOrWhiteSpace(e.JsonPath))
                            {
                                value = GetJsonValue(doc.RootElement, e.JsonPath)
                                        ?? e.DefaultValue
                                        ?? string.Empty;
                            }
                            break;

                        // ───────── HEADER EXTRACTION ─────────
                        case ExtractionSource.Header:
                            if (!string.IsNullOrWhiteSpace(e.HeaderName))
                            {
                                if (response.Headers.TryGetValues(e.HeaderName, out var values) ||
                                    response.Content.Headers.TryGetValues(e.HeaderName, out values))
                                {
                                    value = values.FirstOrDefault() ?? "";
                                }
                                else
                                {
                                    value = e.DefaultValue ?? "";
                                }
                            }
                            break;
                    }
                }
                catch
                {
                    value = e.DefaultValue ?? "";
                }

                result[e.VariableName] = value;

                _logger.LogInformation(
                    "Extracted variable {Var} = {Value}",
                    e.VariableName, value);
            }

            return result;
        }

        private static string? GetJsonValue(JsonElement root, string jsonPath)
        {
            try
            {
                // Remove "$." if present
                var path = jsonPath.Replace("$.", "");
                var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

                var current = root;

                foreach (var part in parts)
                {
                    if (current.ValueKind == JsonValueKind.Object &&
                        current.TryGetProperty(part, out var next))
                    {
                        current = next;
                    }
                    else
                    {
                        return null;
                    }
                }

                return current.ValueKind switch
                {
                    JsonValueKind.String => current.GetString(),
                    JsonValueKind.Number => current.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => current.GetRawText()
                };
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────
        private static string ResolveVariables(string input, Dictionary<string, string> context)
        {
            return Regex.Replace(input, @"\{\{(\w+)\}\}", m =>
            {
                var key = m.Groups[1].Value;
                return context.ContainsKey(key) ? context[key] : m.Value;
            });
        }
    }
}