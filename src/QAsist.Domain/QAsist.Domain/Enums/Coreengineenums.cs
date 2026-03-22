namespace QAsist.Domain.Enums
{
    

    /// <summary>
    /// HTTP methods supported by the execution engine.
    /// </summary>
    public enum HttpMethod
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        PATCH = 4,
        DELETE = 5,
        HEAD = 6,
        OPTIONS = 7
    }

    /// <summary>
    /// Authentication type for a test step or environment.
    /// </summary>
    public enum AuthType
    {
        None = 0,
        Bearer = 1,
        ApiKey = 2,
        Custom = 3
    }

    /// <summary>
    /// Result status for a single assertion evaluation.
    /// </summary>
    public enum AssertionType
    {
        StatusCodeEquals = 1,
        ResponseTimeLessThan = 2,
        BodyContains = 3,
        FieldEquals = 4,
        FieldExists = 5,
        FieldNotExists = 6,
        FieldMatchesRegex = 7
    }

    /// <summary>
    /// Where to extract a variable value from in the HTTP response.
    /// </summary>
    public enum ExtractionSource
    {
        Body = 1,   // JSONPath on response body
        Header = 2,   // Response header by name
        StatusCode = 3    // HTTP status code as string
    }

    /// <summary>
    /// Result of a single test step execution.
    /// </summary>
    public enum StepStatus
    {
        Passed = 1,
        Failed = 2,
        Skipped = 3,
        TimedOut = 4,
        Error = 5
    }

    /// <summary>
    /// Overall status of a suite execution batch.
    /// </summary>
    public enum ExecutionStatus
    {
        Queued = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Aborted = 5
    }

    /// <summary>
    /// What triggered this execution.
    /// </summary>
    public enum ExecutionTrigger
    {
        Manual = 1,
        Schedule = 2,
        CiCd = 3,
        Webhook = 4
    }

    /// <summary>
    /// Status for URL monitoring checks.
    /// </summary>
    public enum MonitorStatus
    {
        Up = 1,
        Down = 2,
        Degraded = 3
    }
}