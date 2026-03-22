using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext;
using System.Collections.Concurrent;

namespace QAsist.Application.Execution
{
    /// <summary>
    /// In-memory execution state for one suite run.
    /// 
    /// Variable priority (highest → lowest):
    ///   1. Runtime variables (extracted from responses during execution)
    ///   2. Suite-level default variables (set at suite creation)
    ///   3. Environment variables (base URL, global headers, tokens)
    /// 
    /// Later SetVariable() calls always win — this enables step-by-step chaining.
    /// 
    /// Thread safety: ConcurrentDictionary allows parallel case execution
    /// without locks. Variables are keyed by string (case-sensitive).
    /// </summary>
    public class ExecutionContext : IExecutionContext
    {
        // ── Internal variable store ───────────────────────────────────────────
        // ConcurrentDictionary: thread-safe for parallel case execution
        private readonly ConcurrentDictionary<string, string> _variables;

        // ── T13: Identity properties ──────────────────────────────────────────
        public Guid TenantId { get; }
        public Guid ProjectId { get; }
        public Guid SuiteId { get; }
        public Guid EnvironmentId { get; }
        public Guid BatchId { get; }

        // ── T13: Logger ───────────────────────────────────────────────────────
        public ILogger Logger { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public ExecutionContext(
            Guid tenantId,
            Guid projectId,
            Guid suiteId,
            Guid environmentId,
            Guid batchId,
            ILogger logger)
        {
            TenantId = tenantId;
            ProjectId = projectId;
            SuiteId = suiteId;
            EnvironmentId = environmentId;
            BatchId = batchId;
            Logger = logger;
            _variables = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        }

        // ── T14: SetVariable ──────────────────────────────────────────────────
        /// <summary>
        /// Stores a variable in the execution context.
        /// Overwrites any existing value with the same key.
        /// Called by ExtractionEngine after each step.
        /// </summary>
        public void SetVariable(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Logger.LogWarning("Attempted to set variable with null/empty key. Skipped.");
                return;
            }

            _variables[key] = value;

            Logger.LogDebug("Variable set: {Key} = {Value}", key,
                // Redact sensitive variable names in logs
                IsSensitiveKey(key) ? "[REDACTED]" : value);
        }

        // ── T14: GetVariable ──────────────────────────────────────────────────
        /// <summary>
        /// Gets a variable value by key.
        /// Returns null if not found — VariableResolver handles the null case
        /// (logs warning, replaces with empty string).
        /// </summary>
        public string? GetVariable(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            _variables.TryGetValue(key, out var value);
            return value;
        }

        // ── T14: HasVariable ──────────────────────────────────────────────────
        public bool HasVariable(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return _variables.ContainsKey(key);
        }

        // ── T14: GetAllVariables ──────────────────────────────────────────────
        /// <summary>
        /// Returns a READ-ONLY snapshot.
        /// Used for debugging, logging, and frontend variable panel display.
        /// Sensitive keys are redacted in log output but returned as-is here
        /// (caller is responsible for display redaction).
        /// </summary>
        public IReadOnlyDictionary<string, string> GetAllVariables()
        {
            return _variables.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.Ordinal);
        }

        // ── T15: MergeEnvironmentVariables ────────────────────────────────────
        /// <summary>
        /// Seeds environment variables into the context.
        /// Called ONCE before execution starts, after context is created.
        /// 
        /// IMPORTANT: Does NOT overwrite variables already in context.
        /// This means suite-level variables (set before this call) take
        /// precedence over environment variables.
        /// 
        /// Order of seeding (call sequence in SuiteExecutor):
        ///   1. ctx = new ExecutionContext(...)
        ///   2. ctx.MergeEnvironmentVariables(envVars)    ← environment vars (lowest priority)
        ///   3. [execution starts — SetVariable called per extraction]  ← highest priority
        /// </summary>
        public void MergeEnvironmentVariables(Dictionary<string, string> envVars)
        {
            if (envVars is null || envVars.Count == 0)
            {
                Logger.LogDebug("No environment variables to merge.");
                return;
            }

            int merged = 0;
            int skipped = 0;

            foreach (var (key, value) in envVars)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // TryAdd: only adds if key does NOT already exist
                // This preserves suite-level defaults over environment vars
                if (_variables.TryAdd(key, value))
                    merged++;
                else
                    skipped++;
            }

            Logger.LogDebug(
                "Environment variables merged. Added={Merged}, Skipped={Skipped} (already set)",
                merged, skipped);
        }

        // ── PRIVATE: Sensitive key detection ─────────────────────────────────
        /// <summary>
        /// Returns true for variable names that likely contain secrets.
        /// Used to redact values in log output.
        /// </summary>
        private static bool IsSensitiveKey(string key)
        {
            var lower = key.ToLowerInvariant();
            return lower.Contains("token")
                || lower.Contains("password")
                || lower.Contains("secret")
                || lower.Contains("apikey")
                || lower.Contains("api_key")
                || lower.Contains("auth")
                || lower.Contains("credential");
        }
    }
}
