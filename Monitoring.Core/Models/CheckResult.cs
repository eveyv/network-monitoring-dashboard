namespace Monitoring.Core.Models;

public class CheckResult
{
    public Guid DeviceId { get; set; }
    public CheckType Type { get; set; }

    public bool IsSuccess { get; set; }
    public long ResponseTimeMs { get; set; }

    public string? Message { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
