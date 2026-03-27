using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Interfaces.IContext;
using System.Text.RegularExpressions;

namespace QAsist.Application.Execution
{
    /// <summary>
    /// T17-T20 — Resolves {{variable}} placeholders in strings.
    ///
    /// FIX: Nested variables like {{key_{{env}}}} require the INNER
    /// placeholder to be resolved first. The standard regex Replace()
    /// processes left-to-right and cannot handle this — it tries to
    /// match {{key_{{env}}}} as one token which fails.
    ///
    /// Solution: Before running Replace(), detect if the template
    /// contains nested {{ patterns and resolve the innermost ones first
    /// using an inside-out pass.
    /// </summary>
    public class VariableResolver : IVariableResolver
    {
        // Matches the INNERMOST {{variable}} — no {{ or }} inside it
        // {{[\w\.]+}} = {{ then only word chars/dots then }}
        // This guarantees we always match the innermost placeholder first
        private static readonly Regex _innermostRegex =
            new(@"\{\{([\w\.]+)\}\}", RegexOptions.Compiled);

        // Detects if any {{ still remain (used to check if more passes needed)
        private static readonly Regex _hasPlaceholder =
            new(@"\{\{", RegexOptions.Compiled);

        private const int MaxPasses = 10; // safety limit against infinite loops

        private readonly ILogger<VariableResolver> _logger;

        public VariableResolver(ILogger<VariableResolver> logger)
        {
            _logger = logger;
        }

        // ── T17: Core Resolve ─────────────────────────────────────────────────
        public string Resolve(string? template, IExecutionContext context)
        {
            // T18: Null/empty guard
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            // Fast path — no placeholders at all
            if (!template.Contains("{{"))
                return template;

            return ResolveInternal(template, context);
        }

        // ── ResolveHeaders ────────────────────────────────────────────────────
        public Dictionary<string, string> ResolveHeaders(
            Dictionary<string, string> headers,
            IExecutionContext context)
        {
            if (headers is null || headers.Count == 0)
                return new Dictionary<string, string>();

            var resolved = new Dictionary<string, string>(headers.Count);
            foreach (var (key, value) in headers)
                resolved[key] = Resolve(value, context);

            return resolved;
        }

        // ── Internal: inside-out multi-pass resolver ──────────────────────────
        /// <summary>
        /// Resolves placeholders from the INSIDE OUT using multiple passes.
        ///
        /// Pass 1: resolve all innermost {{var}} (no nested {{ inside)
        ///         "{{key_{{env}}}}" → first resolves {{env}} → "{{key_prod}}"
        /// Pass 2: resolve newly formed placeholders
        ///         "{{key_prod}}" → resolves → "secret123"
        ///
        /// Cycle detection: tracks which keys we resolved in this full
        /// resolution chain. If a key appears again, it's a circular reference.
        /// </summary>
        private string ResolveInternal(string template, IExecutionContext context)
        {
            var current = template;
            var globalVisited = new HashSet<string>(); // for circular detection
            int pass = 0;

            while (pass < MaxPasses && _hasPlaceholder.IsMatch(current))
            {
                pass++;
                var previous = current;

                current = _innermostRegex.Replace(current, match =>
                {
                    var key = match.Groups[1].Value;

                    // T20: Circular reference check
                    if (globalVisited.Contains(key))
                    {
                        var cycle = string.Join(" → ", globalVisited) + " → " + key;
                        throw new CircularVariableException(cycle);
                    }

                    var value = context.GetVariable(key);

                    // T18: Missing variable
                    if (value is null)
                    {
                        _logger.LogWarning(
                            "Variable '{Key}' not found in ExecutionContext. " +
                            "Replacing with empty string. Available: [{Available}]",
                            key,
                            string.Join(", ", context.GetAllVariables().Keys));
                        return string.Empty;
                    }

                    globalVisited.Add(key);

                    _logger.LogDebug(
                        "Resolved {{{{{Key}}}}} = '{Value}'",
                        key,
                        IsSensitiveKey(key) ? "[REDACTED]" : value);

                    return value;
                });

                // No change this pass — nothing left to resolve, stop
                if (current == previous)
                    break;
            }

            if (pass >= MaxPasses)
            {
                _logger.LogWarning(
                    "Variable resolution hit max passes ({Max}). " +
                    "Possible unresolvable nesting. Template: '{Template}'",
                    MaxPasses, template);
            }

            return current;
        }

        private static bool IsSensitiveKey(string key)
        {
            var lower = key.ToLowerInvariant();
            return lower.Contains("token")
                || lower.Contains("password")
                || lower.Contains("secret")
                || lower.Contains("apikey")
                || lower.Contains("api_key")
                || lower.Contains("auth");
        }
    }
}