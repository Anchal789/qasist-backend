using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QAsist.Application.Execution.Assertions;
using QAsist.Application.Execution.Assertions.Assertors;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using Xunit;

namespace QAsist.Tests.src.QAsist.Tests.QAsist.Tests.Unit.Assertions
{
    /// <summary>
    /// Week 3 unit tests — StatusCode, ResponseTime, BodyContains assertors.
    /// All tests must pass before moving to Week 4.
    /// Run with: dotnet test --filter "AssertionEnginePartOneTests"
    /// </summary>
    public class AssertionEnginePartOneTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────
        private static Domain.Entities.Assertion MakeAssertion(
            AssertionType type,
            string? expectedValue = null,
            string? field = null,
            bool isRequired = true) => new()
            {
                Id = Guid.NewGuid(),
                TestStepId = Guid.NewGuid(),
                AssertionType = type,
                ExpectedValue = expectedValue,
                Field = field,
                IsRequired = isRequired,
                OrderIndex = 0,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

        private static HttpStepResponse MakeResponse(
            int statusCode = 200,
            string body = "",
            long durationMs = 100) => new()
            {
                StatusCode = statusCode,
                Body = body,
                Headers = new Dictionary<string, string>(),
                DurationMs = durationMs
            };

        // ═══════════════════════════════════════════════════════════════════
        // StatusCodeAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly StatusCodeAssertor _statusAssertor = new();

        [Fact]
        public void StatusCode_Equals_Expected_Passes()
        {
            var assertion = MakeAssertion(AssertionType.StatusCodeEquals, "200");
            var response = MakeResponse(statusCode: 200);

            var result = _statusAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
            result.ActualValue.Should().Be("200");
        }

        [Fact]
        public void StatusCode_NotEquals_Expected_Fails()
        {
            var assertion = MakeAssertion(AssertionType.StatusCodeEquals, "200");
            var response = MakeResponse(statusCode: 404);

            var result = _statusAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.ActualValue.Should().Be("404");
            result.ExpectedValue.Should().Be("200");
        }

        [Fact]
        public void StatusCode_InvalidExpected_Fails_WithDescriptiveMessage()
        {
            var assertion = MakeAssertion(AssertionType.StatusCodeEquals, "abc");
            var response = MakeResponse(statusCode: 200);

            var result = _statusAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("Invalid");
            result.Message.Should().Contain("abc");
        }

        [Fact]
        public void StatusCode_NullExpected_Fails_WithoutThrowing()
        {
            var assertion = MakeAssertion(AssertionType.StatusCodeEquals, null);
            var response = MakeResponse(statusCode: 200);

            var act = () => _statusAssertor.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            var result = _statusAssertor.Evaluate(assertion, response, 100);
            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void StatusCode_AssertorType_IsStatusCodeEquals()
        {
            _statusAssertor.SupportedType.Should().Be(AssertionType.StatusCodeEquals);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ResponseTimeAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly ResponseTimeAssertor _timeAssertor = new();

        [Fact]
        public void ResponseTime_BelowThreshold_Passes()
        {
            var assertion = MakeAssertion(AssertionType.ResponseTimeLessThan, "500");
            var response = MakeResponse(durationMs: 200);

            var result = _timeAssertor.Evaluate(assertion, response, 200);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ResponseTime_AboveThreshold_Fails()
        {
            var assertion = MakeAssertion(AssertionType.ResponseTimeLessThan, "500");
            var response = MakeResponse(durationMs: 600);

            var result = _timeAssertor.Evaluate(assertion, response, 600);

            result.Passed.Should().BeFalse();
            result.ActualValue.Should().Be("600ms");
        }

        [Fact]
        public void ResponseTime_ExactlyAtThreshold_Fails_StrictlyLessThan()
        {
            // Boundary: 500 is NOT less than 500 → must FAIL
            var assertion = MakeAssertion(AssertionType.ResponseTimeLessThan, "500");
            var response = MakeResponse(durationMs: 500);

            var result = _timeAssertor.Evaluate(assertion, response, 500);

            result.Passed.Should().BeFalse("500ms is not strictly less than 500ms");
        }

        [Fact]
        public void ResponseTime_InvalidThreshold_Fails_WithDescriptiveMessage()
        {
            var assertion = MakeAssertion(AssertionType.ResponseTimeLessThan, "fast");
            var response = MakeResponse(durationMs: 100);

            var result = _timeAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("Invalid");
        }

        // ═══════════════════════════════════════════════════════════════════
        // BodyContainsAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly BodyContainsAssertor _bodyAssertor = new();

        [Fact]
        public void BodyContains_ExpectedSubstring_Passes()
        {
            var assertion = MakeAssertion(AssertionType.BodyContains, "success");
            var response = MakeResponse(body: "{\"status\":\"success\",\"data\":{}}");

            var result = _bodyAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void BodyContains_CaseInsensitive_Passes()
        {
            // "SUCCESS" should match "success" — case-insensitive
            var assertion = MakeAssertion(AssertionType.BodyContains, "SUCCESS");
            var response = MakeResponse(body: "{\"status\":\"success\"}");

            var result = _bodyAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue("comparison is case-insensitive");
        }

        [Fact]
        public void BodyContains_MissingSubstring_Fails()
        {
            var assertion = MakeAssertion(AssertionType.BodyContains, "error");
            var response = MakeResponse(body: "{\"status\":\"success\"}");

            var result = _bodyAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void BodyContains_NullBody_Fails_WithoutThrowing()
        {
            var assertion = MakeAssertion(AssertionType.BodyContains, "success");
            var response = new HttpStepResponse
            {
                StatusCode = 200,
                Body = null!,   // simulate null body
                DurationMs = 100
            };

            var act = () => _bodyAssertor.Evaluate(assertion, response, 100);
            var result = _bodyAssertor.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void BodyContains_EmptyBody_Fails()
        {
            var assertion = MakeAssertion(AssertionType.BodyContains, "success");
            var response = MakeResponse(body: "");

            var result = _bodyAssertor.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════
        // AssertionEngine Tests
        // ═══════════════════════════════════════════════════════════════════
        private AssertionEngine CreateEngine() => new(
            assertors: new IAssertor[]
            {
                new StatusCodeAssertor(),
                new ResponseTimeAssertor(),
                new BodyContainsAssertor()
            },
            logger: NullLogger<AssertionEngine>.Instance);

        [Fact]
        public void Engine_EvaluatesAll_ReturnsAllResults()
        {
            var engine = CreateEngine();
            var assertions = new List<Domain.Entities.Assertion>
            {
                MakeAssertion(AssertionType.StatusCodeEquals,    "200"),
                MakeAssertion(AssertionType.ResponseTimeLessThan,"500"),
                MakeAssertion(AssertionType.BodyContains,        "success")
            };
            var response = MakeResponse(
                statusCode: 200,
                body: "{\"status\":\"success\"}",
                durationMs: 100);

            var results = engine.EvaluateAll(assertions, response, 100);

            results.Should().HaveCount(3);
            results.Should().AllSatisfy(r => r.Passed.Should().BeTrue());
        }

        [Fact]
        public void Engine_OneAssertionFails_ReturnsAllResults_DoesNotAbort()
        {
            var engine = CreateEngine();
            var assertions = new List<Domain.Entities.Assertion>
            {
                MakeAssertion(AssertionType.StatusCodeEquals, "201"), // will FAIL (got 200)
                MakeAssertion(AssertionType.BodyContains,     "ok")   // will PASS
            };
            var response = MakeResponse(statusCode: 200, body: "ok");

            var results = engine.EvaluateAll(assertions, response, 100);

            // Both evaluated — engine never aborts on first failure
            results.Should().HaveCount(2);
            results.Any(r => !r.Passed).Should().BeTrue();
            results.Any(r => r.Passed).Should().BeTrue();
        }

        [Fact]
        public void Engine_EmptyAssertions_ReturnsEmptyList()
        {
            var engine = CreateEngine();
            var response = MakeResponse();

            var results = engine.EvaluateAll(
                new List<Domain.Entities.Assertion>(), response, 100);

            results.Should().BeEmpty();
        }

        [Fact]
        public void Engine_UnknownAssertionType_ReturnsErrorResult_DoesNotThrow()
        {
            var engine = CreateEngine();
            // Use an assertion type not registered in the engine
            var assertions = new List<Domain.Entities.Assertion>
            {
                MakeAssertion((AssertionType)99) // unknown type
            };
            var response = MakeResponse();

            var act = () => engine.EvaluateAll(assertions, response, 100);
            var results = engine.EvaluateAll(assertions, response, 100);

            act.Should().NotThrow();
            results.Should().HaveCount(1);
            results[0].Passed.Should().BeFalse();
        }
    }
}
