//using Microsoft.AspNetCore.SignalR;
//using Microsoft.Extensions.Logging;
//using QAsist.Application.DTOs.Monitoring;
//using QAsist.Application.Interfaces.IServices;
//using QAsist.Infrastructure.SignalR;

//namespace QAsist.Infrastructure.Monitoring
//{
//    public class MonitoringNotifier : IMonitoringNotifier
//    {
//        private readonly IHubContext<MonitoringHub> _hub;

//        public MonitoringNotifier(IHubContext<MonitoringHub> hub)
//        {
//            _hub = hub;
//        }

//        public async Task NotifyEndpointUpdateAsync(EndpointStatusUpdateDto dto)
//        {
//            await _hub.Clients.Group(dto.ProjectId.ToString())
//                .SendAsync("EndpointUpdated", dto);
//        }

//        public class MonitoringNotifier
//        {
//            private readonly IHubContext<MonitoringHub> _hubContext;
//            private readonly ILogger<MonitoringNotifier> _logger;

//            public MonitoringNotifier(
//                IHubContext<MonitoringHub> hubContext,
//                ILogger<MonitoringNotifier> logger)
//            {
//                _hubContext = hubContext;
//                _logger = logger;
//            }

//            /// <summary>
//            /// Broadcasts update to:
//            ///   1. All connected clients (Clients.All)
//            ///   2. Endpoint-specific group subscribers
//            ///   3. Project-wide group subscribers
//            /// </summary>
//            public async Task NotifyEndpointUpdateAsync(EndpointStatusUpdateDto update)
//            {
//                try
//                {
//                    // Push to ALL clients (global dashboard)
//                    await _hubContext.Clients.All
//                        .SendAsync("endpointUpdated", update);

//                    // Push to endpoint-specific subscribers
//                    await _hubContext.Clients
//                        .Group($"endpoint:{update.EndpointId}")
//                        .SendAsync("endpointUpdated", update);

//                    // Push to project-wide subscribers
//                    await _hubContext.Clients
//                        .Group($"project:{update.ProjectId}")
//                        .SendAsync("endpointUpdated", update);

//                    // Extra alert event if status changed (Up→Down or Down→Up)
//                    if (update.StatusChanged)
//                    {
//                        await _hubContext.Clients.All
//                            .SendAsync("statusChanged", update);

//                        if (update.Status == "Down")
//                        {
//                            _logger.LogWarning(
//                                "SignalR ALERT: Endpoint {EndpointId} ({Url}) is DOWN!",
//                                update.EndpointId, update.Url);

//                            await _hubContext.Clients.All
//                                .SendAsync("endpointDown", update);
//                        }
//                        else if (update.Status == "Up")
//                        {
//                            _logger.LogInformation(
//                                "SignalR: Endpoint {EndpointId} ({Url}) recovered → UP",
//                                update.EndpointId, update.Url);

//                            await _hubContext.Clients.All
//                                .SendAsync("endpointRecovered", update);
//                        }
//                    }

//                    _logger.LogDebug(
//                        "SignalR pushed: {EndpointId} = {Status} in {Ms}ms",
//                        update.EndpointId, update.Status, update.ResponseTimeMs);
//                }
//                catch (Exception ex)
//                {
//                    // Never let SignalR failure break monitoring
//                    _logger.LogWarning(ex,
//                        "Failed to push SignalR update for endpoint {EndpointId}",
//                        update.EndpointId);
//                }
//            }
//        }
//    }
//}

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs.Monitoring;
using QAsist.Application.Interfaces.IServices;
using QAsist.Infrastructure.SignalR;

namespace QAsist.Infrastructure.Monitoring
{
    public class MonitoringNotifier : IMonitoringNotifier
    {
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly ILogger<MonitoringNotifier> _logger;

        public MonitoringNotifier(
            IHubContext<MonitoringHub> hubContext,
            ILogger<MonitoringNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyEndpointUpdateAsync(EndpointStatusUpdateDto update)
        {
            try
            {
                // 🔥 Global broadcast
                await _hubContext.Clients.All
                    .SendAsync("endpointUpdated", update);

                // 🔥 Endpoint group
                await _hubContext.Clients
                    .Group($"endpoint:{update.EndpointId}")
                    .SendAsync("endpointUpdated", update);

                // 🔥 Project group
                await _hubContext.Clients
                    .Group($"project:{update.ProjectId}")
                    .SendAsync("endpointUpdated", update);

                // 🔥 Status change alerts
                if (update.StatusChanged)
                {
                    await _hubContext.Clients.All
                        .SendAsync("statusChanged", update);

                    if (update.Status == "Down")
                    {
                        _logger.LogWarning(
                            "ALERT: Endpoint {EndpointId} is DOWN!",
                            update.EndpointId);

                        await _hubContext.Clients.All
                            .SendAsync("endpointDown", update);
                    }
                    else if (update.Status == "Up")
                    {
                        _logger.LogInformation(
                            "RECOVERED: Endpoint {EndpointId} is UP",
                            update.EndpointId);

                        await _hubContext.Clients.All
                            .SendAsync("endpointRecovered", update);
                    }
                }

                _logger.LogDebug(
                    "SignalR pushed: {EndpointId} = {Status}",
                    update.EndpointId, update.Status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SignalR push failed for endpoint {EndpointId}",
                    update.EndpointId);
            }
        }
    }
}