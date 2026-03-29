using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QAsist.Application.Execution;
using QAsist.Application.Execution.Assertions;
using QAsist.Application.Execution.Assertions.Assertors;
using QAsist.Application.Execution.Auth;
using QAsist.Application.Execution.Extractions;
using QAsist.Application.Execution.Http;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using QAsist.Domain.ValueObjects;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using ExecutionContext = QAsist.Application.Execution.ExecutionContext;
using HttpMethod = QAsist.Domain.Enums.HttpMethod;

namespace QAsist.Tests.src.QAsist.Tests.Unit.Execution
{
    /// <summary>
    /// Uses WireMock.Net to mock HTTP calls (no real network).
    /// Run with: dotnet test --filter "StepExecutorTests"
    /// </summary>
    public class StepExecutorTests : IDisposable
    {
        private readonly WireMockServer _server;
        private readonly StepExecutor _executor;
        private readonly SuiteExecutor _suiteExecutor;

        public StepExecutorTests()
        {
            // Start WireMock local server on random port
            _server = WireMockServer.Start();

            // Build all real dependencies (no mocks — integration-style unit test)
            var loggerFactory = new NullLoggerFactory();
            var resolver = new VariableResolver(NullLogger<VariableResolver>.Instance);
            var authInjector = new AuthInjector(resolver, NullLogger<AuthInjector>.Instance);

            var assertionEngine = new AssertionEngine(
                new IAssertor[]
                {
                    new StatusCodeAssertor(),
                    new ResponseTimeAssertor(),
                    new BodyContainsAssertor(),
                    new FieldEqualsAssertor(),
                    new FieldExistsAssertor(),
                    new FieldNotExistsAssertor()
                },
                NullLogger<AssertionEngine>.Instance);

            var extractionEngine = new ExtractionEngine(
                new IExtractor[]
                {
                    new BodyExtractor(),
                    new HeaderExtractor(),
                    new StatusCodeExtractor()
                },
                NullLogger<ExtractionEngine>.Instance);

            // Use a real HttpClient pointed at WireMock server
            var httpClientFactory = new TestHttpClientFactory(_server.Urls[0]);

            var requestBuilder = new RequestBuilder(
                authInjector,
                NullLogger<RequestBuilder>.Instance);

            var responseReader = new ResponseReader(
                NullLogger<ResponseReader>.Instance);

            _executor = new StepExecutor(
                resolver, requestBuilder, responseReader,
                assertionEngine, extractionEngine, authInjector,
                httpClientFactory, NullLogger<StepExecutor>.Instance);

            _suiteExecutor = new SuiteExecutor(
    httpClientFactory,
    NullLogger<SuiteExecutor>.Instance);
        }

        public void Dispose() => _server.Stop();

        // ── Helpers ───────────────────────────────────────────────────────────
        private ExecutionContext CreateContext(
            Dictionary<string, string>? vars = null)
        {
            var ctx = new ExecutionContext(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), NullLogger.Instance);
            if (vars != null)
                foreach (var (k, v) in vars)
                    ctx.SetVariable(k, v);
            return ctx;
        }

        private TestStep MakeStep(
            string method, string path,
            List<Assertion>? assertions = null,
            List<Extraction>? extractions = null,
            AuthConfig? authConfig = null) => new()
            {
                Id = Guid.NewGuid(),
                TestCaseId = Guid.NewGuid(),
                Name = $"Test {method} {path}",
                Method = Enum.Parse<HttpMethod>(method, true),
                Url = _server.Urls[0] + path,
                RequestHeaders = new Dictionary<string, string>
                { { "Content-Type", "application/json" } },
                TimeoutMs = 5000,
                IsEnabled = true,
                Assertions = assertions ?? new(),
                Extractions = extractions ?? new(),
                AuthConfig = authConfig,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        private static Assertion MakeAssertion(
            AssertionType type, string? expected = null, string? field = null) => new()
            {
                Id = Guid.NewGuid(),
                TestStepId = Guid.NewGuid(),
                AssertionType = type,
                ExpectedValue = expected,
                Field = field,
                IsRequired = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        private static Extraction MakeExtraction(
            string varName, ExtractionSource source,
            string? jsonPath = null, string? headerName = null) => new()
            {
                Id = Guid.NewGuid(),
                TestStepId = Guid.NewGuid(),
                VariableName = varName,
                Source = source,
                JsonPath = jsonPath,
                HeaderName = headerName,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        // ═══════════════════════════════════════════════════════════════════
        // StepExecutor Tests
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Step_Get_Returns200_Passes()
        {
            _server.Given(Request.Create().WithPath("/ping").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("{\"status\":\"ok\"}"));

            var step = MakeStep("GET", "/ping",
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "200"),
                    MakeAssertion(AssertionType.BodyContains, "ok")
                });

            var result = await _executor.ExecuteAsync(step, CreateContext());

            result.Status.Should().Be(StepStatus.Passed);
            result.AssertionResults.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
        }

        [Fact]
        public async Task Step_Post_WithBody_Returns201_Passes()
        {
            _server.Given(Request.Create().WithPath("/users").UsingPost())
                   .RespondWith(Response.Create()
                       .WithStatusCode(201)
                       .WithBody("{\"data\":{\"id\":\"new-123\"}}"));

            var step = MakeStep("POST", "/users",
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "201"),
                    MakeAssertion(AssertionType.FieldExists, field: "$.data.id")
                });
            step.RequestBody = "{\"name\":\"Alice\"}";

            var result = await _executor.ExecuteAsync(step, CreateContext());

            result.Status.Should().Be(StepStatus.Passed);
            var requestLog = result.RequestLog as RequestLog;

            requestLog.Should().NotBeNull();
            requestLog!.Body.Should().Be("{\"name\":\"Alice\"}");
        }

        [Fact]
        public async Task Step_AssertionFails_Returns_FailedStatus()
        {
            _server.Given(Request.Create().WithPath("/not-found").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(404));

            var step = MakeStep("GET", "/not-found",
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "200") // expects 200, gets 404
                });

            var result = await _executor.ExecuteAsync(step, CreateContext());

            result.Status.Should().Be(StepStatus.Failed);
            result.AssertionResults[0].Passed.Should().BeFalse();
            result.AssertionResults[0].ActualValue.Should().Be("404");
        }

        [Fact]
        public async Task Step_ExtractsVariable_StoresInContext()
        {
            _server.Given(Request.Create().WithPath("/login").UsingPost())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("{\"data\":{\"token\":\"eyJhbG...\"}}"));

            var step = MakeStep("POST", "/login",
                extractions: new()
                {
                    MakeExtraction("accessToken", ExtractionSource.Body, "$.data.token")
                });

            var ctx = CreateContext();
            await _executor.ExecuteAsync(step, ctx);

            ctx.GetVariable("accessToken").Should().Be("eyJhbG...");
        }

        [Fact]
        public async Task Step_VariablesInUrl_ResolvedBeforeSending()
        {
            _server.Given(Request.Create().WithPath("/users/abc-123").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(200)
                       .WithBody("{\"id\":\"abc-123\"}"));

            // Step URL has {{userId}} which will be resolved from context
            var step = MakeStep("GET", "/users/placeholder");
            step.Url = _server.Urls[0] + "/users/{{userId}}";

            var ctx = CreateContext(new() { { "userId", "abc-123" } });
            var result = await _executor.ExecuteAsync(step, ctx);

            result.Status.Should().Be(StepStatus.Passed);
            var requestLog = result.RequestLog as RequestLog;
            requestLog!.Url.Should().Contain("abc-123");
        }

        [Fact]
        public async Task Step_BearerAuth_InjectsAuthorizationHeader_Redacted_InLog()
        {
            _server.Given(Request.Create().WithPath("/protected").UsingGet()
                       .WithHeader("Authorization", "Bearer secret-token"))
                   .RespondWith(Response.Create().WithStatusCode(200));

            var step = MakeStep("GET", "/protected",
                authConfig: AuthConfig.Bearer("{{token}}"));

            var ctx = CreateContext(new() { { "token", "secret-token" } });
            var result = await _executor.ExecuteAsync(step, ctx);

            // Auth header injected correctly (WireMock validates it)
            result.Status.Should().Be(StepStatus.Passed);

            // Auth header REDACTED in log
            var requestLog = result.RequestLog as RequestLog;

            requestLog!.Headers
                .Should().ContainKey("Authorization")
                .WhoseValue.Should().Be("[REDACTED]");
        }

        [Fact]
        public async Task Step_Disabled_ReturnsSkipped()
        {
            var step = MakeStep("GET", "/anything");
            step.IsEnabled = false;

            var result = await _executor.ExecuteAsync(step, CreateContext());

            result.Status.Should().Be(StepStatus.Skipped);
        }

        [Fact]
        public async Task Step_Timeout_ReturnsTimedOut()
        {
            _server.Given(Request.Create().WithPath("/slow").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithDelay(TimeSpan.FromSeconds(5))); // 5s delay

            var step = MakeStep("GET", "/slow");
            step.TimeoutMs = 100; // 100ms timeout → will timeout

            var result = await _executor.ExecuteAsync(step, CreateContext());

            result.Status.Should().Be(StepStatus.TimedOut);
        }

        // ═══════════════════════════════════════════════════════════════════
        // SuiteExecutor Tests (Week 7)
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Suite_MultiStep_ChainedVariables_EndToEnd()
        {
            // Simulates real login → use token flow
            // Step 1: POST /auth/login → extract token
            // Step 2: GET /profile (with {{token}} in header) → assert success

            _server.Given(Request.Create().WithPath("/auth/login").UsingPost())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("{\"data\":{\"token\":\"my-jwt-token\",\"userId\":\"u-001\"}}"));

            _server.Given(Request.Create()
                       .WithPath("/profile")
                       .UsingGet()
                       .WithHeader("Authorization", "Bearer my-jwt-token"))
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("{\"data\":{\"name\":\"Alice\"}}"));

            var suite = BuildLoginSuite();
            var options = new ExecutionOptions
            {
                TenantId = Guid.NewGuid(),
                EnvironmentId = Guid.NewGuid(),
                FailFast = false,
                EnvironmentVariables = new()
                {
                    { "baseUrl", _server.Urls[0] }
                }
            };

            var result = await _suiteExecutor.ExecuteAsync(suite, options);

            result.IsSuccess.Should().BeTrue();
            result.PassedSteps.Should().Be(2);
            result.FailedSteps.Should().Be(0);
        }

        [Fact]
        public async Task Suite_FailFast_AbortsOnFirstFailure()
        {
            _server.Given(Request.Create().WithPath("/step1").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(500)); // will FAIL

            _server.Given(Request.Create().WithPath("/step2").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(200)); // should NOT run

            var suite = BuildTwoStepSuite("/step1", "/step2");
            var options = new ExecutionOptions
            {
                TenantId = Guid.NewGuid(),
                EnvironmentId = Guid.NewGuid(),
                FailFast = true  // abort on first failure
            };

            var result = await _suiteExecutor.ExecuteAsync(suite, options);

            result.FailedSteps.Should().Be(1);
            result.SkippedSteps.Should().Be(1);  // step 2 skipped
        }

        // ── Suite builders ────────────────────────────────────────────────────
        private TestSuite BuildLoginSuite()
        {
            var step1 = MakeStep("POST", "/auth/login",
                extractions: new()
                {
                    MakeExtraction("token",  ExtractionSource.Body, "$.data.token"),
                    MakeExtraction("userId", ExtractionSource.Body, "$.data.userId")
                });
            step1.Url = _server.Urls[0] + "/auth/login";

            var step2 = MakeStep("GET", "/profile",
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "200"),
                    MakeAssertion(AssertionType.FieldExists, field: "$.data.name")
                },
                authConfig: AuthConfig.Bearer("{{token}}")); // uses extracted token
            step2.Url = _server.Urls[0] + "/profile";

            var testCase = new TestCaseSuite
            {
                Id = Guid.NewGuid(),
                TestSuiteId = Guid.NewGuid(),
                Name = "Login Flow",
                OrderIndex = 0,
                IsEnabled = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Steps = new List<TestStep> { step1, step2 }
            };

            return new TestSuite
            {
                Id = testCase.TestSuiteId,
                ProjectId = Guid.NewGuid(),
                Name = "Auth Suite",
                IsActive = true,
                Variables = new(),
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                TestCases = new List<TestCaseSuite> { testCase }
            };
        }

        private TestSuite BuildTwoStepSuite(string path1, string path2)
        {
            var step1 = MakeStep("GET", path1,
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "200")
                });

            var step2 = MakeStep("GET", path2,
                assertions: new()
                {
                    MakeAssertion(AssertionType.StatusCodeEquals, "200")
                });

            var testCase = new TestCaseSuite
            {
                Id = Guid.NewGuid(),
                TestSuiteId = Guid.NewGuid(),
                Name = "Two Step Case",
                OrderIndex = 0,
                IsEnabled = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Steps = new List<TestStep> { step1, step2 }
            };

            return new TestSuite
            {
                Id = testCase.TestSuiteId,
                ProjectId = Guid.NewGuid(),
                Name = "FailFast Suite",
                IsActive = true,
                Variables = new(),
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                TestCases = new List<TestCaseSuite> { testCase }
            };
        }
    }

    // ── TestHttpClientFactory — wraps a real HttpClient for tests ─────────────
    internal class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public HttpClient CreateClient(string name) => _client;
    }
}
