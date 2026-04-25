namespace InterviewScheduler.Shared.Dtos;

public class AppointmentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public string MinorMessageTemplate { get; set; } = string.Empty;

    public int BufferTimeBeforeMinutes { get; set; }
    public int BufferTimeAfterMinutes { get; set; }
    public int MinimumDurationMinutes { get; set; }
    public int MaximumDurationMinutes { get; set; }
    public int MinimumAdvanceBookingHours { get; set; }
    public int MaximumAdvanceBookingDays { get; set; }
    public int SchedulingPriority { get; set; } = 5;
    public bool RequireStrictBufferTime { get; set; }
    public bool AllowWeekendScheduling { get; set; } = true;
    public bool AllowAfterHoursScheduling { get; set; } = true;
    public string ColorCode { get; set; } = "#007bff";
}
