using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Core.Extensions;
using InterviewScheduler.Core.Helpers;
using static InterviewScheduler.Core.Interfaces.ICalendarService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Claims;
using Itenso.TimePeriod;

namespace InterviewScheduler.Infrastructure.Services;

// Helper class to use access token with Google APIs
public class AccessTokenCredential : IConfigurableHttpClientInitializer
{
    private readonly string _accessToken;

    public AccessTokenCredential(string accessToken)
    {
        _accessToken = accessToken;
    }

    public void Initialize(ConfigurableHttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }
}

public class GoogleCalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private CalendarService? _calendarService;

    public GoogleCalendarService(
        IConfiguration configuration, 
        ILogger<GoogleCalendarService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // Check if we have the access token
                var accessToken = await httpContext.GetTokenAsync("access_token");
                return !string.IsNullOrEmpty(accessToken);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public Task<string> GetAuthorizationUrlAsync(string redirectUri)
    {
        // With ASP.NET Core authentication, we just return the challenge URL
        return Task.FromResult("/signin-google");
    }

    public Task<bool> ProcessAuthorizationCodeAsync(string code, string redirectUri)
    {
        // This method is no longer needed with ASP.NET Core authentication
        // The authentication is handled by the authentication middleware
        _logger.LogInformation("ProcessAuthorizationCodeAsync called - authentication is now handled by ASP.NET Core");
        return Task.FromResult(true);
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
        // Call the enhanced version with default working hours
        var defaultWorkingHours = WorkingHours.CreateStandardBusinessHours();
        return await GetAvailableTimeSlotsAsync(calendarId, startDate, endDate, durationMinutes, defaultWorkingHours);
    }

    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string calendarId, DateTime startDate, DateTime endDate, int durationMinutes, WorkingHours? workingHours)
    {
        await EnsureAuthenticatedAsync();

        var timeSlots = new List<TimeSlot>();

        try
        {
            _logger.LogInformation($"Fetching calendar events from {startDate} to {endDate} for calendar {calendarId}");

            // Get all events in the date range and convert to TimePeriodCollection
            var conflictingPeriods = await GetConflictingPeriodsAsync(calendarId, new TimeRange(startDate, endDate));

            _logger.LogInformation($"Found {conflictingPeriods.Count} existing events in the date range");

            // Use working hours if provided, otherwise use full time range
            if (workingHours != null)
            {
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    // Get available slots for this date based on working hours
                    var daySlots = workingHours.GetAvailableSlots(currentDate);
                    
                    foreach (var workingSlot in daySlots)
                    {
                        // Ensure we only consider slots within the requested time range
                        var constrainedSlot = new TimeRange(
                            workingSlot.Start < startDate ? startDate : workingSlot.Start,
                            workingSlot.End > endDate ? endDate : workingSlot.End
                        );

                        if (constrainedSlot.Duration.TotalMinutes >= durationMinutes)
                        {
                            var slots = constrainedSlot.SplitIntoSlots(durationMinutes);
                            
                            foreach (var slot in slots)
                            {
                                var isAvailable = !conflictingPeriods.Any(cp => cp.IntersectsWith(slot));
                                
                                timeSlots.Add(new TimeSlot
                                {
                                    StartTime = slot.Start,
                                    EndTime = slot.End,
                                    IsAvailable = isAvailable,
                                    LeaderId = 0, // Default, will be set by specific leader methods
                                    LeaderName = "" // Default, will be set by specific leader methods
                                });
                            }
                        }
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }
            }
            else
            {
                // Fallback to original logic for backward compatibility
                var searchRange = new TimeRange(startDate, endDate);
                var slots = searchRange.SplitIntoSlots(durationMinutes);
                
                foreach (var slot in slots)
                {
                    var isAvailable = !conflictingPeriods.Any(cp => cp.IntersectsWith(slot));
                    
                    timeSlots.Add(new TimeSlot
                    {
                        StartTime = slot.Start,
                        EndTime = slot.End,
                        IsAvailable = isAvailable,
                        LeaderId = 0,
                        LeaderName = ""
                    });
                }
            }

            _logger.LogInformation($"Generated {timeSlots.Count} total time slots, {timeSlots.Count(s => s.IsAvailable)} available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available time slots for calendar {CalendarId}", calendarId);
            throw;
        }

        return timeSlots;
    }

    public async Task<List<TimeSlot>> GetAvailableTimeSlotsForLeaderAsync(string calendarId, int leaderId, string leaderName, DateTime startDate, DateTime endDate, int durationMinutes, WorkingHours? workingHours = null)
    {
        var timeSlots = await GetAvailableTimeSlotsAsync(calendarId, startDate, endDate, durationMinutes, workingHours);
        
        // Set leader information on all slots
        foreach (var slot in timeSlots)
        {
            slot.LeaderId = leaderId;
            slot.LeaderName = leaderName;
        }
        
        return timeSlots;
    }

    public async Task<bool> IsTimeSlotAvailableAsync(string calendarId, ITimePeriod timeRange)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var conflictingPeriods = await GetConflictingPeriodsAsync(calendarId, timeRange);
            return !conflictingPeriods.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check time slot availability using TimePeriod");
            return false;
        }
    }

    public async Task<TimePeriodCollection> GetConflictingPeriodsAsync(string calendarId, ITimePeriod timeRange)
    {
        await EnsureAuthenticatedAsync();

        var conflicts = new TimePeriodCollection();

        try
        {
            var request = _calendarService!.Events.List(calendarId);
            request.TimeMinDateTimeOffset = timeRange.Start;
            request.TimeMaxDateTimeOffset = timeRange.End;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();

            foreach (var eventItem in events.Items)
            {
                var eventStart = eventItem.Start.DateTimeDateTimeOffset?.DateTime ?? eventItem.Start.DateTime;
                var eventEnd = eventItem.End.DateTimeDateTimeOffset?.DateTime ?? eventItem.End.DateTime;
                
                if (eventStart.HasValue && eventEnd.HasValue)
                {
                    // Convert to local time if needed
                    var localEventStart = eventStart.Value.Kind == DateTimeKind.Utc ? eventStart.Value.ToLocalTime() : eventStart.Value;
                    var localEventEnd = eventEnd.Value.Kind == DateTimeKind.Utc ? eventEnd.Value.ToLocalTime() : eventEnd.Value;
                    
                    var eventTimeRange = new TimeRange(localEventStart, localEventEnd);
                    
                    // Only add if it actually conflicts with our time range
                    if (eventTimeRange.IntersectsWith(timeRange))
                    {
                        conflicts.Add(eventTimeRange);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conflicting periods for calendar {CalendarId}", calendarId);
            throw;
        }

        return conflicts;
    }

    public async Task<List<Appointment>> FindConflictingAppointmentsAsync(string calendarId, AppointmentTimeRange appointmentTimeRange, IEnumerable<Appointment> existingAppointments)
    {
        var conflicts = new List<Appointment>();

        try
        {
            // First check Google Calendar conflicts
            var calendarConflicts = await GetConflictingPeriodsAsync(calendarId, appointmentTimeRange);
            
            // Then check local appointment conflicts
            var localConflicts = appointmentTimeRange.GetConflicts(existingAppointments);
            
            // Combine and return unique conflicts
            conflicts.AddRange(localConflicts);
            
            // Log information about conflicts found
            if (calendarConflicts.Any())
            {
                _logger.LogWarning($"Found {calendarConflicts.Count} Google Calendar conflicts for appointment {appointmentTimeRange.AppointmentId}");
            }
            
            if (localConflicts.Any())
            {
                _logger.LogWarning($"Found {localConflicts.Count} local appointment conflicts for appointment {appointmentTimeRange.AppointmentId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find conflicting appointments");
            throw;
        }

        return conflicts.Distinct().ToList();
    }

    public async Task<string> CreateEventAsync(string calendarId, Appointment appointment)
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var newEvent = new Event()
            {
                Summary = appointment.Contact?.DisplayName ?? "Appointment",
                Description = $"Appointment Type: {appointment.AppointmentType?.Name}\nContact: {appointment.Contact?.DisplayName}\nPhone: {appointment.Contact?.PhoneNumber}",
                Start = new EventDateTime()
                {
                    DateTimeDateTimeOffset = appointment.ScheduledTime,
                    TimeZone = TimeZoneInfo.Local.Id
                },
                End = new EventDateTime()
                {
                    DateTimeDateTimeOffset = appointment.ScheduledTime.AddMinutes(appointment.AppointmentType?.Duration ?? 30),
                    TimeZone = TimeZoneInfo.Local.Id
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
            
            eventToUpdate.Summary = appointment.Contact?.DisplayName ?? "Appointment";
            eventToUpdate.Description = $"Appointment Type: {appointment.AppointmentType?.Name}\nContact: {appointment.Contact?.DisplayName}\nPhone: {appointment.Contact?.PhoneNumber}";
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

    private async Task EnsureAuthenticatedAsync()
    {
        if (_calendarService == null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("User is not authenticated. Please sign in with Google.");
            }

            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Access token not found. Please sign in again.");
            }

            // Initialize the Calendar service with the access token
            _calendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = new AccessTokenCredential(accessToken),
                ApplicationName = "Interview Scheduler"
            });

            _logger.LogInformation("Google Calendar service initialized with user access token");
        }
    }


    public Task ClearStoredCredentialsAsync()
    {
        // With ASP.NET Core authentication, clearing is handled by signing out
        _calendarService = null;
        _logger.LogInformation("Calendar service cleared - use sign out to clear authentication");
        return Task.CompletedTask;
    }

    public async Task<List<CalendarInfo>> GetCalendarsAsync()
    {
        await EnsureAuthenticatedAsync();

        try
        {
            var request = _calendarService!.CalendarList.List();
            var calendars = await request.ExecuteAsync();

            return calendars.Items.Select(cal => new CalendarInfo
            {
                Id = cal.Id,
                Name = cal.Summary ?? "Unnamed Calendar",
                Description = cal.Description ?? string.Empty,
                IsPrimary = cal.Primary ?? false
            }).OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve calendars");
            return new List<CalendarInfo>();
        }
    }
}