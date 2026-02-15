using System;

namespace Monitoring.Core.Models;

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty; 
    public int Port { get; set; } 
}
