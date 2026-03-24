using Microsoft.Extensions.Logging;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Domain.Entities;
using QAsist.Domain.Enums;

namespace QAsist.Application.Execution.Extractions
{
    /// Resilience rules (T36):
    ///   - One extractor failing NEVER stops the others
    ///   - Errors are logged as warnings, never thrown
    ///   - If extraction fails and DefaultValue is set → use DefaultValue
    ///   - If extraction fails and no DefaultValue → store empty string
    ///   - Runs AFTER assertions regardless of assertion results
    /// </summary>
    public class ExtractionEngine : IExtractionEngine
    {
        private readonly IReadOnlyDictionary<ExtractionSource, IExtractor> _extractors;
        private readonly ILogger<ExtractionEngine> _logger;

        public ExtractionEngine(
            IEnumerable<IExtractor> extractors,
            ILogger<ExtractionEngine> logger)
        {
            _extractors = extractors.ToDictionary(e => e.SupportedSource);
            _logger = logger;
        }

        public Dictionary<string, string> ExtractAll(
            IEnumerable<Extraction> extractions,
            HttpStepResponse response,
            IExecutionContext context)
        {
            var extracted = new Dictionary<string, string>();
            var extractionList = extractions?.ToList() ?? new List<Extraction>();

            if (extractionList.Count == 0)
            {
                _logger.LogDebug("No extractions configured for this step.");
                return extracted;
            }

            _logger.LogDebug("Running {Count} extractions.", extractionList.Count);

            foreach (var extraction in extractionList)
            {
                try
                {
                    ProcessExtraction(extraction, response, context, extracted);
                }
                catch (Exception ex)
                {
                    // T36: Never abort — catch all exceptions per extraction
                    _logger.LogWarning(ex,
                        "Unexpected error extracting variable '{VarName}'. Skipping.",
                        extraction.VariableName);

                    StoreValue(extraction.VariableName,
                        extraction.DefaultValue ?? string.Empty,
                        context, extracted);
                }
            }

            _logger.LogInformation(
                "Extraction complete. Variables extracted: [{Variables}]",
                string.Join(", ", extracted.Keys));

            return extracted;
        }

        private void ProcessExtraction(
            Extraction extraction,
            HttpStepResponse response,
            IExecutionContext context,
            Dictionary<string, string> extracted)
        {
            // Look up the extractor for this source type
            if (!_extractors.TryGetValue(extraction.Source, out var extractor))
            {
                _logger.LogWarning(
                    "No extractor for source '{Source}'. Variable '{VarName}' not extracted.",
                    extraction.Source, extraction.VariableName);

                StoreValue(extraction.VariableName,
                    extraction.DefaultValue ?? string.Empty,
                    context, extracted);
                return;
            }

            var value = extractor.Extract(extraction, response, out var errorMessage);

            if (errorMessage is not null)
            {
                // T36: Log warning, use DefaultValue, never throw
                _logger.LogWarning(
                    "Extraction failed for '{VarName}' (source={Source}): {Error}. " +
                    "Using default: '{Default}'",
                    extraction.VariableName,
                    extraction.Source,
                    errorMessage,
                    extraction.DefaultValue ?? "(empty)");

                StoreValue(extraction.VariableName,
                    extraction.DefaultValue ?? string.Empty,
                    context, extracted);
                return;
            }

            // Successful extraction
            var finalValue = value ?? extraction.DefaultValue ?? string.Empty;
            StoreValue(extraction.VariableName, finalValue, context, extracted);

            _logger.LogDebug(
                "Extracted '{VarName}' = '{Value}' from {Source}",
                extraction.VariableName,
                finalValue.Length > 50
                    ? finalValue[..50] + "..."
                    : finalValue,
                extraction.Source);
        }

        private static void StoreValue(
            string variableName,
            string value,
            IExecutionContext context,
            Dictionary<string, string> extracted)
        {
            context.SetVariable(variableName, value);
            extracted[variableName] = value;
        }
    }
}
