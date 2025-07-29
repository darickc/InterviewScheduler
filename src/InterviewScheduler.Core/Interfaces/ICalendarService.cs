using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Interfaces;

public interface ICalendarService
{
    Task<bool> IsTimeSlotAvailableAsync(string calendarId, DateTime startTime, DateTime endTime);
    Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string calendarId, DateTime startDate, DateTime endDate, int durationMinutes);
    Task<string> CreateEventAsync(string calendarId, Appointment appointment);
    Task<bool> UpdateEventAsync(string calendarId, string eventId, Appointment appointment);
    Task<bool> DeleteEventAsync(string calendarId, string eventId);
    Task<string> GetAuthorizationUrlAsync(string redirectUri);
    Task<bool> ProcessAuthorizationCodeAsync(string code, string redirectUri);
    Task<bool> IsAuthenticatedAsync();
    Task ClearStoredCredentialsAsync();
    Task<List<CalendarInfo>> GetCalendarsAsync();
}

public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
}

public class CalendarInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}