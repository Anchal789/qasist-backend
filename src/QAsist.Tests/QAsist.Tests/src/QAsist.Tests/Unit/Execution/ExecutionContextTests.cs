using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ExecutionContext = QAsist.Application.Execution.ExecutionContext;

namespace QAsist.Tests.Unit.Execution
{
    /// <summary>
    /// Week 1 Done Criteria: All tests below must pass before moving to Week 2.
    /// Run with: dotnet test --filter "ExecutionContextTests"
    /// </summary>
    public class ExecutionContextTests
    {
        // ?? Helpers ???????????????????????????????????????????????????????????
        private static ExecutionContext CreateContext() => new(
            tenantId: Guid.NewGuid(),
            projectId: Guid.NewGuid(),
            suiteId: Guid.NewGuid(),
            environmentId: Guid.NewGuid(),
            batchId: Guid.NewGuid(),
            logger: NullLogger.Instance);

        // ?? Test 1: SetVariable and GetVariable ???????????????????????????????
        [Fact]
        public void SetVariable_ThenGetVariable_ReturnsCorrectValue()
        {
            var ctx = CreateContext();

            ctx.SetVariable("token", "abc123");

            Assert.Equal("abc123", ctx.GetVariable("token"));
        }

        // ?? Test 2: HasVariable ???????????????????????????????????????????????
        [Fact]
        public void HasVariable_WhenSet_ReturnsTrue()
        {
            var ctx = CreateContext();
            ctx.SetVariable("userId", "42");

            Assert.True(ctx.HasVariable("userId"));
        }

        [Fact]
        public void HasVariable_WhenNotSet_ReturnsFalse()
        {
            var ctx = CreateContext();

            Assert.False(ctx.HasVariable("nonExistentVar"));
        }

        // ?? Test 3: GetVariable missing ???????????????????????????????????????
        [Fact]
        public void GetVariable_WhenNotSet_ReturnsNull()
        {
            var ctx = CreateContext();

            var result = ctx.GetVariable("missing");

            Assert.Null(result);
        }

        // ?? Test 4: SetVariable overwrites ????????????????????????????????????
        [Fact]
        public void SetVariable_CalledTwice_OverwritesValue()
        {
            var ctx = CreateContext();
            ctx.SetVariable("token", "first");
            ctx.SetVariable("token", "second");

            Assert.Equal("second", ctx.GetVariable("token"));
        }

        // ?? Test 5: MergeEnvironmentVariables ?????????????????????????????????
        [Fact]
        public void MergeEnvironmentVariables_SeedsAllVariables()
        {
            var ctx = CreateContext();
            var envVars = new Dictionary<string, string>
            {
                { "baseUrl", "https://api.dev.io" },
                { "timeout", "5000" }
            };

            ctx.MergeEnvironmentVariables(envVars);

            Assert.Equal("https://api.dev.io", ctx.GetVariable("baseUrl"));
            Assert.Equal("5000", ctx.GetVariable("timeout"));
        }

        // ?? Test 6: MergeEnvironmentVariables does NOT overwrite existing ?????
        [Fact]
        public void MergeEnvironmentVariables_DoesNotOverwriteExistingVariable()
        {
            var ctx = CreateContext();
            ctx.SetVariable("baseUrl", "https://api.staging.io");  // already set

            ctx.MergeEnvironmentVariables(new Dictionary<string, string>
            {
                { "baseUrl", "https://api.dev.io" }  // should NOT overwrite
            });

            Assert.Equal("https://api.staging.io", ctx.GetVariable("baseUrl"));
        }

        // ?? Test 7: SetVariable with empty key does nothing ???????????????????
        [Fact]
        public void SetVariable_EmptyKey_DoesNotThrow()
        {
            var ctx = CreateContext();

            var ex = Record.Exception(() => ctx.SetVariable("", "value"));

            Assert.Null(ex);
            Assert.False(ctx.HasVariable(""));
        }

        // ?? Test 8: GetAllVariables returns snapshot ??????????????????????????
        [Fact]
        public void GetAllVariables_ReturnsAllSetVariables()
        {
            var ctx = CreateContext();
            ctx.SetVariable("a", "1");
            ctx.SetVariable("b", "2");
            ctx.SetVariable("c", "3");

            var all = ctx.GetAllVariables();

            Assert.Equal(3, all.Count);
            Assert.Equal("1", all["a"]);
            Assert.Equal("2", all["b"]);
            Assert.Equal("3", all["c"]);
        }

        // ?? Test 9: Thread safety with parallel SetVariable ???????????????????
        [Fact]
        public async Task SetVariable_CalledFromMultipleThreads_DoesNotThrow()
        {
            var ctx = CreateContext();

            // Simulate parallel case execution setting variables concurrently
            var tasks = Enumerable.Range(0, 100).Select(i =>
                Task.Run(() => ctx.SetVariable($"var_{i}", i.ToString())));

            await Task.WhenAll(tasks);

            // All 100 variables should be set without exception
            Assert.Equal(100, ctx.GetAllVariables().Count);
        }

        // ?? Test 10: Identity properties set correctly ????????????????????????
        [Fact]
        public void Constructor_SetsIdentityProperties()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var suiteId = Guid.NewGuid();
            var environmentId = Guid.NewGuid();
            var batchId = Guid.NewGuid();

            var ctx = new ExecutionContext(
                tenantId, projectId, suiteId, environmentId, batchId,
                NullLogger.Instance);

            Assert.Equal(tenantId, ctx.TenantId);
            Assert.Equal(projectId, ctx.ProjectId);
            Assert.Equal(suiteId, ctx.SuiteId);
            Assert.Equal(environmentId, ctx.EnvironmentId);
            Assert.Equal(batchId, ctx.BatchId);
        }
    }
}