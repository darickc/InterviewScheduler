using System.Net.Http.Json;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Services;

public interface IContactsApiClient
{
    Task<IReadOnlyList<ContactDto>> GetAllAsync(CancellationToken ct = default);
    Task<ContactDto?> GetAsync(int id, CancellationToken ct = default);
    Task<ContactDto> CreateAsync(ContactDto dto, CancellationToken ct = default);
    Task<ContactDto> UpdateAsync(int id, ContactDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ContactDto>> ImportAsync(Stream csvContent, string fileName, CancellationToken ct = default);
}

public class ContactsApiClient : IContactsApiClient
{
    private readonly HttpClient _http;

    public ContactsApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<ContactDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<ContactDto>>("api/contacts", ct);
        return result ?? new List<ContactDto>();
    }

    public Task<ContactDto?> GetAsync(int id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<ContactDto>($"api/contacts/{id}", ct);

    public async Task<ContactDto> CreateAsync(ContactDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/contacts", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactDto>(cancellationToken: ct))!;
    }

    public async Task<ContactDto> UpdateAsync(int id, ContactDto dto, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/contacts/{id}", dto, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/contacts/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ContactDto>> ImportAsync(Stream csvContent, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(csvContent);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync("api/contacts/import", content, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ContactDto>>(cancellationToken: ct);
        return result ?? new List<ContactDto>();
    }
}
