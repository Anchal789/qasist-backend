using FluentAssertions;
using QAsist.Application.Execution.Assertions;
using QAsist.Application.Execution.Assertions.Assertors;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Enums;
using Xunit;

namespace QAsist.Tests.src.QAsist.Tests.QAsist.Tests.Unit.Assertions
{
    /// <summary>
    /// Week 4 unit tests — FieldEquals, FieldExists, FieldNotExists,
    /// FieldMatchesRegex assertors + JSONPath edge cases.
    /// Run with: dotnet test --filter "AssertionEnginePartTwoTests"
    /// </summary>
    public class AssertionEnginePartTwoTests
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

        private static HttpStepResponse MakeResponse(string body) => new()
        {
            StatusCode = 200,
            Body = body,
            DurationMs = 100,
            Headers = new Dictionary<string, string>()
        };

        // ═══════════════════════════════════════════════════════════════════
        // FieldEqualsAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly FieldEqualsAssertor _fieldEquals = new();

        [Fact]
        public void FieldEquals_MatchingValue_Passes()
        {
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.data.id", expectedValue: "42");
            var response = MakeResponse("{\"data\":{\"id\":\"42\"}}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
            result.ActualValue.Should().Be("42");
        }

        [Fact]
        public void FieldEquals_NonMatchingValue_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.data.id", expectedValue: "99");
            var response = MakeResponse("{\"data\":{\"id\":\"42\"}}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.ActualValue.Should().Be("42");
            result.ExpectedValue.Should().Be("99");
        }

        [Fact]
        public void FieldEquals_InvalidJsonPath_Fails_WithDescriptiveMessage()
        {
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "INVALID[[[", expectedValue: "42");
            var response = MakeResponse("{\"data\":{}}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.Message.Should().NotBeEmpty();
        }

        [Fact]
        public void FieldEquals_InvalidJsonBody_Fails_WithoutThrowing()
        {
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.data.id", expectedValue: "42");
            var response = MakeResponse("NOT JSON AT ALL");

            var act = () => _fieldEquals.Evaluate(assertion, response, 100);
            var result = _fieldEquals.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("not valid JSON");
        }

        [Fact]
        public void FieldEquals_PathNotFound_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.data.missing", expectedValue: "42");
            var response = MakeResponse("{\"data\":{}}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public void FieldEquals_ArrayIndexPath_Passes()
        {
            // Access first element of array
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.items[0].id", expectedValue: "1");
            var response = MakeResponse("{\"items\":[{\"id\":\"1\"},{\"id\":\"2\"}]}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void FieldEquals_IntegerValue_ComparesAsString()
        {
            // DB returns integer 42, expected "42" — should match
            var assertion = MakeAssertion(AssertionType.FieldEquals,
                field: "$.count", expectedValue: "42");
            var response = MakeResponse("{\"count\":42}");

            var result = _fieldEquals.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════════════════
        // FieldExistsAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly FieldExistsAssertor _fieldExists = new();

        [Fact]
        public void FieldExists_FieldPresent_Passes()
        {
            var assertion = MakeAssertion(AssertionType.FieldExists,
                field: "$.data.token");
            var response = MakeResponse("{\"data\":{\"token\":\"abc123\"}}");

            var result = _fieldExists.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void FieldExists_FieldMissing_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldExists,
                field: "$.data.token");
            var response = MakeResponse("{\"data\":{}}");

            var result = _fieldExists.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void FieldExists_FieldPresentWithNullValue_Passes()
        {
            // Field EXISTS even if its value is null
            var assertion = MakeAssertion(AssertionType.FieldExists,
                field: "$.data.token");
            var response = MakeResponse("{\"data\":{\"token\":null}}");

            var result = _fieldExists.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue("field exists even with null value");
        }

        [Fact]
        public void FieldExists_InvalidJson_Fails_WithoutThrowing()
        {
            var assertion = MakeAssertion(AssertionType.FieldExists,
                field: "$.data.token");
            var response = MakeResponse("INVALID JSON");

            var act = () => _fieldExists.Evaluate(assertion, response, 100);
            var result = _fieldExists.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            result.Passed.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════
        // FieldNotExistsAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly FieldNotExistsAssertor _fieldNotExists = new();

        [Fact]
        public void FieldNotExists_FieldAbsent_Passes()
        {
            var assertion = MakeAssertion(AssertionType.FieldNotExists,
                field: "$.error");
            var response = MakeResponse("{\"data\":{\"id\":\"1\"}}");

            var result = _fieldNotExists.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void FieldNotExists_FieldPresent_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldNotExists,
                field: "$.error");
            var response = MakeResponse("{\"error\":\"not found\"}");

            var result = _fieldNotExists.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
            result.ActualValue.Should().Be("not found");
        }

        [Fact]
        public void FieldNotExists_InvalidJson_Fails_WithoutThrowing()
        {
            var assertion = MakeAssertion(AssertionType.FieldNotExists,
                field: "$.error");
            var response = MakeResponse("BAD JSON");

            var act = () => _fieldNotExists.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            _fieldNotExists.Evaluate(assertion, response, 100)
                .Passed.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════
        // FieldMatchesRegexAssertor Tests
        // ═══════════════════════════════════════════════════════════════════
        private readonly FieldMatchesRegexAssertor _fieldRegex = new();

        [Fact]
        public void FieldMatchesRegex_ValidEmailPattern_Passes()
        {
            var assertion = MakeAssertion(AssertionType.FieldMatchesRegex,
                field: "$.data.email",
                expectedValue: @"^[^@]+@[^@]+\.[^@]+$");
            var response = MakeResponse("{\"data\":{\"email\":\"test@test.com\"}}");

            var result = _fieldRegex.Evaluate(assertion, response, 100);

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void FieldMatchesRegex_PatternDoesNotMatch_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldMatchesRegex,
                field: "$.data.email",
                expectedValue: @"^[^@]+@[^@]+\.[^@]+$");
            var response = MakeResponse("{\"data\":{\"email\":\"notanemail\"}}");

            var result = _fieldRegex.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
        }

        [Fact]
        public void FieldMatchesRegex_InvalidRegexPattern_Fails_WithDescriptiveMessage()
        {
            var assertion = MakeAssertion(AssertionType.FieldMatchesRegex,
                field: "$.data.id",
                expectedValue: "[[[invalid regex");
            var response = MakeResponse("{\"data\":{\"id\":\"abc\"}}");

            var act = () => _fieldRegex.Evaluate(assertion, response, 100);
            var result = _fieldRegex.Evaluate(assertion, response, 100);

            act.Should().NotThrow();
            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("Invalid regex");
        }

        [Fact]
        public void FieldMatchesRegex_FieldNotFound_Fails()
        {
            var assertion = MakeAssertion(AssertionType.FieldMatchesRegex,
                field: "$.missing",
                expectedValue: ".*");
            var response = MakeResponse("{\"data\":{}}");

            var result = _fieldRegex.Evaluate(assertion, response, 100);

            result.Passed.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════
        // JsonPathHelper Tests
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void JsonPathHelper_ValidPath_ReturnsToken()
        {
            var token = JsonPathHelper.SelectToken(
                "{\"data\":{\"id\":\"abc\"}}",
                "$.data.id",
                out var error);

            error.Should().BeNull();
            token.Should().NotBeNull();
            JsonPathHelper.TokenToString(token).Should().Be("abc");
        }

        [Fact]
        public void JsonPathHelper_PathNotFound_ReturnsNull_NoError()
        {
            var token = JsonPathHelper.SelectToken(
                "{\"data\":{}}",
                "$.data.missing",
                out var error);

            error.Should().BeNull();     // not an error — just not found
            token.Should().BeNull();
        }

        [Fact]
        public void JsonPathHelper_InvalidJson_ReturnsError()
        {
            var token = JsonPathHelper.SelectToken(
                "NOT JSON",
                "$.data.id",
                out var error);

            error.Should().NotBeNull();
            error.Should().Contain("not valid JSON");
            token.Should().BeNull();
        }

        [Fact]
        public void JsonPathHelper_InvalidPath_ReturnsError()
        {
            var token = JsonPathHelper.SelectToken(
                "{\"data\":{}}",
                "[[[bad path",
                out var error);

            error.Should().NotBeNull();
            token.Should().BeNull();
        }
    }
}
