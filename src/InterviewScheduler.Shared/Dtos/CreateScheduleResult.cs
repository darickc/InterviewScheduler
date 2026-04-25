namespace InterviewScheduler.Shared.Dtos;

public class CreateScheduleResult
{
    public List<AppointmentDto> CreatedAppointments { get; set; } = new();
    public List<ScheduleFailure> Failures { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class ScheduleFailure
{
    public int ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
