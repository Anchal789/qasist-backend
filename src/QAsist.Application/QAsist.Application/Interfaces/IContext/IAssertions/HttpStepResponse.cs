namespace QAsist.Application.Interfaces.IContext.IAssertions
{
    /// <summary>
    /// Represents the HTTP response passed to the assertion engine.
    /// Populated by ResponseReader (Week 6) after each HTTP call.
    /// </summary>
    public sealed class HttpStepResponse
    {
        public int StatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
        public Dictionary<string, string> Headers { get; init; } = new();
        public long DurationMs { get; init; }

        // ── Factory ───────────────────────────────────────────────────────────
        public static HttpStepResponse Empty() => new()
        {
            StatusCode = 0,
            Body = string.Empty,
            Headers = new Dictionary<string, string>(),
            DurationMs = 0
        };
    }
}
