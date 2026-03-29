namespace QAsist.Domain.Entities
{
    /// <summary>
    /// A single executable HTTP step inside a TestCase.
    /// Replaces the old List&lt;string&gt; Steps model.
    /// </summary>
    public class ExecutableStep
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>GET | POST | PUT | PATCH | DELETE</summary>
        public string Method { get; set; } = "GET";

        /// <summary>Relative or absolute URL. Supports {{variable}} interpolation.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>JSON object serialized as string, or null for GET/DELETE.</summary>
        public string? RequestBody { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; } = new();

        public int TimeoutMs { get; set; } = 10_000;

        public int RetryCount { get; set; } = 0;

        public bool IsEnabled { get; set; } = true;

        /// <summary>Assertions validated after the HTTP call.</summary>
        public List<StepAssertion> Assertions { get; set; } = new();

        /// <summary>Variables extracted from the response and placed in context.</summary>
        public List<StepExtraction> Extractions { get; set; } = new();
    }

    public class StepAssertion
    {
        /// <summary>StatusCode | BodyContains | JsonPath | ResponseTimeMs | HeaderExists</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Expected value as string (cast internally as needed).</summary>
        public string Expected { get; set; } = string.Empty;

        /// <summary>JSON path used when Type == JsonPath, e.g. "data.id"</summary>
        public string? JsonPath { get; set; }
    }

    public class StepExtraction
    {
        /// <summary>Variable name placed in context, e.g. "productId"</summary>
        public string Variable { get; set; } = string.Empty;

        /// <summary>Dot-notation JSON path, e.g. "id" or "data.token"</summary>
        public string JsonPath { get; set; } = string.Empty;

        public string? DefaultValue { get; set; }
    }
}