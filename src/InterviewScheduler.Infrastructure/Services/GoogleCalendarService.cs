using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InterviewScheduler.Infrastructure.Services;

public class GoogleCalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private CalendarService? _calendarService;
    private UserCredential? _credential;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            return Task.FromResult(_credential != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<string> GetAuthorizationUrlAsync(string redirectUri)
    {
        var clientId = _configuration["GoogleCalendar:ClientId"];
        var clientSecret = _configuration["GoogleCalendar:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google Calendar credentials not configured");
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new[] { CalendarService.Scope.Calendar },
            DataStore = new FileDataStore("CalendarCredentials")
        });

        var authRequest = flow.CreateAuthorizationCodeRequest(redirectUri);
        return authRequest.Build().ToString();
    }

    public async Task<bool> ProcessAuthorizationCodeAsync(string code, string redirectUri)
    {
        try
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"];

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("CalendarCredentials")
            });

            var token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
            _credential = new UserCredential(flow, "user", token);

            // Initialize calendar service
            _calendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Interview Scheduler"
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process authorization code");
            return false;
        }
    }

    public async Task<bool> IsTimeSlotAvailableAsync(string calendarId, DateTime startTime, DateTime endTime)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var request = _calendarService!.Events.List(calendarId);
            request.TimeMinDateTimeOffset = startTime;
            request.TimeMaxDateTimeOffset = endTime;
            request.SingleEvents = true;

            var events = await request.ExecuteAsync();
            return !events.Items.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check time slot availability");
            return false;
        }
    }

    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string calendarId, DateTime startDate, DateTime endDate, int durationMinutes)
    {
        await EnsureAuthenticatedAsync();

        var timeSlots = new List<TimeSlot>();

        try
        {
            // Get all events in the date range
            var request = _calendarService!.Events.List(calendarId);
            request.TimeMinDateTimeOffset = startDate;
            request.TimeMaxDateTimeOffset = endDate;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();

            // Generate potential time slots (e.g., every 30 minutes from 9 AM to 5 PM)
            var current = startDate.Date.AddHours(9); // Start at 9 AM
            var dayEnd = startDate.Date.AddHours(17); // End at 5 PM

            while (current.Date <= endDate.Date)
            {
                while (current.TimeOfDay < dayEnd.TimeOfDay)
                {
                    var slotEnd = current.AddMinutes(durationMinutes);
                    
                    if (slotEnd.TimeOfDay <= dayEnd.TimeOfDay)
                    {
                        var isAvailable = !events.Items.Any(e =>
                        {
                            var eventStart = e.Start.DateTimeDateTimeOffset?.DateTime;
                            var eventEnd = e.End.DateTimeDateTimeOffset?.DateTime;
                            
                            if (eventStart.HasValue && eventEnd.HasValue)
                            {
                                return current < eventEnd && slotEnd > eventStart;
                            }
                            return false;
                        });

                        timeSlots.Add(new TimeSlot
                        {
                            StartTime = current,
                            EndTime = slotEnd,
                            IsAvailable = isAvailable
                        });
                    }

                    current = current.AddMinutes(30); // Move to next 30-minute slot
                }

                current = current.Date.AddDays(1).AddHours(9); // Move to next day at 9 AM
                dayEnd = current.Date.AddHours(17);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available time slots");
        }

        return timeSlots;
    }

    public async Task<string> CreateEventAsync(string calendarId, Appointment appointment)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var newEvent = new Event()
            {
                Summary = $"Meeting with {appointment.Contact?.FullName}",
                Description = $"Appointment Type: {appointment.AppointmentType?.Name}\nContact: {appointment.Contact?.FullName}\nPhone: {appointment.Contact?.PhoneNumber}",
                Start = new EventDateTime()
                {
                    DateTimeDateTimeOffset = appointment.ScheduledTime,
                    TimeZone = TimeZoneInfo.Local.Id
                },
                End = new EventDateTime()
                {
                    DateTimeDateTimeOffset = appointment.ScheduledTime.AddMinutes(appointment.AppointmentType?.Duration ?? 30),
                    TimeZone = TimeZoneInfo.Local.Id
                },
                Attendees = new[]
                {
                    new EventAttendee()
                    {
                        Email = appointment.Contact?.PhoneNumber + "@sms.example.com", // Placeholder - would need actual email
                        DisplayName = appointment.Contact?.FullName
                    }
                }
            };

            var request = _calendarService!.Events.Insert(newEvent, calendarId);
            var createdEvent = await request.ExecuteAsync();

            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event");
            throw;
        }
    }

    public async Task<bool> UpdateEventAsync(string calendarId, string eventId, Appointment appointment)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var eventToUpdate = await _calendarService!.Events.Get(calendarId, eventId).ExecuteAsync();
            
            eventToUpdate.Summary = $"Meeting with {appointment.Contact?.FullName}";
            eventToUpdate.Description = $"Appointment Type: {appointment.AppointmentType?.Name}\nContact: {appointment.Contact?.FullName}\nPhone: {appointment.Contact?.PhoneNumber}";
            eventToUpdate.Start.DateTimeDateTimeOffset = appointment.ScheduledTime;
            eventToUpdate.End.DateTimeDateTimeOffset = appointment.ScheduledTime.AddMinutes(appointment.AppointmentType?.Duration ?? 30);

            await _calendarService.Events.Update(eventToUpdate, calendarId, eventId).ExecuteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event");
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(string calendarId, string eventId)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            await _calendarService!.Events.Delete(calendarId, eventId).ExecuteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event");
            return false;
        }
    }

    private Task EnsureAuthenticatedAsync()
    {
        if (_calendarService == null)
        {
            throw new InvalidOperationException("Google Calendar service not authenticated. Please complete the OAuth flow first.");
        }
        return Task.CompletedTask;
    }
}