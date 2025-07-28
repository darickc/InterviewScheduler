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
}

public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
}