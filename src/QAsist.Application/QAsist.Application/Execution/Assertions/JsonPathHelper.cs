using Newtonsoft.Json.Linq;

namespace QAsist.Application.Execution.Assertions
{
    /// Uses Newtonsoft.Json JToken.SelectToken() which supports
    /// full JSONPath syntax including array indexing, filters, etc.
    ///
    /// Examples:
    ///   "$.data.id"          → simple field
    ///   "$.users[0].email"   → array index
    ///   "$.items[?(@.active == true)].name"  → filter expression
    /// </summary>
    public static class JsonPathHelper
    {
        /// <summary>
        /// Evaluates a JSONPath expression against a JSON body string.
        /// Returns null if: body is invalid JSON, path is invalid, or token not found.
        /// Never throws — all exceptions returned as null with out error message.
        /// </summary>
        public static JToken? SelectToken(
            string? jsonBody,
            string? jsonPath,
            out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(jsonBody))
            {
                errorMessage = "Response body is empty or null.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                errorMessage = "JSONPath expression is empty or null.";
                return null;
            }

            JToken root;
            try
            {
                root = JToken.Parse(jsonBody);
            }
            catch (Exception ex)
            {
                errorMessage = $"Response body is not valid JSON: {ex.Message}";
                return null;
            }

            try
            {
                var token = root.SelectToken(jsonPath);
                return token;   // null = path not found (not an error)
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid JSONPath expression '{jsonPath}': {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Converts a JToken to its string representation.
        /// Arrays return comma-separated values.
        /// Null returns null.
        /// </summary>
        public static string? TokenToString(JToken? token)
        {
            if (token is null)
                return null;

            return token.Type switch
            {
                JTokenType.String => token.Value<string>(),
                JTokenType.Integer => token.Value<long>().ToString(),
                JTokenType.Float => token.Value<double>().ToString(),
                JTokenType.Boolean => token.Value<bool>().ToString().ToLowerInvariant(),
                JTokenType.Null => null,
                JTokenType.Array => string.Join(",",
                    ((JArray)token).Select(t => TokenToString(t))),
                _ => token.ToString()
            };
        }
    }
}
