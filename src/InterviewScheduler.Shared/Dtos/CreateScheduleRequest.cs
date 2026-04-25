namespace InterviewScheduler.Shared.Dtos;

public class CreateScheduleRequest
{
    public DateTime Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int AppointmentTypeId { get; set; }
    public List<int> LeaderIds { get; set; } = new();
    public List<int> ContactIds { get; set; } = new();
}
