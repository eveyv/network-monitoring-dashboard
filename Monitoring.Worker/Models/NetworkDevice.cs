using System;
using System.Collections.Generic;

namespace Monitoring.Worker.Models
{
    public class NetworkDevice
    {
        public string IP { get; set; } = "";
        public string MAC { get; set; } = "";
        public string Name { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public HashSet<OutboundConnection> Connections { get; set; } = new();
    }

    public class OutboundConnection
    {
        public string DestinationIP { get; set; } = "";
        public int Port { get; set; }
        public string Protocol { get; set; } = "";
        public string Domain { get; set; } = "";
    }
}