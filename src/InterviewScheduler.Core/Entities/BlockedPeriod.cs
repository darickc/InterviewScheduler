using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// Represents a blocked time period with metadata about why it's blocked.
/// </summary>
public class BlockedPeriod : TimeRange
{
    /// <summary>
    /// The reason why this period is blocked.
    /// </summary>
    public BlockedPeriodReason Reason { get; set; }

    /// <summary>
    /// Additional description of the blocked period.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The source of the blocked period (e.g., "Working Hours", "Holiday", "Recurring Blackout").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new blocked period.
    /// </summary>
    public BlockedPeriod(DateTime start, DateTime end, BlockedPeriodReason reason, string description = "", string source = "")
        : base(start, end)
    {
        Reason = reason;
        Description = description;
        Source = source;
    }

    /// <summary>
    /// Gets a user-friendly description of this blocked period.
    /// </summary>
    public string GetFriendlyDescription()
    {
        var timeRange = $"{Start:h:mm tt} - {End:h:mm tt}";
        
        return Reason switch
        {
            BlockedPeriodReason.OutsideWorkingHours => $"Outside working hours ({timeRange})",
            BlockedPeriodReason.LunchBreak => $"Lunch break ({timeRange})",
            BlockedPeriodReason.Holiday => $"Holiday: {Description} ({Start:MMM d})",
            BlockedPeriodReason.RecurringBlackout => $"{Description} ({timeRange})",
            BlockedPeriodReason.Weekend => $"Weekend ({Start:dddd, MMM d})",
            BlockedPeriodReason.CustomBlackout => $"Blocked period: {Description} ({timeRange})",
            _ => $"Blocked period ({timeRange})"
        };
    }
}

/// <summary>
/// Enum representing different reasons why a time period might be blocked.
/// </summary>
public enum BlockedPeriodReason
{
    /// <summary>
    /// The time falls outside of configured working hours.
    /// </summary>
    OutsideWorkingHours,

    /// <summary>
    /// The time falls during a lunch break or similar gap between work sessions.
    /// </summary>
    LunchBreak,

    /// <summary>
    /// The time falls on a holiday.
    /// </summary>
    Holiday,

    /// <summary>
    /// The time falls during a recurring blackout period (e.g., weekly maintenance).
    /// </summary>
    RecurringBlackout,

    /// <summary>
    /// The time falls on a weekend.
    /// </summary>
    Weekend,

    /// <summary>
    /// The time falls during a custom blackout period.
    /// </summary>
    CustomBlackout
}