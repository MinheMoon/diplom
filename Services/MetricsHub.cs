using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    public class MetricsHub : Hub
    {
        private readonly ISystemMetricsService _metricsService;

        public MetricsHub(ISystemMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        public async Task RequestCurrentMetrics()
        {
            var metrics = _metricsService.GetLatestMetrics();
            
            await Clients.Caller.SendAsync("ReceiveMetrics", metrics);
        }

        public async Task RequestHistoricalMetrics(int hours)
        {
            var metrics = _metricsService.GetLatestMetrics();
            await Clients.Caller.SendAsync("ReceiveHistoricalMetrics", new[] { metrics });
        }
    }
}