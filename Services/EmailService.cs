using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAlertEmailAsync(SystemMetrics metrics)
        {
            try
            {
                string toAddress = _configuration.GetValue<string>("EmailSettings:ToAddress") ?? "";
                if (string.IsNullOrEmpty(toAddress))
                {
                    _logger.LogWarning("Email recipient address not configured.");
                    return;
                }

                var emailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration.GetValue<string>("EmailSettings:FromAddress") ?? "systemmonitor@example.com"),
                    Subject = "Системний моніторинг - ПОПЕРЕДЖЕННЯ",
                    IsBodyHtml = true,
                    Body = GenerateAlertEmailBody(metrics)
                };

                emailMessage.To.Add(toAddress);

                using var smtpClient = new SmtpClient
                {
                    Host = _configuration.GetValue<string>("EmailSettings:SmtpServer") ?? "",
                    EnableSsl = _configuration.GetValue<bool>("EmailSettings:UseSsl", true)
                };

                int port = 587; 
                string? portString = _configuration.GetValue<string>("EmailSettings:Port");
                if (!string.IsNullOrEmpty(portString) && int.TryParse(portString, out int parsedPort))
                {
                    port = parsedPort;
                }
                smtpClient.Port = port;

                if (_configuration.GetValue<bool>("EmailSettings:UseAuthentication", true))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(
                        _configuration.GetValue<string>("EmailSettings:Username") ?? "",
                        _configuration.GetValue<string>("EmailSettings:Password") ?? ""
                    );
                }
                else
                {
                    smtpClient.UseDefaultCredentials = true;
                }

                await smtpClient.SendMailAsync(emailMessage);
                _logger.LogInformation("Alert email sent successfully to {EmailAddress}", toAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert email");
                throw;
            }
        }

        private string GenerateAlertEmailBody(SystemMetrics metrics)
        {
            var body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .alert {{ color: #D8000C; background-color: #FFBABA; padding: 10px; border-radius: 5px; }}
        .warning {{ color: #9F6000; background-color: #FEEFB3; padding: 10px; border-radius: 5px; }}
        .info {{ color: #00529B; background-color: #BDE5F8; padding: 10px; border-radius: 5px; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <h2>Системний моніторинг - Попередження</h2>
    <p>Час: {metrics.Timestamp}</p>

    <h3>Стан системи:</h3>
    <div class='{(metrics.CpuUsagePercentage > 90 ? "alert" : metrics.CpuUsagePercentage > 70 ? "warning" : "info")}'>
        <strong>ЦП використання:</strong> {metrics.CpuUsagePercentage}%
    </div>
    <br>
    <div class='{(metrics.MemoryUsagePercentage > 90 ? "alert" : metrics.MemoryUsagePercentage > 70 ? "warning" : "info")}'>
        <strong>Пам'ять:</strong> {metrics.MemoryUsagePercentage}% ({metrics.UsedMemoryGB:F1} ГБ використано з {metrics.TotalMemoryGB:F1} ГБ)
    </div>

    <h3>Диски:</h3>
    <table>
        <tr>
            <th>Диск</th>
            <th>Використано</th>
            <th>Вільно</th>
            <th>Всього</th>
            <th>Статус</th>
        </tr>";

            foreach (var disk in metrics.DiskMetrics)
            {
                double usedPercentage = (disk.UsedSpaceGB / disk.TotalSpaceGB) * 100;
                string statusClass = usedPercentage > 90 ? "alert" : usedPercentage > 70 ? "warning" : "info";
                
                body += $@"
        <tr>
            <td>{disk.DriveName}</td>
            <td>{disk.UsedSpaceGB:F1} ГБ ({usedPercentage:F1}%)</td>
            <td>{disk.FreeSpaceGB:F1} ГБ</td>
            <td>{disk.TotalSpaceGB:F1} ГБ</td>
            <td class='{statusClass}'>{(usedPercentage > 90 ? "Критично" : usedPercentage > 70 ? "Увага" : "OK")}</td>
        </tr>";
            }

            body += @"
    </table>

    <h3>Мережа:</h3>
    <table>
        <tr>
            <th>Інтерфейс</th>
            <th>Статус</th>
            <th>Затримка</th>
            <th>Втрати пакетів</th>
            <th>Використання</th>
        </tr>";

            foreach (var network in metrics.NetworkMetrics)
            {
                string statusClass = !network.IsConnected ? "alert" : 
                                     network.PacketLoss > 10 || network.Latency > 500 ? "warning" : "info";
                
                body += $@"
        <tr>
            <td>{network.InterfaceName}</td>
            <td class='{(!network.IsConnected ? "alert" : "info")}'>{(network.IsConnected ? "Підключено" : "Відключено")}</td>
            <td class='{(network.Latency > 500 ? "warning" : "info")}'>{network.Latency} мс</td>
            <td class='{(network.PacketLoss > 10 ? "warning" : "info")}'>{network.PacketLoss}%</td>
            <td>{network.BandwidthUsagePercentage}%</td>
        </tr>";
            }

            body += @"
    </table>

    <p>Це автоматичне сповіщення від системи моніторингу.</p>
</body>
</html>";

            return body;
        }
    }

    public interface IEmailService
    {
        Task SendAlertEmailAsync(SystemMetrics metrics);
    }
}