using InterviewScheduler.Core.Enums;
using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    
    public int LeaderId { get; set; }
    public Leader Leader { get; set; } = null!;
    
    public int AppointmentTypeId { get; set; }
    public AppointmentType AppointmentType { get; set; } = null!;
    
    public DateTime ScheduledTime { get; set; }
    public string? GoogleEventId { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // User relationship
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Gets the time range for this appointment using TimePeriodLibrary.
    /// This is computed from ScheduledTime and AppointmentType.Duration.
    /// </summary>
    public TimeRange TimeRange
    {
        get
        {
            var endTime = ScheduledTime.AddMinutes(AppointmentType?.Duration ?? 30);
            return new TimeRange(ScheduledTime, endTime);
        }
    }
    
    /// <summary>
    /// Gets the appointment as an AppointmentTimeRange with metadata.
    /// </summary>
    public AppointmentTimeRange AppointmentTimeRange
    {
        get
        {
            return AppointmentTimeRange.FromAppointment(this);
        }
    }
    
    /// <summary>
    /// Gets the start time of the appointment (same as ScheduledTime for compatibility).
    /// </summary>
    public DateTime StartTime => ScheduledTime;
    
    /// <summary>
    /// Gets the end time of the appointment based on duration.
    /// </summary>
    public DateTime EndTime => ScheduledTime.AddMinutes(AppointmentType?.Duration ?? 30);
    
    /// <summary>
    /// Checks if this appointment conflicts with another appointment.
    /// </summary>
    /// <param name="other">The other appointment to check against.</param>
    /// <returns>True if the appointments overlap, false otherwise.</returns>
    public bool ConflictsWith(Appointment other)
    {
        if (other == null) return false;
        if (LeaderId != other.LeaderId) return false; // Different leaders, no conflict
        
        return TimeRange.IntersectsWith(other.TimeRange);
    }
    
    /// <summary>
    /// Checks if this appointment conflicts with a time range.
    /// </summary>
    /// <param name="timeRange">The time range to check against.</param>
    /// <returns>True if the appointment overlaps with the time range, false otherwise.</returns>
    public bool ConflictsWith(ITimePeriod timeRange)
    {
        return TimeRange.IntersectsWith(timeRange);
    }
}