using Microsoft.AspNetCore.Server.Kestrel.Core;
using SystemMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHostedService<BackgroundMetricsService>();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<MetricsHub>("/metricsHub");

app.Run();