using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface ICalendarApiClient
{
    Task<IReadOnlyList<TimeSlotDto>> GetEventsAsync(int leaderId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<TimeSlotDto>> GetAvailabilityAsync(int leaderId, DateTime start, DateTime end, int durationMinutes, CancellationToken ct = default);
    Task DeleteEventAsync(string eventId, int leaderId, CancellationToken ct = default);
}

public class CalendarApiClient : ICalendarApiClient
{
    private readonly HttpClient _http;

    public CalendarApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<TimeSlotDto>> GetEventsAsync(int leaderId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var url = $"api/calendar/events?leaderId={leaderId}" +
                  $"&start={Uri.EscapeDataString(start.ToString("o"))}" +
                  $"&end={Uri.EscapeDataString(end.ToString("o"))}";
        var result = await _http.GetFromJsonAsync<List<TimeSlotDto>>(url, ct);
        return result ?? new List<TimeSlotDto>();
    }

    public async Task<IReadOnlyList<TimeSlotDto>> GetAvailabilityAsync(int leaderId, DateTime start, DateTime end, int durationMinutes, CancellationToken ct = default)
    {
        var url = $"api/calendar/availability?leaderId={leaderId}" +
                  $"&start={Uri.EscapeDataString(start.ToString("o"))}" +
                  $"&end={Uri.EscapeDataString(end.ToString("o"))}" +
                  $"&durationMinutes={durationMinutes}";
        var result = await _http.GetFromJsonAsync<List<TimeSlotDto>>(url, ct);
        return result ?? new List<TimeSlotDto>();
    }

    public async Task DeleteEventAsync(string eventId, int leaderId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/calendar/events/{Uri.EscapeDataString(eventId)}?leaderId={leaderId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
