namespace SystemMonitor.Models;

public class SystemMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercentage { get; set; }
    
    // Розширена інформація про пам'ять
    public double MemoryUsagePercentage { get; set; }
    public double TotalMemoryGB { get; set; }
    public double UsedMemoryGB { get; set; }
    public double FreeMemoryGB { get; set; }
    
    // Інформація про диск
    public List<DiskMetric> DiskMetrics { get; set; } = new();
    
    // Інформація про мережу
    public List<NetworkMetric> NetworkMetrics { get; set; } = new();
    
    // Індикатори критичних станів
    public bool IsCpuCritical => CpuUsagePercentage > 90;
    public bool IsMemoryCritical => MemoryUsagePercentage > 90;
    public bool HasCriticalDisk => DiskMetrics.Any(d => d.UsagePercentage > 90);
    public bool HasCriticalNetwork => NetworkMetrics.Any(n => n.IsNetworkCritical);
    
    public bool HasAnyCriticalState => IsCpuCritical || IsMemoryCritical || HasCriticalDisk || HasCriticalNetwork;
}

public class DiskMetric
{
    public string DriveName { get; set; } = string.Empty;
    public double TotalSpaceGB { get; set; }
    public double UsedSpaceGB { get; set; }
    public double FreeSpaceGB { get; set; }
    public double UsagePercentage => (UsedSpaceGB / TotalSpaceGB) * 100;
    public bool IsCritical => UsagePercentage > 90;
}

public class NetworkMetric
{
    public string InterfaceName { get; set; } = string.Empty;
    public double BytesSent { get; set; }
    public double BytesReceived { get; set; }
    public double SendSpeed { get; set; } // bytes per second
    public double ReceiveSpeed { get; set; } // bytes per second
    
    // Нові поля для мережевої активності
    public double PacketLoss { get; set; } // percentage
    public double Latency { get; set; } // milliseconds
    public bool IsConnected { get; set; }
    public double BandwidthUsagePercentage { get; set; }
    
    public bool IsNetworkCritical => PacketLoss > 20 || Latency > 500 || !IsConnected || BandwidthUsagePercentage > 90;
}