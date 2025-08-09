namespace InterviewScheduler.Core.Entities;

public class AppointmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; } // in minutes
    public string MessageTemplate { get; set; } = string.Empty;
    public string MinorMessageTemplate { get; set; } = string.Empty;
    
    // User relationship
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Scheduling-specific properties for Phase 4 enhancement
    
    /// <summary>
    /// Buffer time required before this appointment type (in minutes).
    /// </summary>
    public int BufferTimeBeforeMinutes { get; set; } = 0;
    
    /// <summary>
    /// Buffer time required after this appointment type (in minutes).
    /// </summary>
    public int BufferTimeAfterMinutes { get; set; } = 0;
    
    /// <summary>
    /// Minimum duration allowed for this appointment type (in minutes).
    /// If 0, uses the default Duration value.
    /// </summary>
    public int MinimumDurationMinutes { get; set; } = 0;
    
    /// <summary>
    /// Maximum duration allowed for this appointment type (in minutes).
    /// If 0, no maximum limit is enforced.
    /// </summary>
    public int MaximumDurationMinutes { get; set; } = 0;
    
    /// <summary>
    /// Minimum advance booking time required for this appointment type (in hours).
    /// If 0, uses system default advance booking rules.
    /// </summary>
    public int MinimumAdvanceBookingHours { get; set; } = 0;
    
    /// <summary>
    /// Maximum advance booking time allowed for this appointment type (in days).
    /// If 0, uses system default advance booking rules.
    /// </summary>
    public int MaximumAdvanceBookingDays { get; set; } = 0;
    
    /// <summary>
    /// Priority level for scheduling conflicts (1 = highest, 10 = lowest).
    /// Higher priority appointments can potentially override lower priority ones.
    /// </summary>
    public int SchedulingPriority { get; set; } = 5;
    
    /// <summary>
    /// Whether this appointment type requires strict buffer time enforcement.
    /// If true, buffer time violations will block scheduling.
    /// If false, buffer time violations will generate warnings only.
    /// </summary>
    public bool RequireStrictBufferTime { get; set; } = false;
    
    /// <summary>
    /// Whether this appointment type can be scheduled on weekends.
    /// </summary>
    public bool AllowWeekendScheduling { get; set; } = true;
    
    /// <summary>
    /// Whether this appointment type can be scheduled outside normal working hours.
    /// </summary>
    public bool AllowAfterHoursScheduling { get; set; } = true;
    
    /// <summary>
    /// Color code for calendar display (hex format, e.g., "#FF5733").
    /// </summary>
    public string ColorCode { get; set; } = "#007bff";
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    
    // Computed properties for convenience
    
    /// <summary>
    /// Gets the total buffer time (before + after) in minutes.
    /// </summary>
    public int TotalBufferTimeMinutes => BufferTimeBeforeMinutes + BufferTimeAfterMinutes;
    
    /// <summary>
    /// Gets the effective minimum duration (uses MinimumDurationMinutes if set, otherwise Duration).
    /// </summary>
    public int EffectiveMinimumDuration => MinimumDurationMinutes > 0 ? MinimumDurationMinutes : Duration;
    
    /// <summary>
    /// Gets the effective maximum duration (uses MaximumDurationMinutes if set, otherwise no limit).
    /// </summary>
    public int? EffectiveMaximumDuration => MaximumDurationMinutes > 0 ? MaximumDurationMinutes : null;
    
    /// <summary>
    /// Checks if this appointment type has any buffer time requirements.
    /// </summary>
    public bool HasBufferTimeRequirements => BufferTimeBeforeMinutes > 0 || BufferTimeAfterMinutes > 0;
    
    /// <summary>
    /// Checks if this appointment type has custom advance booking rules.
    /// </summary>
    public bool HasCustomAdvanceBookingRules => MinimumAdvanceBookingHours > 0 || MaximumAdvanceBookingDays > 0;
}