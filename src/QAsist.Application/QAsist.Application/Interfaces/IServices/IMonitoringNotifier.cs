using QAsist.Application.DTOs.Monitoring;

namespace QAsist.Application.Interfaces.IServices
{
    public interface IMonitoringNotifier
    {
        Task NotifyEndpointUpdateAsync(EndpointStatusUpdateDto dto);
    }
}
