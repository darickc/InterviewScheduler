using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface ILeadersApiClient
{
    Task<IReadOnlyList<LeaderDto>> GetAllAsync(CancellationToken ct = default);
    Task<LeaderDto?> GetAsync(int id, CancellationToken ct = default);
    Task<LeaderDto> CreateAsync(LeaderDto dto, CancellationToken ct = default);
    Task<LeaderDto> UpdateAsync(int id, LeaderDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarInfoDto>> GetCalendarsAsync(CancellationToken ct = default);
}

public class LeadersApiClient : ILeadersApiClient
{
    private readonly HttpClient _http;

    public LeadersApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<LeaderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<LeaderDto>>("api/leaders", ct);
        return result ?? new List<LeaderDto>();
    }

    public Task<LeaderDto?> GetAsync(int id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<LeaderDto>($"api/leaders/{id}", ct);

    public async Task<LeaderDto> CreateAsync(LeaderDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/leaders", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LeaderDto>(cancellationToken: ct))!;
    }

    public async Task<LeaderDto> UpdateAsync(int id, LeaderDto dto, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/leaders/{id}", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LeaderDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/leaders/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<CalendarInfoDto>> GetCalendarsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<CalendarInfoDto>>("api/leaders/calendars", ct);
        return result ?? new List<CalendarInfoDto>();
    }
}
