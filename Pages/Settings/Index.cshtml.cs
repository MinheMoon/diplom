using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SystemMonitor.Models;
using SystemMonitor.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SystemMonitor.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public bool SendEmailAlerts { get; set; }
    
    [BindProperty]
    public int IntervalMinutes { get; set; }
    
    [BindProperty]
    public required string EmailAddress { get; set; }
    
    [BindProperty]
    public int CpuThreshold { get; set; }
    
    [BindProperty]
    public int MemoryThreshold { get; set; }
    
    [BindProperty]
    public int DiskThreshold { get; set; }
    
    [BindProperty]
    public required string SmtpServer { get; set; }
    
    [BindProperty]
    public int SmtpPort { get; set; }
    
    [BindProperty]
    public required string SmtpFrom { get; set; }
    
    [BindProperty]
    public bool SmtpUseSsl { get; set; }
    
    [BindProperty]
    public bool SmtpUseAuth { get; set; }
    
    [BindProperty]
    public required string SmtpUsername { get; set; }
    
    [BindProperty]
    public required string SmtpPassword { get; set; }

    public IndexModel(IConfiguration configuration, IEmailService emailService, ILogger<IndexModel> logger)
    {
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
        
        EmailAddress = string.Empty;
        SmtpServer = string.Empty;
        SmtpFrom = string.Empty;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
    }

    public void OnGet()
    {
        SendEmailAlerts = _configuration.GetValue<bool>("AlertSettings:SendEmailAlerts", false);
        IntervalMinutes = _configuration.GetValue<int>("AlertSettings:IntervalMinutes", 15);
        EmailAddress = _configuration.GetValue<string>("EmailSettings:ToAddress") ?? "admin@example.com";
        
        CpuThreshold = _configuration.GetValue<int>("AlertSettings:CpuThreshold", 90);
        MemoryThreshold = _configuration.GetValue<int>("AlertSettings:MemoryThreshold", 90);
        DiskThreshold = _configuration.GetValue<int>("AlertSettings:DiskThreshold", 90);
        
        SmtpServer = _configuration.GetValue<string>("EmailSettings:SmtpServer") ?? "";
        SmtpPort = _configuration.GetValue<int>("EmailSettings:Port", 587);
        SmtpFrom = _configuration.GetValue<string>("EmailSettings:FromAddress") ?? "";
        SmtpUseSsl = _configuration.GetValue<string>("EmailSettings:UseSsl", "true") == "true";
        SmtpUseAuth = _configuration.GetValue<string>("EmailSettings:UseAuthentication", "true") == "true";
        SmtpUsername = _configuration.GetValue<string>("EmailSettings:Username") ?? "";
        SmtpPassword = _configuration.GetValue<string>("EmailSettings:Password") ?? "";
    }

    public IActionResult OnPost()
    {
        try
        {
            var configFile = "appsettings.json";
            var json = System.IO.File.ReadAllText(configFile);
            var jsonNode = JsonNode.Parse(json);
            
            if (jsonNode == null)
            {
                jsonNode = new JsonObject();
            }
            
            var jsonObject = jsonNode.AsObject();
            
            if (jsonObject["AlertSettings"] == null)
            {
                jsonObject["AlertSettings"] = new JsonObject();
            }
            
            var alertSettings = new JsonObject
            {
                ["SendEmailAlerts"] = SendEmailAlerts,
                ["IntervalMinutes"] = IntervalMinutes,
                ["CpuThreshold"] = CpuThreshold,
                ["MemoryThreshold"] = MemoryThreshold,
                ["DiskThreshold"] = DiskThreshold
            };
            
            jsonObject["AlertSettings"] = alertSettings;
            
            if (jsonObject["EmailSettings"] == null)
            {
                jsonObject["EmailSettings"] = new JsonObject();
            }
            
            var emailSettings = jsonObject["EmailSettings"]!.AsObject();
            emailSettings["ToAddress"] = EmailAddress;
            
            jsonObject["EmailSettings"] = emailSettings;
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(jsonObject, options);
            System.IO.File.WriteAllText(configFile, updatedJson);
            
            TempData["SuccessMessage"] = "Налаштування сповіщень успішно збережено.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving alert settings");
            TempData["ErrorMessage"] = "Помилка при збереженні налаштувань: " + ex.Message;
        }
        
        return RedirectToPage();
    }
    
    public IActionResult OnPostSaveSmtp()
    {
        try
        {
            var configFile = "appsettings.json";
            var json = System.IO.File.ReadAllText(configFile);
            var jsonNode = JsonNode.Parse(json);
            
            if (jsonNode == null)
            {
                jsonNode = new JsonObject();
            }
            
            var jsonObject = jsonNode.AsObject();
            
            if (jsonObject["EmailSettings"] == null)
            {
                jsonObject["EmailSettings"] = new JsonObject();
            }
            
            var emailSettings = new JsonObject
            {
                ["SmtpServer"] = SmtpServer,
                ["Port"] = SmtpPort,
                ["FromAddress"] = SmtpFrom,
                ["ToAddress"] = _configuration.GetValue<string>("EmailSettings:ToAddress") ?? "admin@example.com",
                ["UseSsl"] = SmtpUseSsl.ToString().ToLower(),
                ["UseAuthentication"] = SmtpUseAuth.ToString().ToLower(),
                ["Username"] = SmtpUsername,
                ["Password"] = SmtpPassword
            };
            
            jsonObject["EmailSettings"] = emailSettings;
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(jsonObject, options);
            System.IO.File.WriteAllText(configFile, updatedJson);
            
            TempData["SuccessMessage"] = "SMTP налаштування успішно збережено.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SMTP settings");
            TempData["ErrorMessage"] = "Помилка при збереженні SMTP налаштувань: " + ex.Message;
        }
        
        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnPostTestEmailAsync()
    {
        try
        {
            var testMetrics = new SystemMetrics
            {
                Timestamp = DateTime.Now,
                CpuUsagePercentage = 95,
                MemoryUsagePercentage = 92,
                TotalMemoryGB = 16,
                UsedMemoryGB = 14.7,
                FreeMemoryGB = 1.3,
                DiskMetrics = new List<DiskMetric>
                {
                    new DiskMetric
                    {
                        DriveName = "C:",
                        TotalSpaceGB = 500,
                        UsedSpaceGB = 475,
                        FreeSpaceGB = 25
                    }
                },
                NetworkMetrics = new List<NetworkMetric>
                {
                    new NetworkMetric
                    {
                        InterfaceName = "Ethernet",
                        IsConnected = true,
                        PacketLoss = 25,
                        Latency = 600,
                        BandwidthUsagePercentage = 92
                    }
                }
            };
            
            await _emailService.SendAlertEmailAsync(testMetrics);
            
            TempData["SuccessMessage"] = "Тестове сповіщення успішно надіслано! Перевірте вашу електронну пошту.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email");
            TempData["ErrorMessage"] = "Помилка при відправці тестового сповіщення: " + ex.Message;
        }
        
        return RedirectToPage();
    }
}