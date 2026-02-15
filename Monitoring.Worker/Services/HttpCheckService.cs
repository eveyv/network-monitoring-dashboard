using System.Diagnostics;
using Monitoring.Core.Models;


namespace Monitoring.Worker.Services;

public class HttpCheckService
{
    private readonly HttpClient _httpClient;

    public HttpCheckService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<CheckResult> ExecuteAsync(Device device, string url, string? requiredContent)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            stopwatch.Stop();

            bool containsRequired = requiredContent == null ||
                                    content.Contains(requiredContent, StringComparison.OrdinalIgnoreCase);

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Http,
                IsSuccess = response.IsSuccessStatusCode && containsRequired,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = containsRequired
                    ? $"Status {(int)response.StatusCode}"
                    : "Required content not found"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new CheckResult
            {
                DeviceId = device.Id,
                Type = CheckType.Http,
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message
            };
        }
    }
}
