using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Application.Execution.Extractions
{
    public interface IExtractor
    {
        Domain.Enums.ExtractionSource SupportedSource { get; }

        /// <summary>
        /// Extracts a value from the HTTP response.
        /// Returns null if extraction fails — caller uses DefaultValue.
        /// NEVER throws — all exceptions handled internally.
        /// </summary>
        string? Extract(Extraction extraction, HttpStepResponse response, out string? errorMessage);
    }
    /// CRITICAL RULES:
    ///   - Runs AFTER assertions (even if assertions failed)
    ///   - One extraction failing NEVER aborts the others
    ///   - All errors are logged and skipped silently
    ///   - Variables stored in context are available to ALL subsequent steps
    /// </summary>
    public interface IExtractionEngine
    {
        /// <summary>
        /// Runs all extractions and stores values in the context.
        /// Returns a dictionary of what was extracted (for logging/storage).
        /// </summary>
        Dictionary<string, string> ExtractAll(
            IEnumerable<Extraction> extractions,
            HttpStepResponse response,
            IExecutionContext context);
    }
}
