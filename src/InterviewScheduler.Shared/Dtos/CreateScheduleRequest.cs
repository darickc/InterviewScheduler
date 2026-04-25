namespace InterviewScheduler.Shared.Dtos;

public class CreateScheduleRequest
{
    public int LeaderId { get; set; }
    public int AppointmentTypeId { get; set; }
    public List<int> ContactIds { get; set; } = new();
    public DateTime DateRangeStart { get; set; }
    public DateTime DateRangeEnd { get; set; }
}
