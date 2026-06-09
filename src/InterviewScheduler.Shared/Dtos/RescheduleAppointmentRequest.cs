namespace InterviewScheduler.Shared.Dtos;

public class RescheduleAppointmentRequest
{
    public int LeaderId { get; set; }
    public DateTime ScheduledTime { get; set; }
}
