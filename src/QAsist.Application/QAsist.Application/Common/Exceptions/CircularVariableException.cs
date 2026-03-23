namespace QAsist.Application.Common.Exceptions
{
    /// <summary>
    /// T20 — Thrown when a circular variable reference is detected.
    ///
    /// Example that triggers this:
    ///   context["a"] = "{{b}}"
    ///   context["b"] = "{{a}}"
    ///   resolver.Resolve("{{a}}", ctx)  → throws CircularVariableException
    ///
    /// The exception message names the full cycle so the developer
    /// can immediately identify which variables are circular.
    /// Example message: "Circular variable reference detected: a → b → a"
    /// </summary>
    public class CircularVariableException : Exception
    {
        public string CyclePath { get; }

        public CircularVariableException(string cyclePath)
            : base($"Circular variable reference detected: {cyclePath}")
        {
            CyclePath = cyclePath;
        }
    }
}
