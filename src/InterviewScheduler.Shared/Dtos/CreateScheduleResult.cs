namespace InterviewScheduler.Shared.Dtos;

public class CreateScheduleResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AppointmentsCreated { get; set; }
    public int CalendarEventsCreated { get; set; }
    public List<string> UnscheduledContacts { get; set; } = new();
    public List<AppointmentDto> Appointments { get; set; } = new();
}
