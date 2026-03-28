using Hangfire;
using Microsoft.AspNetCore.Builder;
using QAsist.Application.Interfaces.IServices;

namespace QAsist.Infrastructure.Extensions
{
    public static class HangfireExtensions
    {
        public static WebApplication UseQAsistHangfire(this WebApplication app)
        {
            app.UseHangfireDashboard("/hangfire");

            // Example recurring job (every 1 min)
            RecurringJob.AddOrUpdate<IMonitoringService>(
     "monitor-all-endpoints",
     s => s.CheckAllEndpointsAsync(CancellationToken.None),
     "* * * * *"
 );

            return app;
        }
    }
}