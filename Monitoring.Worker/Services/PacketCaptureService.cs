using System;
using System.Net;
using SharpPcap;
using PacketDotNet;
using System.Text;
using Monitoring.Worker.Models;
using System.Collections.Concurrent;

namespace Monitoring.Worker.Services;

public class PacketCaptureService
{
    private ICaptureDevice? _device;

    // private readonly Dictionary<string, string> _devices = new();
    public readonly ConcurrentDictionary<string, NetworkDevice> _devices
    = new();

    public void StartCapture(string? filter = null)
    {
        var devices = CaptureDeviceList.Instance;

        if (devices.Count == 0)
            throw new Exception("No capture devices found.");

        var device = devices.FirstOrDefault(d => d.Name.Contains("en0"));

        if (device == null)
        {
            Console.WriteLine("Available devices:");
            foreach (var d in devices)
                Console.WriteLine($"- {d.Name}");

            throw new Exception("Could not find active interface (en0).");
        }

        Console.WriteLine($"Started packet capture on {device.Name}");

        device.Open(DeviceModes.Promiscuous);

        if (!string.IsNullOrWhiteSpace(filter))
            device.Filter = filter;

        device.OnPacketArrival += OnPacketArrival;
        device.StartCapture();
    }

    // Resolve the device name on the LAN
    private string GetDeviceName(string ip)
    {
        try
        {
            var entry = Dns.GetHostEntry(ip);
            return entry.HostName;
        }
        catch
        {
            return "Unknown";
        }
    }

    // build out or use lookup
    private string GetDeviceTypeFromMac(string mac)
    {
        var cleaned = mac.Replace(":", "").Replace("-", "").ToUpper();
        if (cleaned.Length < 6) return "Unknown Device";

        var oui = cleaned.Substring(0, 6);
        return oui switch
        {
            "3C846A" => "Apple Device",
            "A8B57C" => "Samsung Device",
            "D88083" => "Apple Device",
            _ => "Unknown Device"
        };
    }

    // resolve external IP to hostname
    private string ResolveExternalDomain(string ip)
    {
        try
        {
            var entry = Dns.GetHostEntry(ip);
            return entry.HostName;
        }
        catch
        {
            return "";
        }
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);

        var ethernet = packet.Extract<EthernetPacket>();
        var ipPacket = packet.Extract<IPPacket>();

        if (ethernet == null || ipPacket == null)
            return;

        var srcIp = ipPacket.SourceAddress.ToString();
        var dstIp = ipPacket.DestinationAddress.ToString();
        var mac = ethernet.SourceHardwareAddress.ToString();

        // filter for outbound LAN traffic
        if (!srcIp.StartsWith("192.168.68."))
            return;

        var tcp = packet.Extract<TcpPacket>();
        var udp = packet.Extract<UdpPacket>();

        var port = tcp?.DestinationPort ?? udp?.DestinationPort ?? 0;
        var protocol = ipPacket.Protocol.ToString();

        var device = _devices.GetOrAdd(mac, _ =>
        {
            var newDevice = new NetworkDevice
            {
                IP = srcIp,
                MAC = mac,
                Name = GetDeviceName(srcIp),        // mDNS/DNS
                DeviceType = GetDeviceTypeFromMac(mac),
                FirstSeen = DateTime.UtcNow
            };

            Console.WriteLine("New device detected:");
            Console.WriteLine($"IP: {srcIp}");
            Console.WriteLine($"MAC: {mac}");
            Console.WriteLine($"Name: {newDevice.Name}");
            Console.WriteLine($"Type: {newDevice.DeviceType}");
            Console.WriteLine("--------------------------------");

            return newDevice;
        });

        if (!dstIp.StartsWith("192.168.68."))
        {
            var conn = new OutboundConnection
            {
                DestinationIP = dstIp,
                Port = port,
                Protocol = protocol,
                Domain = ResolveExternalDomain(dstIp)
            };

            device.Connections.Add(conn);
        }
    }

    public void StopCapture()
    {
        if (_device != null)
        {
            _device.StopCapture();
            _device.Close();
            _device = null;
        }
    }

    private string ResolveDomain(string ip)
    {
        try
        {
            var host = Dns.GetHostEntry(ip);
            return host.HostName;
        }
        catch
        {
            return "Unknown";
        }
    }

    private string ResolveHostName(string ip)
    {
        try
        {
            var host = Dns.GetHostEntry(ip);
            return host.HostName;
        }
        catch
        {
            return "Unknown";
        }
    }

    public void PrintDeviceSummary()
    {   
        Console.WriteLine();
        Console.WriteLine("===== DEVICE SUMMARY =====");

        foreach (var device in _devices.Values)
        {
            Console.WriteLine($"Device: {device.IP} ({device.MAC})");
            Console.WriteLine($"Name: {device.Name}");
            Console.WriteLine($"Type: {device.DeviceType}");

            foreach (var conn in device.Connections)
            {
                var domainDisplay = string.IsNullOrWhiteSpace(conn.Domain) ? "" : $" ({conn.Domain})";
                Console.WriteLine($"{device.MAC} / {device.IP} -> {conn.DestinationIP}:{conn.Port} [{conn.Protocol}]{domainDisplay}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("==========================");
    }
}
