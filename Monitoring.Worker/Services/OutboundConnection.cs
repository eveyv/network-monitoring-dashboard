namespace Monitoring.Worker.Models;

public class OutboundConnection
{
    public string DestinationIP { get; set; } = "";
    public int Port { get; set; }
    public string Protocol { get; set; } = "";
    public string Domain { get; set; } = "";

    public override int GetHashCode()
        => HashCode.Combine(DestinationIP, Port, Protocol);

    public override bool Equals(object? obj)
    {
        if (obj is not OutboundConnection other)
            return false;

        return DestinationIP == other.DestinationIP
            && Port == other.Port
            && Protocol == other.Protocol;
    }
}