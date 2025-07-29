namespace InterviewScheduler.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string GoogleUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Leader> Leaders { get; set; } = new List<Leader>();
    public ICollection<AppointmentType> AppointmentTypes { get; set; } = new List<AppointmentType>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}