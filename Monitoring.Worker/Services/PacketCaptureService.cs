using System;
using System.Net;
using SharpPcap;
using PacketDotNet;
using System.Text;

namespace Monitoring.Worker.Services;

public class PacketCaptureService
{
    private ICaptureDevice? _device;

    private readonly Dictionary<string, string> _devices = new();

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

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);

        var udp = packet.Extract<UdpPacket>();
        if (udp != null && udp.DestinationPort == 5353)
        {
            var payload = udp.PayloadData;
            var text = Encoding.ASCII.GetString(payload);

            if (text.Contains(".local"))
            {
                Console.WriteLine("mDNS name broadcast detected:");
                Console.WriteLine(text);
                Console.WriteLine("------------------------");
            }
        }

        var ethernet = packet.Extract<EthernetPacket>();
        if (ethernet == null)
            return;

        var ipPacket = packet.Extract<IPPacket>();
        if (ipPacket == null)
            return;

        var mac = ethernet.SourceHardwareAddress.ToString();
        var ip = ipPacket.SourceAddress.ToString();

        if (!_devices.ContainsKey(mac))
        {
            _devices[mac] = ip;

            var name = ResolveHostName(ip);

            Console.WriteLine("New device detected:");
            Console.WriteLine($"IP: {ip}");
            Console.WriteLine($"MAC: {mac}");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine("------------------------");
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
}
