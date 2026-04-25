namespace InterviewScheduler.Shared.Dtos;

public class LeaderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string GoogleCalendarId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
