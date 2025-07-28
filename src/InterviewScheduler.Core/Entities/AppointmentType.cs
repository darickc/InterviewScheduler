namespace InterviewScheduler.Core.Entities;

public class AppointmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; } // in minutes
    public string MessageTemplate { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}