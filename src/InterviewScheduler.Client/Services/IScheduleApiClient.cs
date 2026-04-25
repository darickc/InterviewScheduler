using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface IScheduleApiClient
{
    Task<CreateScheduleResult> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default);
}

public class ScheduleApiClient : IScheduleApiClient
{
    private readonly HttpClient _http;

    public ScheduleApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreateScheduleResult> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/schedule/create", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateScheduleResult>(cancellationToken: ct))!;
    }
}
