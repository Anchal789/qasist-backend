using System.Linq.Expressions;

namespace QAsist.Application.Interfaces.IHangfireService
{
    public interface IBackgroundJobService
    {
        void Enqueue(Expression<Func<Task>> methodCall);
        void ScheduleRecurring(string jobId, Expression<Func<Task>> methodCall, string cron);
        void RemoveRecurring(string jobId);
    }
}
