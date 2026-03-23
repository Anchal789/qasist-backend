using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Execution;
using Xunit;
using ExecutionContext = QAsist.Application.Execution.ExecutionContext;

namespace QAsist.Tests.src.QAsist.Tests.QAsist.Tests.Unit.Execution
{
    /// <summary>
    /// T21 — VariableResolver unit tests.
    /// ALL 9 tests must pass before moving to Week 3.
    /// Run with: dotnet test --filter "VariableResolverTests"
    /// </summary>
    public class VariableResolverTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────
        private readonly VariableResolver _resolver;

        public VariableResolverTests()
        {
            _resolver = new VariableResolver(
                new NullLogger<VariableResolver>());
        }

        private static ExecutionContext CreateContext(
            Dictionary<string, string>? variables = null)
        {
            var ctx = new ExecutionContext(
                tenantId: Guid.NewGuid(),
                projectId: Guid.NewGuid(),
                suiteId: Guid.NewGuid(),
                environmentId: Guid.NewGuid(),
                batchId: Guid.NewGuid(),
                logger: NullLogger.Instance);

            if (variables != null)
                foreach (var (key, value) in variables)
                    ctx.SetVariable(key, value);

            return ctx;
        }

        // ── Test 1: Simple single variable resolved ───────────────────────────
        [Fact]
        public void Resolve_SimpleVariable_ReplacesCorrectly()
        {
            var ctx = CreateContext(new() { { "name", "World" } });

            var result = _resolver.Resolve("Hello {{name}}", ctx);

            result.Should().Be("Hello World");
        }

        // ── Test 2: Multiple variables all replaced ───────────────────────────
        [Fact]
        public void Resolve_MultipleVariables_ReplacesAll()
        {
            var ctx = CreateContext(new()
            {
                { "baseUrl", "https://api.dev.io" },
                { "userId",  "abc-123" }
            });

            var result = _resolver.Resolve("{{baseUrl}}/users/{{userId}}", ctx);

            result.Should().Be("https://api.dev.io/users/abc-123");
        }

        // ── Test 3: Missing variable → empty string, no exception ─────────────
        [Fact]
        public void Resolve_MissingVariable_ReturnsEmptyStringWithoutThrowing()
        {
            var ctx = CreateContext(); // empty context

            var result = _resolver.Resolve("Hello {{missing}}", ctx);

            result.Should().Be("Hello ");   // replaced with empty string
        }

        // ── Test 4: Nested variable resolves inner first ──────────────────────
        [Fact]
        public void Resolve_NestedVariable_ResolvesInnerFirst()
        {
            // ctx["env"]      = "prod"
            // ctx["key_prod"] = "secret123"
            // template: "{{key_{{env}}}}"
            // Step 1: inner {{env}} → "prod"
            // Step 2: outer {{key_prod}} → "secret123"
            var ctx = CreateContext(new()
            {
                { "env",      "prod"      },
                { "key_prod", "secret123" }
            });

            var result = _resolver.Resolve("{{key_{{env}}}}", ctx);

            result.Should().Be("secret123");
        }

        // ── Test 5: Circular reference → throws CircularVariableException ─────
        [Fact]
        public void Resolve_CircularReference_ThrowsCircularVariableException()
        {
            // a → b → a  (infinite loop)
            var ctx = CreateContext(new()
            {
                { "a", "{{b}}" },
                { "b", "{{a}}" }
            });

            var act = () => _resolver.Resolve("{{a}}", ctx);

            act.Should().Throw<CircularVariableException>()
               .WithMessage("*a*b*");  // message contains both variable names
        }

        // ── Test 6: No placeholders → returns original string unchanged ───────
        [Fact]
        public void Resolve_NoPlaceholders_ReturnsOriginalString()
        {
            var ctx = CreateContext(new() { { "x", "y" } });

            var result = _resolver.Resolve("plain text no vars", ctx);

            result.Should().Be("plain text no vars");
        }

        // ── Test 7: Empty template → returns empty string ─────────────────────
        [Fact]
        public void Resolve_EmptyTemplate_ReturnsEmptyString()
        {
            var ctx = CreateContext();

            var result = _resolver.Resolve("", ctx);

            result.Should().BeEmpty();
        }

        // ── Test 8: Null template → returns empty string, no NullRef ─────────
        [Fact]
        public void Resolve_NullTemplate_ReturnsEmptyStringWithoutThrowing()
        {
            var ctx = CreateContext();

            var act = () => _resolver.Resolve(null, ctx);
            var result = _resolver.Resolve(null, ctx);

            act.Should().NotThrow();
            result.Should().BeEmpty();
        }

        // ── Test 9: URL with special chars → value preserved exactly ─────────
        [Fact]
        public void Resolve_UrlWithSpecialChars_PreservesValueExactly()
        {
            // URL query strings contain & = ? which must not be modified
            var ctx = CreateContext(new()
            {
                { "apiUrl", "https://api.io/search?q=test&page=1&size=20" }
            });

            var result = _resolver.Resolve("{{apiUrl}}", ctx);

            result.Should().Be("https://api.io/search?q=test&page=1&size=20");
        }

        // ── BONUS Test 10: ResolveHeaders resolves all values ─────────────────
        [Fact]
        public void ResolveHeaders_ResolvesAllValues_KeepsKeysUnchanged()
        {
            var ctx = CreateContext(new()
            {
                { "token",    "eyJhbG..." },
                { "tenantId", "tenant-001" }
            });

            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer {{token}}" },
                { "X-Tenant-Id",   "{{tenantId}}"     },
                { "Content-Type",  "application/json" }  // no variable
            };

            var resolved = _resolver.ResolveHeaders(headers, ctx);

            resolved["Authorization"].Should().Be("Bearer eyJhbG...");
            resolved["X-Tenant-Id"].Should().Be("tenant-001");
            resolved["Content-Type"].Should().Be("application/json"); // unchanged
            resolved.Keys.Should().Contain("Authorization");          // key unchanged
        }

        // ── BONUS Test 11: JSON body with variables resolved ──────────────────
        [Fact]
        public void Resolve_JsonBody_ReplacesVariablesInsideJson()
        {
            var ctx = CreateContext(new()
            {
                { "email",    "test@test.com" },
                { "password", "pass123"       }
            });

            var body = "{\"email\": \"{{email}}\", \"password\": \"{{password}}\"}";

            var result = _resolver.Resolve(body, ctx);

            result.Should().Be("{\"email\": \"test@test.com\", \"password\": \"pass123\"}");
        }
    }
}
