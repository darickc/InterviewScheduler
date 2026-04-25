using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface IAppointmentTypesApiClient
{
    Task<IReadOnlyList<AppointmentTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<AppointmentTypeDto?> GetAsync(int id, CancellationToken ct = default);
    Task<AppointmentTypeDto> CreateAsync(AppointmentTypeDto dto, CancellationToken ct = default);
    Task<AppointmentTypeDto> UpdateAsync(int id, AppointmentTypeDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class AppointmentTypesApiClient : IAppointmentTypesApiClient
{
    private readonly HttpClient _http;

    public AppointmentTypesApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AppointmentTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<AppointmentTypeDto>>("api/appointment-types", ct);
        return result ?? new List<AppointmentTypeDto>();
    }

    public Task<AppointmentTypeDto?> GetAsync(int id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<AppointmentTypeDto>($"api/appointment-types/{id}", ct);

    public async Task<AppointmentTypeDto> CreateAsync(AppointmentTypeDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/appointment-types", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentTypeDto>(cancellationToken: ct))!;
    }

    public async Task<AppointmentTypeDto> UpdateAsync(int id, AppointmentTypeDto dto, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/appointment-types/{id}", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentTypeDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/appointment-types/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
