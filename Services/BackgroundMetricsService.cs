using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SystemMonitor.Services
{
    public class BackgroundMetricsService : BackgroundService
    {
        private readonly ISystemMetricsService _metricsService;
        private readonly IHubContext<MetricsHub> _hubContext;
        private readonly ILogger<BackgroundMetricsService> _logger;

        public BackgroundMetricsService(
            ISystemMetricsService metricsService,
            IHubContext<MetricsHub> hubContext,
            ILogger<BackgroundMetricsService> logger)
        {
            _metricsService = metricsService;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Metrics Service starting");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _metricsService.CollectMetricsAsync();
                        
                        await _metricsService.CheckAlertsAsync();
                        
                        var metrics = _metricsService.GetLatestMetrics();
                        await _hubContext.Clients.All.SendAsync("ReceiveMetrics", metrics, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in metrics collection cycle");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background Metrics Service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Background Metrics Service");
                throw;
            }
        }
    }
}