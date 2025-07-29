using InterviewScheduler.Core.Enums;

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
}