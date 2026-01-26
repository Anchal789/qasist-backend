namespace QAsist.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetIpAddress(this HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        public static string GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers.UserAgent.ToString() ?? "Unknown";
        }
    }
}
