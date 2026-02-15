using System.Diagnostics;
using System.Net.NetworkInformation;
using Monitoring.Core.Models;

namespace Monitoring.Worker.Services;

public class PingCheckService
{
    public async Task<CheckResult> ExecuteAsync(Device device)
    {
        var stopwatch = Stopwatch.StartNew();
        using var ping = new Ping();

        try
        {
            var reply = await ping.SendPingAsync(device.IpAddress, 3000);
            stopwatch.Stop();

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Ping,
                IsSuccess = reply.Status == IPStatus.Success,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = reply.Status.ToString()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Ping,
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message
            };
        }
    }
}
