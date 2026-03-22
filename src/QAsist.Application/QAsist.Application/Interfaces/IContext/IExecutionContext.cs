using Microsoft.Extensions.Logging;

namespace QAsist.Application.Interfaces.IContext
{
    public interface IExecutionContext
    {
        // ── Identity ──────────────────────────────────────────────────────────
        Guid TenantId { get; }
        Guid ProjectId { get; }
        Guid SuiteId { get; }
        Guid EnvironmentId { get; }
        Guid BatchId { get; }

        // ── Variable Operations ───────────────────────────────────────────────

        /// <summary>Store a variable. Overwrites if already exists.</summary>
        void SetVariable(string key, string value);

        /// <summary>Get a variable value. Returns null if not found.</summary>
        string? GetVariable(string key);

        /// <summary>Returns true if the variable exists in context.</summary>
        bool HasVariable(string key);

        /// <summary>Returns a snapshot of all current variables (for logging/debugging).</summary>
        IReadOnlyDictionary<string, string> GetAllVariables();

        /// <summary>
        /// Merges a dictionary of variables into the context.
        /// Used to seed environment variables before execution starts.
        /// Existing variables are NOT overwritten (environment vars are lowest priority).
        /// </summary>
        void MergeEnvironmentVariables(Dictionary<string, string> envVars);

        // ── Logging ───────────────────────────────────────────────────────────
        ILogger Logger { get; }
    }
}
