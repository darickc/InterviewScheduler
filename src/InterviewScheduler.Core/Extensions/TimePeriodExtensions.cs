using Itenso.TimePeriod;
using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Extensions;

/// <summary>
/// Extension methods for working with TimePeriodLibrary classes and DateTime objects.
/// </summary>
public static class TimePeriodExtensions
{
    /// <summary>
    /// Creates a TimeRange from a start DateTime and duration in minutes.
    /// </summary>
    /// <param name="startTime">The start time.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    /// <returns>A new TimeRange instance.</returns>
    public static TimeRange ToTimeRange(this DateTime startTime, int durationMinutes)
    {
        var endTime = startTime.AddMinutes(durationMinutes);
        return new TimeRange(startTime, endTime);
    }

    /// <summary>
    /// Creates a TimeRange from a start DateTime and duration TimeSpan.
    /// </summary>
    /// <param name="startTime">The start time.</param>
    /// <param name="duration">The duration as a TimeSpan.</param>
    /// <returns>A new TimeRange instance.</returns>
    public static TimeRange ToTimeRange(this DateTime startTime, TimeSpan duration)
    {
        var endTime = startTime.Add(duration);
        return new TimeRange(startTime, endTime);
    }

    /// <summary>
    /// Creates a TimeRange for a specific time on a given date.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="startTime">The start time (time of day).</param>
    /// <param name="endTime">The end time (time of day).</param>
    /// <returns>A new TimeRange instance.</returns>
    public static TimeRange ToTimeRange(this DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var start = date.Date.Add(startTime);
        var end = date.Date.Add(endTime);
        return new TimeRange(start, end);
    }

    /// <summary>
    /// Checks if a DateTime falls within a time period.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <param name="timePeriod">The time period to check against.</param>
    /// <returns>True if the DateTime is within the time period, false otherwise.</returns>
    public static bool IsWithin(this DateTime dateTime, ITimePeriod timePeriod)
    {
        return timePeriod.HasInside(dateTime);
    }

    /// <summary>
    /// Gets the intersection of two time periods.
    /// </summary>
    /// <param name="timePeriod">The first time period.</param>
    /// <param name="other">The second time period.</param>
    /// <returns>The intersection time period, or null if no intersection exists.</returns>
    public static ITimePeriod? GetOverlap(this ITimePeriod timePeriod, ITimePeriod other)
    {
        if (!timePeriod.IntersectsWith(other))
            return null;
            
        var start = timePeriod.Start > other.Start ? timePeriod.Start : other.Start;
        var end = timePeriod.End < other.End ? timePeriod.End : other.End;
        return new TimeRange(start, end);
    }

    /// <summary>
    /// Checks if a time period falls entirely within working hours.
    /// </summary>
    /// <param name="timePeriod">The time period to check.</param>
    /// <param name="workingHours">The working hours configuration.</param>
    /// <returns>True if the time period is within working hours, false otherwise.</returns>
    public static bool IsWithinWorkingHours(this ITimePeriod timePeriod, WorkingHours workingHours)
    {
        return workingHours.IsWithinWorkingHours(timePeriod);
    }

    /// <summary>
    /// Gets all conflicts between a time period and a collection of appointments.
    /// </summary>
    /// <param name="timePeriod">The time period to check.</param>
    /// <param name="appointments">The appointments to check against.</param>
    /// <returns>A list of conflicting appointments.</returns>
    public static List<Appointment> GetConflicts(this ITimePeriod timePeriod, IEnumerable<Appointment> appointments)
    {
        return appointments.Where(apt => apt.ConflictsWith(timePeriod)).ToList();
    }

    /// <summary>
    /// Gets all conflicts between a time period and a collection of time slots for a specific leader.
    /// </summary>
    /// <param name="timePeriod">The time period to check.</param>
    /// <param name="timeSlots">The time slots to check against.</param>
    /// <param name="leaderId">The leader ID to filter by (optional).</param>
    /// <returns>A list of conflicting time slots.</returns>
    public static List<TimeSlot> GetConflicts(this ITimePeriod timePeriod, IEnumerable<TimeSlot> timeSlots, int? leaderId = null)
    {
        var slotsToCheck = leaderId.HasValue 
            ? timeSlots.Where(slot => slot.LeaderId == leaderId.Value)
            : timeSlots;
            
        return slotsToCheck.Where(slot => slot.ConflictsWith(timePeriod)).ToList();
    }

    /// <summary>
    /// Splits a time period into smaller time slots of a specified duration.
    /// </summary>
    /// <param name="timePeriod">The time period to split.</param>
    /// <param name="slotDurationMinutes">The duration of each slot in minutes.</param>
    /// <returns>A list of time ranges representing the slots.</returns>
    public static List<TimeRange> SplitIntoSlots(this ITimePeriod timePeriod, int slotDurationMinutes)
    {
        var slots = new List<TimeRange>();
        var slotDuration = TimeSpan.FromMinutes(slotDurationMinutes);
        var current = timePeriod.Start;

        while (current.Add(slotDuration) <= timePeriod.End)
        {
            slots.Add(new TimeRange(current, current.Add(slotDuration)));
            current = current.Add(slotDuration);
        }

        return slots;
    }

    /// <summary>
    /// Combines multiple time periods into a single collection, merging overlapping periods.
    /// </summary>
    /// <param name="timePeriods">The time periods to combine.</param>
    /// <returns>A TimePeriodCollection with merged periods.</returns>
    public static TimePeriodCollection CombineAndMerge(this IEnumerable<ITimePeriod> timePeriods)
    {
        var collection = new TimePeriodCollection();
        collection.AddAll(timePeriods);
        
        // The TimePeriodLibrary automatically handles merging of overlapping periods
        return collection;
    }

    /// <summary>
    /// Finds gaps between time periods within a specified time range.
    /// </summary>
    /// <param name="timePeriods">The existing time periods.</param>
    /// <param name="searchRange">The range to search for gaps within.</param>
    /// <returns>A collection of time periods representing the gaps.</returns>
    public static TimePeriodCollection FindGaps(this IEnumerable<ITimePeriod> timePeriods, ITimePeriod searchRange)
    {
        var gaps = new TimePeriodCollection();
        var sortedPeriods = timePeriods
            .Where(p => p.IntersectsWith(searchRange))
            .OrderBy(p => p.Start)
            .ToList();

        if (!sortedPeriods.Any())
        {
            gaps.Add(new TimeRange(searchRange.Start, searchRange.End));
            return gaps;
        }

        var current = searchRange.Start;
        
        foreach (var period in sortedPeriods)
        {
            if (current < period.Start)
            {
                gaps.Add(new TimeRange(current, period.Start));
            }
            current = period.End > current ? period.End : current;
        }

        if (current < searchRange.End)
        {
            gaps.Add(new TimeRange(current, searchRange.End));
        }

        return gaps;
    }

    /// <summary>
    /// Converts a collection of appointments to AppointmentTimeRange objects.
    /// </summary>
    /// <param name="appointments">The appointments to convert.</param>
    /// <returns>A list of AppointmentTimeRange objects.</returns>
    public static List<AppointmentTimeRange> ToAppointmentTimeRanges(this IEnumerable<Appointment> appointments)
    {
        return appointments.Select(apt => apt.AppointmentTimeRange).ToList();
    }

    /// <summary>
    /// Filters appointments by a specific date.
    /// </summary>
    /// <param name="appointments">The appointments to filter.</param>
    /// <param name="date">The date to filter by.</param>
    /// <returns>Appointments scheduled on the specified date.</returns>
    public static IEnumerable<Appointment> ForDate(this IEnumerable<Appointment> appointments, DateTime date)
    {
        return appointments.Where(apt => apt.ScheduledTime.Date == date.Date);
    }

    /// <summary>
    /// Filters appointments by a specific leader.
    /// </summary>
    /// <param name="appointments">The appointments to filter.</param>
    /// <param name="leaderId">The leader ID to filter by.</param>
    /// <returns>Appointments for the specified leader.</returns>
    public static IEnumerable<Appointment> ForLeader(this IEnumerable<Appointment> appointments, int leaderId)
    {
        return appointments.Where(apt => apt.LeaderId == leaderId);
    }

    /// <summary>
    /// Filters appointments by a date range.
    /// </summary>
    /// <param name="appointments">The appointments to filter.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <returns>Appointments within the specified date range.</returns>
    public static IEnumerable<Appointment> InDateRange(this IEnumerable<Appointment> appointments, DateTime startDate, DateTime endDate)
    {
        return appointments.Where(apt => apt.ScheduledTime >= startDate && apt.ScheduledTime <= endDate);
    }

    /// <summary>
    /// Formats a time period as a user-friendly string.
    /// </summary>
    /// <param name="timePeriod">The time period to format.</param>
    /// <returns>A formatted string representation.</returns>
    public static string ToFriendlyString(this ITimePeriod timePeriod)
    {
        var start = timePeriod.Start.ToString("MM/dd/yyyy h:mm tt");
        var end = timePeriod.End.ToString("h:mm tt");
        
        // If same date, only show time for end
        if (timePeriod.Start.Date == timePeriod.End.Date)
        {
            return $"{start} - {end}";
        }
        
        // Different dates, show full date/time for both
        end = timePeriod.End.ToString("MM/dd/yyyy h:mm tt");
        return $"{start} - {end}";
    }

    /// <summary>
    /// Gets the total duration of multiple time periods.
    /// </summary>
    /// <param name="timePeriods">The time periods to sum.</param>
    /// <returns>The total duration as a TimeSpan.</returns>
    public static TimeSpan GetTotalDuration(this IEnumerable<ITimePeriod> timePeriods)
    {
        return timePeriods.Aggregate(TimeSpan.Zero, (total, period) => total.Add(period.Duration));
    }
}