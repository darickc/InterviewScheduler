using System.Net.Http.Json;
using InterviewScheduler.Core.Enums;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface IAppointmentsApiClient
{
    Task<IReadOnlyList<AppointmentDto>> GetAllAsync(
        int? leaderId = null,
        int? contactId = null,
        DateTime? from = null,
        DateTime? to = null,
        AppointmentStatus? status = null,
        CancellationToken ct = default);

    Task<AppointmentDto?> GetAsync(int id, CancellationToken ct = default);
    Task<AppointmentDto> CreateAsync(AppointmentDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<AppointmentDto> ConfirmAsync(int id, CancellationToken ct = default);
    Task<AppointmentDto> CancelAsync(int id, CancellationToken ct = default);
    Task<AppointmentDto> RescheduleAsync(int id, RescheduleAppointmentRequest request, CancellationToken ct = default);
}

public class AppointmentsApiClient : IAppointmentsApiClient
{
    private readonly HttpClient _http;

    public AppointmentsApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAllAsync(
        int? leaderId = null,
        int? contactId = null,
        DateTime? from = null,
        DateTime? to = null,
        AppointmentStatus? status = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (leaderId.HasValue) query.Add($"leaderId={leaderId.Value}");
        if (contactId.HasValue) query.Add($"contactId={contactId.Value}");
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (status.HasValue) query.Add($"status={status.Value}");

        var url = "api/appointments" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        var result = await _http.GetFromJsonAsync<List<AppointmentDto>>(url, ct);
        return result ?? new List<AppointmentDto>();
    }

    public Task<AppointmentDto?> GetAsync(int id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<AppointmentDto>($"api/appointments/{id}", ct);

    public async Task<AppointmentDto> CreateAsync(AppointmentDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/appointments", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/appointments/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AppointmentDto> ConfirmAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/appointments/{id}/confirm", content: null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(cancellationToken: ct))!;
    }

    public async Task<AppointmentDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/appointments/{id}/cancel", content: null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(cancellationToken: ct))!;
    }

    public async Task<AppointmentDto> RescheduleAsync(int id, RescheduleAppointmentRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/appointments/{id}/reschedule", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
        }

        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(cancellationToken: ct))!;
    }
}
