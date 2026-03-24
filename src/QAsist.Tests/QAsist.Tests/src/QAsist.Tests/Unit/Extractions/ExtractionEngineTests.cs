using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QAsist.Application.Execution;
using QAsist.Application.Execution.Extractions;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;
using Xunit;
using ExecutionContext = QAsist.Application.Execution.ExecutionContext;

namespace QAsist.Tests.src.QAsist.Tests.Unit.Extractions
{
    /// <summary>
    /// Week 5 unit tests — BodyExtractor, HeaderExtractor,
    /// StatusCodeExtractor, ExtractionEngine resilience.
    /// Run with: dotnet test --filter "ExtractionEngineTests"
    /// </summary>
    public class ExtractionEngineTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────
        private static Extraction MakeExtraction(
            ExtractionSource source,
            string variableName,
            string? jsonPath = null,
            string? headerName = null,
            string? defaultValue = null) => new()
            {
                Id = Guid.NewGuid(),
                TestStepId = Guid.NewGuid(),
                VariableName = variableName,
                Source = source,
                JsonPath = jsonPath,
                HeaderName = headerName,
                DefaultValue = defaultValue,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        private static HttpStepResponse MakeResponse(
            string body = "{}",
            int statusCode = 200,
            Dictionary<string, string>? headers = null) => new()
            {
                StatusCode = statusCode,
                Body = body,
                Headers = headers ?? new Dictionary<string, string>(),
                DurationMs = 100
            };

        private static ExecutionContext CreateContext() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), NullLogger.Instance);

        private ExtractionEngine CreateEngine() => new(
            extractors: new IExtractor[]
            {
                new BodyExtractor(),
                new HeaderExtractor(),
                new StatusCodeExtractor()
            },
            logger: NullLogger<ExtractionEngine>.Instance);

        // ═══════════════════════════════════════════════════════════════════
        // BodyExtractor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly BodyExtractor _bodyExtractor = new();

        [Fact]
        public void BodyExtractor_ValidJsonPath_ExtractsValue()
        {
            var extraction = MakeExtraction(ExtractionSource.Body,
                "userId", jsonPath: "$.data.id");
            var response = MakeResponse("{\"data\":{\"id\":\"abc-123\"}}");

            var value = _bodyExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("abc-123");
        }

        [Fact]
        public void BodyExtractor_MissingPath_ReturnsNullWithError()
        {
            var extraction = MakeExtraction(ExtractionSource.Body,
                "userId", jsonPath: "$.missing.path");
            var response = MakeResponse("{\"data\":{}}");

            var value = _bodyExtractor.Extract(extraction, response, out var error);

            value.Should().BeNull();
            error.Should().NotBeNull();
            error.Should().Contain("no result");
        }

        [Fact]
        public void BodyExtractor_InvalidJson_ReturnsNullWithError()
        {
            var extraction = MakeExtraction(ExtractionSource.Body,
                "userId", jsonPath: "$.data.id");
            var response = MakeResponse("NOT JSON");

            var act = () => _bodyExtractor.Extract(extraction, response, out _);
            var value = _bodyExtractor.Extract(extraction, response, out var error);

            act.Should().NotThrow();
            value.Should().BeNull();
            error.Should().NotBeNull();
        }

        [Fact]
        public void BodyExtractor_ArrayPath_ReturnsFirstElement()
        {
            var extraction = MakeExtraction(ExtractionSource.Body,
                "firstId", jsonPath: "$.items[0].id");
            var response = MakeResponse("{\"items\":[{\"id\":\"1\"},{\"id\":\"2\"}]}");

            var value = _bodyExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("1");
        }

        [Fact]
        public void BodyExtractor_IntegerField_ReturnsAsString()
        {
            var extraction = MakeExtraction(ExtractionSource.Body,
                "count", jsonPath: "$.total");
            var response = MakeResponse("{\"total\":42}");

            var value = _bodyExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("42");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HeaderExtractor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly HeaderExtractor _headerExtractor = new();

        [Fact]
        public void HeaderExtractor_ExistingHeader_ExtractsValue()
        {
            var extraction = MakeExtraction(ExtractionSource.Header,
                "authToken", headerName: "X-Auth-Token");
            var response = MakeResponse(headers: new()
            {
                { "X-Auth-Token", "abc123" },
                { "Content-Type", "application/json" }
            });

            var value = _headerExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("abc123");
        }

        [Fact]
        public void HeaderExtractor_CaseInsensitive_ExtractsValue()
        {
            // Header stored as lowercase, extraction uses PascalCase
            var extraction = MakeExtraction(ExtractionSource.Header,
                "authToken", headerName: "X-AUTH-TOKEN");
            var response = MakeResponse(headers: new()
            {
                { "x-auth-token", "abc123" }
            });

            var value = _headerExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("abc123");
        }

        [Fact]
        public void HeaderExtractor_MissingHeader_ReturnsNullWithError()
        {
            var extraction = MakeExtraction(ExtractionSource.Header,
                "token", headerName: "X-Missing-Header");
            var response = MakeResponse(headers: new()
            {
                { "Content-Type", "application/json" }
            });

            var value = _headerExtractor.Extract(extraction, response, out var error);

            value.Should().BeNull();
            error.Should().NotBeNull();
        }

        [Fact]
        public void HeaderExtractor_NullHeaderName_ReturnsError()
        {
            var extraction = MakeExtraction(ExtractionSource.Header,
                "token", headerName: null);
            var response = MakeResponse();

            var act = () => _headerExtractor.Extract(extraction, response, out _);
            var value = _headerExtractor.Extract(extraction, response, out var error);

            act.Should().NotThrow();
            value.Should().BeNull();
            error.Should().NotBeNull();
        }

        // ═══════════════════════════════════════════════════════════════════
        // StatusCodeExtractor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly StatusCodeExtractor _statusExtractor = new();

        [Fact]
        public void StatusCodeExtractor_Always_ReturnsStatusCodeAsString()
        {
            var extraction = MakeExtraction(ExtractionSource.StatusCode, "loginStatus");
            var response = MakeResponse(statusCode: 201);

            var value = _statusExtractor.Extract(extraction, response, out var error);

            error.Should().BeNull();
            value.Should().Be("201");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExtractionEngine Tests (Resilience T36)
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void Engine_SuccessfulExtraction_StoresInContext()
        {
            var engine = CreateEngine();
            var ctx = CreateContext();
            var extractions = new List<Extraction>
            {
                MakeExtraction(ExtractionSource.Body, "userId", jsonPath: "$.data.id")
            };
            var response = MakeResponse("{\"data\":{\"id\":\"abc-123\"}}");

            engine.ExtractAll(extractions, response, ctx);

            ctx.GetVariable("userId").Should().Be("abc-123");
        }

        [Fact]
        public void Engine_OneExtractionFails_OthersSucceed_Resilient()
        {
            // T36 — CRITICAL: one failure must not abort the rest
            var engine = CreateEngine();
            var ctx = CreateContext();
            var extractions = new List<Extraction>
            {
                // First: FAILS (bad path)
                MakeExtraction(ExtractionSource.Body, "badVar",
                    jsonPath: "$.missing.path"),
                // Second: SUCCEEDS
                MakeExtraction(ExtractionSource.Body, "goodVar",
                    jsonPath: "$.data.name")
            };
            var response = MakeResponse("{\"data\":{\"name\":\"Alice\"}}");

            var act = () => engine.ExtractAll(extractions, response, ctx);

            act.Should().NotThrow();
            ctx.GetVariable("goodVar").Should().Be("Alice");
        }

        [Fact]
        public void Engine_FailedExtraction_UsesDefaultValue()
        {
            var engine = CreateEngine();
            var ctx = CreateContext();
            var extractions = new List<Extraction>
            {
                MakeExtraction(ExtractionSource.Body, "role",
                    jsonPath: "$.missing.role",
                    defaultValue: "guest")          // default when path not found
            };
            var response = MakeResponse("{\"data\":{}}");

            engine.ExtractAll(extractions, response, ctx);

            ctx.GetVariable("role").Should().Be("guest");
        }

        [Fact]
        public void Engine_ExtractAll_ReturnsExtractedDictionary()
        {
            var engine = CreateEngine();
            var ctx = CreateContext();
            var extractions = new List<Extraction>
            {
                MakeExtraction(ExtractionSource.Body, "token",
                    jsonPath: "$.token"),
                MakeExtraction(ExtractionSource.StatusCode, "status")
            };
            var response = MakeResponse(
                "{\"token\":\"xyz789\"}", statusCode: 200);

            var result = engine.ExtractAll(extractions, response, ctx);

            result.Should().ContainKey("token").WhoseValue.Should().Be("xyz789");
            result.Should().ContainKey("status").WhoseValue.Should().Be("200");
        }

        [Fact]
        public void Engine_EmptyExtractions_ReturnsEmptyDictionary()
        {
            var engine = CreateEngine();
            var ctx = CreateContext();
            var response = MakeResponse();

            var result = engine.ExtractAll(
                new List<Extraction>(), response, ctx);

            result.Should().BeEmpty();
        }

        [Fact]
        public void Engine_ExtractedVariable_AvailableForNextStep()
        {
            // Simulates: Step 1 extracts token → Step 2 uses {{token}}
            var engine = CreateEngine();
            var ctx = CreateContext();
            var resolver = new VariableResolver(NullLogger<VariableResolver>.Instance);

            // Step 1: extract token
            var extractions = new List<Extraction>
            {
                MakeExtraction(ExtractionSource.Body, "token",
                    jsonPath: "$.data.token")
            };
            engine.ExtractAll(extractions,
                MakeResponse("{\"data\":{\"token\":\"eyJhbG...\"}}"), ctx);

            // Step 2: use token in header via VariableResolver
            var resolved = resolver.Resolve(
                "Authorization: Bearer {{token}}", ctx);

            resolved.Should().Be("Authorization: Bearer eyJhbG...");
        }
    }
}
