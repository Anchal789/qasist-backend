using Microsoft.Extensions.Logging;
using QAsist.Application.Execution.Auth;
using QAsist.Application.Execution.Extractions;
using QAsist.Application.Execution.Http;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using System.Diagnostics;

namespace QAsist.Application.Execution
{
    /// Flow:
    ///   1. Resolve {{variables}} in URL, headers, body, auth
    ///   2. Build HttpRequestMessage (via IRequestBuilder)
    ///   3. Inject auth headers (via AuthInjector)
    ///   4. Send HTTP request (via IHttpClientFactory + Polly)
    ///   5. Read response (via IResponseReader)
    ///   6. Build request/response logs (auth values REDACTED)
    ///   7. Run assertions (via IAssertionEngine)
    ///   8. Extract variables (via IExtractionEngine) — runs even if assertions fail
    ///   9. Return StepExecutionResult
    ///
    /// NEVER throws — all exceptions captured and returned as Error status.
    /// </summary>
    public class StepExecutor : IStepExecutor
    {
        private readonly IVariableResolver _variableResolver;
        private readonly IRequestBuilder _requestBuilder;
        private readonly IResponseReader _responseReader;
        private readonly IAssertionEngine _assertionEngine;
        private readonly IExtractionEngine _extractionEngine;
        private readonly AuthInjector _authInjector;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StepExecutor> _logger;

        // Named HttpClient registered in DI with Polly policies
        private const string HttpClientName = "StepExecutor";

        public StepExecutor(
            IVariableResolver variableResolver,
            IRequestBuilder requestBuilder,
            IResponseReader responseReader,
            IAssertionEngine assertionEngine,
            IExtractionEngine extractionEngine,
            AuthInjector authInjector,
            IHttpClientFactory httpClientFactory,
            ILogger<StepExecutor> logger)
        {
            _variableResolver = variableResolver;
            _requestBuilder = requestBuilder;
            _responseReader = responseReader;
            _assertionEngine = assertionEngine;
            _extractionEngine = extractionEngine;
            _authInjector = authInjector;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<StepExecutionResult> ExecuteAsync(
            TestStep step,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Executing step '{Name}' [{Method} {Url}]",
                step.Name, step.Method, step.Url);

            if (!step.IsEnabled)
            {
                _logger.LogDebug("Step '{Name}' is disabled — skipping.", step.Name);
                return BuildResult(step.Id, StepStatus.Skipped, 0, null, null, new(), new());
            }

            try
            {
                return await ExecuteInternalAsync(step, context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Step '{Name}' was cancelled.", step.Name);
                return BuildResult(step.Id, StepStatus.Skipped, 0, null, null,
                    new(), new(), "Step cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing step '{Name}'.", step.Name);
                return BuildResult(step.Id, StepStatus.Error, 0, null, null,
                    new(), new(), ex.Message);
            }
        }

        // ── Internal execution ────────────────────────────────────────────────
        private async Task<StepExecutionResult> ExecuteInternalAsync(
            TestStep step,
            IExecutionContext context,
            CancellationToken cancellationToken)
        {
            // ── STEP 1: Resolve variables ─────────────────────────────────────
            var resolvedUrl = _variableResolver.Resolve(step.Url, context);

            var resolvedHeaders = _variableResolver.ResolveHeaders(
                step.RequestHeaders, context);

            var resolvedBody = string.IsNullOrEmpty(step.RequestBody)
                ? null
                : _variableResolver.Resolve(step.RequestBody, context);

            // Resolve auth config (step-level only here; env-level merged in SuiteExecutor)
            var authConfig = step.AuthConfig;
            if (authConfig?.Token is not null)
                authConfig = AuthConfig.Bearer(
                    _variableResolver.Resolve(authConfig.Token, context));

            _logger.LogDebug(
                "Resolved URL: {Url} | Headers: {HdrCount} | Body: {HasBody}",
                resolvedUrl, resolvedHeaders.Count, resolvedBody is not null);

            // ── STEP 2: Build request ─────────────────────────────────────────
            var request = _requestBuilder.Build(
                step.Method.ToString(),
                resolvedUrl,
                resolvedHeaders,
                resolvedBody,
                authConfig);

            // ── STEP 3: Inject auth ───────────────────────────────────────────
            _authInjector.Inject(request, authConfig, context);

            // ── STEP 4: Build request log (BEFORE sending — redact auth) ──────
            var requestLog = BuildRequestLog(request, resolvedUrl, resolvedBody);

            // ── STEP 5: Send HTTP request ─────────────────────────────────────
            var sw = Stopwatch.StartNew();
            HttpResponseMessage? httpResponse = null;
            ResponseLog? responseLog = null;
            StepStatus status = StepStatus.Error;

            try
            {
                using var timeoutCts = CancellationTokenSource
                    .CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(step.TimeoutMs));

                var client = _httpClientFactory.CreateClient(HttpClientName);
                httpResponse = await client.SendAsync(request, timeoutCts.Token);
                sw.Stop();

                _logger.LogInformation(
                    "HTTP {Method} {Url} → {Status} in {Ms}ms",
                    step.Method, resolvedUrl,
                    (int)httpResponse.StatusCode, sw.ElapsedMilliseconds);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    "Step '{Name}' timed out after {Ms}ms.", step.Name, step.TimeoutMs);

                return BuildResult(step.Id, StepStatus.TimedOut, sw.ElapsedMilliseconds,
                    requestLog, null, new(), new(),
                    $"Step timed out after {step.TimeoutMs}ms.");
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "HTTP request failed for step '{Name}'.", step.Name);

                return BuildResult(step.Id, StepStatus.Error, sw.ElapsedMilliseconds,
                    requestLog, null, new(), new(), ex.Message);
            }
            finally
            {
                if (!sw.IsRunning) { } // already stopped
                else sw.Stop();
            }

            // ── STEP 6: Read response ─────────────────────────────────────────
            var stepResponse = await _responseReader.ReadAsync(
                httpResponse, sw.ElapsedMilliseconds, cancellationToken);

            responseLog = BuildResponseLog(stepResponse);

            // ── STEP 7: Run assertions ────────────────────────────────────────
            var assertionResults = _assertionEngine.EvaluateAll(
                step.Assertions ?? new List<Assertion>(),
                stepResponse,
                sw.ElapsedMilliseconds);

            // Determine step status from assertion results
            var allRequiredPassed = assertionResults
                .All(r => r.Passed || !r.IsRequired);

            status = allRequiredPassed ? StepStatus.Passed : StepStatus.Failed;

            _logger.LogInformation(
                "Step '{Name}' assertions: {Passed}/{Total} passed → {Status}",
                step.Name,
                assertionResults.Count(r => r.Passed),
                assertionResults.Count,
                status);

            // ── STEP 8: Extract variables (always runs, even on failure) ──────
            var extracted = _extractionEngine.ExtractAll(
                step.Extractions ?? new List<Extraction>(),
                stepResponse,
                context);

            if (extracted.Count > 0)
                _logger.LogDebug(
                    "Extracted {Count} variables: [{Vars}]",
                    extracted.Count,
                    string.Join(", ", extracted.Keys));

            // ── STEP 9: Build and return result ───────────────────────────────
            return BuildResult(
                step.Id, status, sw.ElapsedMilliseconds,
                requestLog, responseLog,
                assertionResults.ToList(),
                extracted);
        }

        // ── Log builders ──────────────────────────────────────────────────────

        private static RequestLog BuildRequestLog(
            HttpRequestMessage request,
            string resolvedUrl,
            string? body)
        {
            var headers = new Dictionary<string, string>();

            foreach (var header in request.Headers)
            {
                // REDACT auth headers
                var value = header.Key.Equals("Authorization",
                    StringComparison.OrdinalIgnoreCase)
                    ? "[REDACTED]"
                    : string.Join(", ", header.Value);

                headers[header.Key] = value;
            }

            return new RequestLog
            {
                Method = request.Method.Method,
                Url = resolvedUrl,
                Headers = headers,
                Body = body,
                SentAt = DateTime.UtcNow
            };
        }

        private static ResponseLog BuildResponseLog(HttpStepResponse response)
        {
            return new ResponseLog
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                Body = response.Body,
                DurationMs = response.DurationMs,
                ReceivedAt = DateTime.UtcNow
            };
        }

        private static StepExecutionResult BuildResult(
            Guid stepId,
            StepStatus status,
            long durationMs,
            RequestLog? requestLog,
            ResponseLog? responseLog,
            List<AssertionResult> assertionResults,
            Dictionary<string, string> extractedVars,
            string? errorMessage = null) => new()
            {
                TestStepId = stepId,
                Status = status,
                DurationMs = durationMs,
                RequestLog = requestLog,
                ResponseLog = responseLog,
                AssertionResults = assertionResults,
                ExtractedVariables = extractedVars,
                ErrorMessage = errorMessage
            };
    }
}
