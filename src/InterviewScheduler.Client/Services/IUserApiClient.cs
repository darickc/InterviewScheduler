using System.Net;
using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface IUserApiClient
{
    Task<UserDto?> GetMeAsync(CancellationToken ct = default);
}

public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _http;

    public UserApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserDto?> GetMeAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/user/me", ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
    }
}
