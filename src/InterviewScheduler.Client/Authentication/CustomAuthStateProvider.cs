using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using InterviewScheduler.Shared.Dtos;
using Microsoft.AspNetCore.Components.Authorization;

namespace InterviewScheduler.Client.Authentication;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly HttpClient _http;
    private AuthenticationState? _cached;

    public CustomAuthStateProvider(HttpClient http)
    {
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cached is not null) return _cached;

        try
        {
            var response = await _http.GetAsync("api/user/me");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return _cached = Anonymous;
            }

            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user is null) return _cached = Anonymous;

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                },
                authenticationType: "Cookies");

            return _cached = new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (HttpRequestException)
        {
            return _cached = Anonymous;
        }
    }

    public void NotifyUserChanged()
    {
        _cached = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
