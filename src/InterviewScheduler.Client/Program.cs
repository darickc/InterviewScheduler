using InterviewScheduler.Client.Authentication;
using InterviewScheduler.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

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

// SmsService and SchedulingRulesService are pure logic from Shared and run in WASM unchanged.
builder.Services.AddLogging();
builder.Services.AddScoped<InterviewScheduler.Shared.Services.ISmsService, InterviewScheduler.Shared.Services.SmsService>();
builder.Services.Configure<InterviewScheduler.Core.Entities.SchedulingConfiguration>(options =>
{
    var config = InterviewScheduler.Core.Entities.SchedulingConfiguration.CreateUnrestrictedConfiguration();
    config.DefaultBufferTimeMinutes = 0;
    config.DefaultMinimumAdvanceBookingHours = 0;
    config.DefaultMaximumAdvanceBookingDays = 365;
    config.AllowWeekendSchedulingByDefault = true;
    config.AllowAfterHoursSchedulingByDefault = true;
    config.EnforceStrictValidation = false;
    config.Holidays.Clear();
    config.RecurringBlackouts.Clear();

    options.DefaultWorkingHours = config.DefaultWorkingHours;
    options.DefaultBufferTimeMinutes = config.DefaultBufferTimeMinutes;
    options.DefaultMinimumAdvanceBookingHours = config.DefaultMinimumAdvanceBookingHours;
    options.DefaultMaximumAdvanceBookingDays = config.DefaultMaximumAdvanceBookingDays;
    options.MaximumAppointmentDurationMinutes = config.MaximumAppointmentDurationMinutes;
    options.MinimumAppointmentDurationMinutes = config.MinimumAppointmentDurationMinutes;
    options.AllowWeekendSchedulingByDefault = config.AllowWeekendSchedulingByDefault;
    options.AllowAfterHoursSchedulingByDefault = config.AllowAfterHoursSchedulingByDefault;
    options.SystemTimeZone = config.SystemTimeZone;
    options.Holidays = config.Holidays;
    options.RecurringBlackouts = config.RecurringBlackouts;
    options.AllowHighPriorityDoubleBooking = config.AllowHighPriorityDoubleBooking;
    options.DoubleBookingPriorityThreshold = config.DoubleBookingPriorityThreshold;
    options.EnableAutomaticAlternativeSuggestions = config.EnableAutomaticAlternativeSuggestions;
    options.AlternativeSearchDays = config.AlternativeSearchDays;
    options.EnforceStrictValidation = config.EnforceStrictValidation;
    options.DefaultTimeSlotIncrementMinutes = config.DefaultTimeSlotIncrementMinutes;
});
builder.Services.AddScoped<InterviewScheduler.Shared.Services.ISchedulingRulesService, InterviewScheduler.Shared.Services.SchedulingRulesService>();

await builder.Build().RunAsync();
