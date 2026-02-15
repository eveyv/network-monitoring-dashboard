using System.Diagnostics;
using System.Net.Sockets;
using Monitoring.Core.Models;

namespace Monitoring.Worker.Services;

public class TcpCheckService
{
    public async Task<CheckResult> ExecuteAsync(Device device, int port)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(device.IpAddress, port);

            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            stopwatch.Stop();

            if (completedTask == timeoutTask)
            {
                return new CheckResult
                {
                    DeviceId = device.Id,
                    Type = CheckType.Tcp,
                    IsSuccess = false,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Message = "Timeout"
                };
            }

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Tcp,
                IsSuccess = true,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = "Connected"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Tcp,
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message
            };
        }
    }
}
