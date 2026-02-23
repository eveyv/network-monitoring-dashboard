using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Monitoring.Worker.Services;
using System.Collections.Generic;
using SharpPcap;
using PacketDotNet;

public class Worker : BackgroundService
{
    private readonly PacketCaptureService _packetCaptureService;

    private readonly List<string> _hosts = new()
    {
        "192.168.1.1", // router
        // "google.com",  // external host
        // "everettyeaw.com" // personal website
    };

    public Worker()
    {
        _packetCaptureService = new PacketCaptureService();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _packetCaptureService.StartCapture();

    Console.WriteLine("Packet capture started...");

    while (!stoppingToken.IsCancellationRequested)
    {
        // Print the device summary every loop
        _packetCaptureService.PrintDeviceSummary();

        // Optional network checks (currently commented)
        foreach (var host in _hosts)
        {
            // await RunPingCheck(host);
            // await RunTcpCheck(host, 80);
            // await RunHttpCheck(host);
        }

        await Task.Delay(10000, stoppingToken); // 10 second loop
    }

    _packetCaptureService.StopCapture();
}

    private async Task RunPingCheck(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);
            Console.WriteLine($"PING {host}: Success={reply.Status == IPStatus.Success}, {reply.RoundtripTime}ms, {reply.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PING {host}: Error - {ex.Message}");
        }
    }

    private async Task RunTcpCheck(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            var result = await Task.WhenAny(task, Task.Delay(3000));

            if (result == task && client.Connected)
                Console.WriteLine($"TCP {host}:{port}: Success=True");
            else
                Console.WriteLine($"TCP {host}:{port}: Success=False, Timeout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP {host}:{port}: Error - {ex.Message}");
        }
    }

    private async Task RunHttpCheck(string host)
    {
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMilliseconds(3000);

            var response = await http.GetAsync($"http://{host}");
            Console.WriteLine($"HTTP {host}: Success=True, Status {response.StatusCode}");
        }
        catch
        {
            Console.WriteLine($"HTTP {host}: Success=False, Timeout/Error");
        }
    }
}
