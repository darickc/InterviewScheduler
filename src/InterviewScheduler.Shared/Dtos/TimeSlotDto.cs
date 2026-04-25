namespace InterviewScheduler.Shared.Dtos;

public class TimeSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
}
