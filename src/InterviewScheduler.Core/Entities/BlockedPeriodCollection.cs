using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// A collection of blocked periods with enhanced metadata and search capabilities.
/// </summary>
public class BlockedPeriodCollection : List<BlockedPeriod>
{
    /// <summary>
    /// Finds all blocked periods that intersect with the given time range.
    /// </summary>
    public List<BlockedPeriod> GetConflictingPeriods(TimeRange timeRange)
    {
        return this.Where(bp => bp.IntersectsWith(timeRange)).ToList();
    }

    /// <summary>
    /// Gets a human-readable description of all blocking reasons for a given time range.
    /// </summary>
    public string GetConflictDescription(TimeRange timeRange)
    {
        var conflicts = GetConflictingPeriods(timeRange);
        
        if (!conflicts.Any())
            return string.Empty;

        if (conflicts.Count == 1)
        {
            return conflicts.First().GetFriendlyDescription();
        }

        var descriptions = conflicts.Select(c => c.GetFriendlyDescription()).ToList();
        return $"Multiple conflicts: {string.Join("; ", descriptions)}";
    }

    /// <summary>
    /// Adds a blocked period based on TimePeriod with minimal metadata.
    /// </summary>
    public void AddFromTimePeriod(ITimePeriod period, BlockedPeriodReason reason, string description = "", string source = "")
    {
        Add(new BlockedPeriod(period.Start, period.End, reason, description, source));
    }

    /// <summary>
    /// Converts this collection to a standard TimePeriodCollection for compatibility.
    /// </summary>
    public TimePeriodCollection ToTimePeriodCollection()
    {
        var collection = new TimePeriodCollection();
        foreach (var blockedPeriod in this)
        {
            collection.Add(new TimeRange(blockedPeriod.Start, blockedPeriod.End));
        }
        return collection;
    }
}