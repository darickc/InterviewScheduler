namespace InterviewScheduler.Core.Entities;

public class Leader
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // Bishop, 1st Counselor, 2nd Counselor
    public string GoogleCalendarId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // User relationship
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}