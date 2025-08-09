using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// Represents a time slot for appointment scheduling.
/// </summary>
public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the time range for this slot using TimePeriodLibrary.
    /// </summary>
    public TimeRange TimeRange
    {
        get => new TimeRange(StartTime, EndTime);
    }
    
    /// <summary>
    /// Initializes a new TimeSlot.
    /// </summary>
    public TimeSlot()
    {
    }
    
    /// <summary>
    /// Initializes a new TimeSlot with the specified parameters.
    /// </summary>
    /// <param name="startTime">The start time of the slot.</param>
    /// <param name="endTime">The end time of the slot.</param>
    /// <param name="isAvailable">Whether the slot is available.</param>
    /// <param name="leaderId">The ID of the leader.</param>
    /// <param name="leaderName">The name of the leader.</param>
    public TimeSlot(DateTime startTime, DateTime endTime, bool isAvailable, int leaderId, string leaderName)
    {
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = isAvailable;
        LeaderId = leaderId;
        LeaderName = leaderName;
    }
    
    /// <summary>
    /// Creates a TimeSlot from a TimeRange.
    /// </summary>
    /// <param name="timeRange">The time range to create the slot from.</param>
    /// <param name="isAvailable">Whether the slot is available.</param>
    /// <param name="leaderId">The ID of the leader.</param>
    /// <param name="leaderName">The name of the leader.</param>
    /// <returns>A new TimeSlot instance.</returns>
    public static TimeSlot FromTimeRange(ITimePeriod timeRange, bool isAvailable, int leaderId, string leaderName)
    {
        return new TimeSlot(timeRange.Start, timeRange.End, isAvailable, leaderId, leaderName);
    }
    
    /// <summary>
    /// Checks if this time slot conflicts with another time slot.
    /// </summary>
    /// <param name="other">The other time slot to check against.</param>
    /// <returns>True if the slots overlap, false otherwise.</returns>
    public bool ConflictsWith(TimeSlot other)
    {
        if (other == null) return false;
        return TimeRange.IntersectsWith(other.TimeRange);
    }
    
    /// <summary>
    /// Checks if this time slot conflicts with a time period.
    /// </summary>
    /// <param name="timePeriod">The time period to check against.</param>
    /// <returns>True if the slot overlaps with the time period, false otherwise.</returns>
    public bool ConflictsWith(ITimePeriod timePeriod)
    {
        return TimeRange.IntersectsWith(timePeriod);
    }
    
    /// <summary>
    /// Gets the duration of this time slot.
    /// </summary>
    public TimeSpan Duration => TimeRange.Duration;
    
    /// <summary>
    /// Returns a string representation of the time slot.
    /// </summary>
    /// <returns>A string describing the time slot.</returns>
    public override string ToString()
    {
        var status = IsAvailable ? "Available" : "Unavailable";
        return $"{LeaderName} ({LeaderId}): {TimeRange} - {status}";
    }
}