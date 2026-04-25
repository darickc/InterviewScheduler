namespace InterviewScheduler.Shared.Dtos;

public class CalendarInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TimeZone { get; set; } = string.Empty;
}
