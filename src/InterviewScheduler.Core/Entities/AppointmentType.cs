namespace InterviewScheduler.Core.Entities;

public class AppointmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; } // in minutes
    public string MessageTemplate { get; set; } = string.Empty;
    public string MinorMessageTemplate { get; set; } = string.Empty;
    
    // User relationship
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}