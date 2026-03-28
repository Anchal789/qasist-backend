using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QAsist.Application.DTOs.Monitoring;

namespace QAsist.Infrastructure.SignalR
{
    /// <summary>
    /// SignalR Hub for real-time monitoring updates.
    ///
    /// Clients connect to: /hubs/monitoring
    ///
    /// Events pushed to clients:
    ///   endpointUpdated  → EndpointStatusUpdateDto  (every check)
    ///   statusChanged    → EndpointStatusUpdateDto  (only when Up↔Down changes)
    ///   endpointDown     → EndpointStatusUpdateDto  (alert: endpoint went down)
    ///
    /// Client connection example (JS):
    ///   const connection = new signalR.HubConnectionBuilder()
    ///     .withUrl("/hubs/monitoring", { accessTokenFactory: () => token })
    ///     .build();
    ///
    ///   connection.on("endpointUpdated", (data) => { updateUI(data); });
    ///   connection.on("endpointDown",    (data) => { showAlert(data); });
    ///   await connection.start();
    ///
    ///   // Subscribe to a specific endpoint group
    ///   await connection.invoke("SubscribeToEndpoint", endpointId);
    /// </summary>
    [Authorize]
    public class MonitoringHub : Hub
    {
        private readonly ILogger<MonitoringHub> _logger;

        public MonitoringHub(ILogger<MonitoringHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogDebug(
                "SignalR client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug(
                "SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client subscribes to a specific endpoint group.
        /// Push updates only to subscribed clients.
        /// </summary>
        public async Task SubscribeToEndpoint(string endpointId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"endpoint:{endpointId}");
            _logger.LogDebug(
                "Client {ConnectionId} subscribed to endpoint {EndpointId}",
                Context.ConnectionId, endpointId);
        }

        /// <summary>Client unsubscribes from a specific endpoint group.</summary>
        public async Task UnsubscribeFromEndpoint(string endpointId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"endpoint:{endpointId}");
        }

        /// <summary>Client joins a project group (receives updates for ALL endpoints in project).</summary>
        public async Task SubscribeToProject(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
            _logger.LogDebug(
                "Client {ConnectionId} subscribed to project {ProjectId}",
                Context.ConnectionId, projectId);
        }
    }

    /// <summary>
    /// Service for pushing real-time updates from monitoring jobs to clients.
    /// Injected into MonitoringService to push after each check.
    /// </summary>
    
}