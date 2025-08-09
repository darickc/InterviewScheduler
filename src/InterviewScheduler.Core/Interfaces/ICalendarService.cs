using InterviewScheduler.Core.Entities;
using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Interfaces;

public interface ICalendarService
{
    // Existing DateTime-based methods for backward compatibility
    Task<bool> IsTimeSlotAvailableAsync(string calendarId, DateTime startTime, DateTime endTime);
    Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string calendarId, DateTime startDate, DateTime endDate, int durationMinutes);
    
    // New TimePeriod-aware methods
    Task<bool> IsTimeSlotAvailableAsync(string calendarId, ITimePeriod timeRange);
    Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string calendarId, DateTime startDate, DateTime endDate, int durationMinutes, WorkingHours? workingHours);
    Task<List<TimeSlot>> GetAvailableTimeSlotsForLeaderAsync(string calendarId, int leaderId, string leaderName, DateTime startDate, DateTime endDate, int durationMinutes, WorkingHours? workingHours = null);
    Task<TimePeriodCollection> GetConflictingPeriodsAsync(string calendarId, ITimePeriod timeRange);
    Task<List<Appointment>> FindConflictingAppointmentsAsync(string calendarId, AppointmentTimeRange appointmentTimeRange, IEnumerable<Appointment> existingAppointments);
    
    // Event management methods
    Task<string> CreateEventAsync(string calendarId, Appointment appointment);
    Task<bool> UpdateEventAsync(string calendarId, string eventId, Appointment appointment);
    Task<bool> DeleteEventAsync(string calendarId, string eventId);
    
    // Authentication methods
    Task<string> GetAuthorizationUrlAsync(string redirectUri);
    Task<bool> ProcessAuthorizationCodeAsync(string code, string redirectUri);
    Task<bool> IsAuthenticatedAsync();
    Task ClearStoredCredentialsAsync();
    Task<List<CalendarInfo>> GetCalendarsAsync();
}

// TimeSlot class moved to InterviewScheduler.Core.Entities.TimeSlot

public class CalendarInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}