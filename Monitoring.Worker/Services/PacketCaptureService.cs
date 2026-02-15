using System;
using SharpPcap;
using PacketDotNet;

namespace Monitoring.Worker.Services;

public class PacketCaptureService
{
    private ICaptureDevice? _device;

    public void StartCapture(string? filter = null)
    {
        var devices = CaptureDeviceList.Instance;

        if (devices.Count < 1)
        {
            Console.WriteLine("No capture devices found! Make sure you are running as sudo.");
            return;
        }

        Console.WriteLine("Available devices:");
        for (int i = 0; i < devices.Count; i++)
        {
            Console.WriteLine($"{i}: {devices[i].Name} - {devices[i].Description}");
        }

        _device = devices[0];
        _device.OnPacketArrival += Device_OnPacketArrival;

// this order is important: open, filter, capture.
        _device.Open();

        if (!string.IsNullOrEmpty(filter))
            _device.Filter = filter;

        _device.StartCapture();

        Console.WriteLine($"Started packet capture on {_device.Description}");
    }

    private void Device_OnPacketArrival(object sender, PacketCapture e)
    {
        var raw = e.GetPacket();
        var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);

        var ipPacket = packet.Extract<IPPacket>();
        if (ipPacket != null)
        {
            Console.WriteLine($"Packet: {ipPacket.SourceAddress} -> {ipPacket.DestinationAddress}, Protocol: {ipPacket.Protocol}");
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
}
