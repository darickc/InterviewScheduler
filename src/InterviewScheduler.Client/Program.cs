using InterviewScheduler.Client.Authentication;
using InterviewScheduler.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Bare HttpClient is what CustomAuthStateProvider injects. It needs custom 401 handling
// that the typed clients (which call EnsureSuccessStatusCode) don't provide.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

void ConfigureClient(HttpClient client) => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddHttpClient<IUserApiClient, UserApiClient>(ConfigureClient);
builder.Services.AddHttpClient<IContactsApiClient, ContactsApiClient>(ConfigureClient);
builder.Services.AddHttpClient<ILeadersApiClient, LeadersApiClient>(ConfigureClient);
builder.Services.AddHttpClient<IAppointmentTypesApiClient, AppointmentTypesApiClient>(ConfigureClient);
builder.Services.AddHttpClient<IAppointmentsApiClient, AppointmentsApiClient>(ConfigureClient);
builder.Services.AddHttpClient<ICalendarApiClient, CalendarApiClient>(ConfigureClient);
builder.Services.AddHttpClient<IScheduleApiClient, ScheduleApiClient>(ConfigureClient);

// SmsService is pure logic from Shared and runs in WASM unchanged.
builder.Services.AddLogging();
builder.Services.AddScoped<InterviewScheduler.Shared.Services.ISmsService, InterviewScheduler.Shared.Services.SmsService>();

await builder.Build().RunAsync();
