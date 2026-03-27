namespace QAsist.Application.Interfaces.IContext
{
    /// <summary>
    /// T16 — Contract for resolving {{variable}} placeholders in strings.
    ///
    /// Used by StepExecutor (Week 6) before every HTTP call to replace
    /// all {{key}} patterns in URL, headers, body, and auth values.
    ///
    /// Rules:
    ///   - Missing variable → log warning, replace with empty string, never throw
    ///   - Nested variables → resolve inner first, max 3 levels deep
    ///   - Circular reference → throw CircularVariableException
    ///   - Null/empty template → return empty string, never throw
    /// </summary>
    public interface IVariableResolver
    {
        /// <summary>
        /// Replaces all {{key}} placeholders in the template string
        /// with values from the ExecutionContext.
        /// </summary>
        /// <param name="template">String containing {{variable}} placeholders.</param>
        /// <param name="context">Current execution context holding variable values.</param>
        /// <returns>Resolved string with all placeholders replaced.</returns>
        string Resolve(string? template, IExecutionContext context);

        /// <summary>
        /// Resolves a dictionary of values (e.g. request headers).
        /// Each value is resolved independently.
        /// Keys are never resolved — only values.
        /// </summary>
        Dictionary<string, string> ResolveHeaders(
            Dictionary<string, string> headers,
            IExecutionContext context);
    }
}
