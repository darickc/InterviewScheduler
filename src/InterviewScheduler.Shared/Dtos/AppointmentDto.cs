using InterviewScheduler.Core.Enums;

namespace InterviewScheduler.Shared.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public int LeaderId { get; set; }
    public int AppointmentTypeId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string? GoogleEventId { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }

    // Display-helpers populated server-side so the client doesn't need to round-trip
    // for related-entity names when rendering lists.
    public string ContactName { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public string AppointmentTypeName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}
