using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// System-wide configuration for scheduling rules and constraints.
/// Provides centralized management of business rules and scheduling policies.
/// </summary>
public class SchedulingConfiguration
{
    /// <summary>
    /// Default working hours for the system.
    /// </summary>
    public WorkingHours DefaultWorkingHours { get; set; } = WorkingHours.CreateStandardBusinessHours();

    /// <summary>
    /// System-wide default buffer time between appointments (in minutes).
    /// </summary>
    public int DefaultBufferTimeMinutes { get; set; } = 15;

    /// <summary>
    /// Default minimum advance booking time (in hours).
    /// </summary>
    public int DefaultMinimumAdvanceBookingHours { get; set; } = 24;

    /// <summary>
    /// Default maximum advance booking time (in days).
    /// </summary>
    public int DefaultMaximumAdvanceBookingDays { get; set; } = 90;

    /// <summary>
    /// Maximum duration allowed for any appointment (in minutes).
    /// </summary>
    public int MaximumAppointmentDurationMinutes { get; set; } = 480; // 8 hours

    /// <summary>
    /// Minimum duration allowed for any appointment (in minutes).
    /// </summary>
    public int MinimumAppointmentDurationMinutes { get; set; } = 15; // 15 minutes

    /// <summary>
    /// Whether weekend scheduling is allowed by default.
    /// </summary>
    public bool AllowWeekendSchedulingByDefault { get; set; } = false;

    /// <summary>
    /// Whether after-hours scheduling is allowed by default.
    /// </summary>
    public bool AllowAfterHoursSchedulingByDefault { get; set; } = false;

    /// <summary>
    /// Time zone for the scheduling system.
    /// </summary>
    public string SystemTimeZone { get; set; } = TimeZoneInfo.Local.Id;

    /// <summary>
    /// Holidays and blackout dates when scheduling is not allowed.
    /// </summary>
    public List<Holiday> Holidays { get; set; } = new();

    /// <summary>
    /// Recurring blackout periods (e.g., lunch breaks, maintenance windows).
    /// </summary>
    public List<RecurringBlackoutPeriod> RecurringBlackouts { get; set; } = new();

    /// <summary>
    /// Whether to allow double-booking for high-priority appointments.
    /// </summary>
    public bool AllowHighPriorityDoubleBooking { get; set; } = false;

    /// <summary>
    /// Priority threshold for double-booking (if enabled).
    /// Only appointments with priority level <= this value can double-book.
    /// </summary>
    public int DoubleBookingPriorityThreshold { get; set; } = 1;

    /// <summary>
    /// Whether to automatically suggest alternative times when requested time is unavailable.
    /// </summary>
    public bool EnableAutomaticAlternativeSuggestions { get; set; } = true;

    /// <summary>
    /// Number of days to search for alternative time slots.
    /// </summary>
    public int AlternativeSearchDays { get; set; } = 7;

    /// <summary>
    /// Whether to enforce strict validation of all scheduling rules.
    /// If false, some rules may generate warnings instead of errors.
    /// </summary>
    public bool EnforceStrictValidation { get; set; } = true;

    /// <summary>
    /// Default time slot increment for availability generation (in minutes).
    /// </summary>
    public int DefaultTimeSlotIncrementMinutes { get; set; } = 30;

    /// <summary>
    /// Creates a configuration with standard business settings.
    /// </summary>
    /// <returns>A SchedulingConfiguration with typical business hour settings.</returns>
    public static SchedulingConfiguration CreateStandardConfiguration()
    {
        return new SchedulingConfiguration
        {
            DefaultWorkingHours = WorkingHours.CreateStandardBusinessHours(),
            DefaultBufferTimeMinutes = 15,
            DefaultMinimumAdvanceBookingHours = 24,
            DefaultMaximumAdvanceBookingDays = 90,
            AllowWeekendSchedulingByDefault = false,
            AllowAfterHoursSchedulingByDefault = false,
            EnforceStrictValidation = true
        };
    }

    /// <summary>
    /// Creates a configuration with flexible scheduling settings.
    /// </summary>
    /// <returns>A SchedulingConfiguration with more permissive settings.</returns>
    public static SchedulingConfiguration CreateFlexibleConfiguration()
    {
        return new SchedulingConfiguration
        {
            DefaultWorkingHours = WorkingHours.CreateExtendedHours(),
            DefaultBufferTimeMinutes = 0, // No buffer time required
            DefaultMinimumAdvanceBookingHours = 0, // No minimum advance booking
            DefaultMaximumAdvanceBookingDays = 365, // Allow far future booking
            AllowWeekendSchedulingByDefault = true,
            AllowAfterHoursSchedulingByDefault = true,
            EnforceStrictValidation = false,
            EnableAutomaticAlternativeSuggestions = false // Disable since no restrictions
        };
    }

    /// <summary>
    /// Adds a holiday to the system blackout dates.
    /// </summary>
    /// <param name="name">The name of the holiday.</param>
    /// <param name="date">The date of the holiday.</param>
    /// <param name="isRecurring">Whether this holiday occurs annually.</param>
    public void AddHoliday(string name, DateTime date, bool isRecurring = true)
    {
        Holidays.Add(new Holiday
        {
            Name = name,
            Date = date,
            IsRecurring = isRecurring
        });
    }

    /// <summary>
    /// Adds a recurring blackout period to the system.
    /// </summary>
    /// <param name="name">The name of the blackout period.</param>
    /// <param name="startTime">The start time of the blackout period.</param>
    /// <param name="endTime">The end time of the blackout period.</param>
    /// <param name="daysOfWeek">The days of the week this blackout applies.</param>
    public void AddRecurringBlackout(string name, TimeSpan startTime, TimeSpan endTime, params DayOfWeek[] daysOfWeek)
    {
        RecurringBlackouts.Add(new RecurringBlackoutPeriod
        {
            Name = name,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeek = daysOfWeek.ToList()
        });
    }

    /// <summary>
    /// Checks if a given date is a holiday or blackout date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is a blackout date, false otherwise.</returns>
    public bool IsBlackoutDate(DateTime date)
    {
        return Holidays.Any(h => h.IsBlackoutDate(date));
    }

    /// <summary>
    /// Gets all blackout periods for a specific date, including recurring ones.
    /// </summary>
    /// <param name="date">The date to get blackout periods for.</param>
    /// <returns>A collection of blackout time periods.</returns>
    public TimePeriodCollection GetBlackoutPeriods(DateTime date)
    {
        var blackouts = new TimePeriodCollection();

        // Add recurring blackouts that apply to this day of the week
        foreach (var blackout in RecurringBlackouts.Where(b => b.AppliesOnDate(date)))
        {
            var blackoutPeriod = new TimeRange(
                date.Date.Add(blackout.StartTime),
                date.Date.Add(blackout.EndTime)
            );
            blackouts.Add(blackoutPeriod);
        }

        return blackouts;
    }

    /// <summary>
    /// Removes all recurring blackout periods.
    /// </summary>
    public void ClearRecurringBlackouts()
    {
        RecurringBlackouts.Clear();
    }

    /// <summary>
    /// Removes a specific recurring blackout period by name.
    /// </summary>
    /// <param name="name">The name of the blackout period to remove.</param>
    /// <returns>True if the blackout was found and removed, false otherwise.</returns>
    public bool RemoveRecurringBlackout(string name)
    {
        var blackoutToRemove = RecurringBlackouts.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (blackoutToRemove != null)
        {
            RecurringBlackouts.Remove(blackoutToRemove);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all holidays from the blackout dates.
    /// </summary>
    public void ClearHolidays()
    {
        Holidays.Clear();
    }

    /// <summary>
    /// Removes a specific holiday by name.
    /// </summary>
    /// <param name="name">The name of the holiday to remove.</param>
    /// <returns>True if the holiday was found and removed, false otherwise.</returns>
    public bool RemoveHoliday(string name)
    {
        var holidayToRemove = Holidays.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (holidayToRemove != null)
        {
            Holidays.Remove(holidayToRemove);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Creates a configuration with no blackout periods for testing or flexible scheduling.
    /// </summary>
    /// <returns>A SchedulingConfiguration with no blocked periods.</returns>
    public static SchedulingConfiguration CreateUnrestrictedConfiguration()
    {
        return new SchedulingConfiguration
        {
            DefaultWorkingHours = WorkingHours.CreateExtendedHours(),
            DefaultBufferTimeMinutes = 0,
            DefaultMinimumAdvanceBookingHours = 0,
            DefaultMaximumAdvanceBookingDays = 365,
            AllowWeekendSchedulingByDefault = true,
            AllowAfterHoursSchedulingByDefault = true,
            EnforceStrictValidation = false,
            // No holidays or recurring blackouts
            Holidays = new List<Holiday>(),
            RecurringBlackouts = new List<RecurringBlackoutPeriod>()
        };
    }
}

/// <summary>
/// Represents a holiday or special blackout date.
/// </summary>
public class Holiday
{
    /// <summary>
    /// The name of the holiday.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The date of the holiday.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Whether this holiday occurs annually on the same date.
    /// </summary>
    public bool IsRecurring { get; set; } = true;

    /// <summary>
    /// Checks if this holiday applies to the given date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if this holiday applies to the date, false otherwise.</returns>
    public bool IsBlackoutDate(DateTime date)
    {
        if (IsRecurring)
        {
            return Date.Month == date.Month && Date.Day == date.Day;
        }
        else
        {
            return Date.Date == date.Date;
        }
    }
}

/// <summary>
/// Represents a recurring blackout period (e.g., lunch break, maintenance window).
/// </summary>
public class RecurringBlackoutPeriod
{
    /// <summary>
    /// The name of the blackout period.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The start time of the blackout period.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// The end time of the blackout period.
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// The days of the week this blackout period applies.
    /// </summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// Checks if this blackout period applies on the given date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if this blackout applies on the date, false otherwise.</returns>
    public bool AppliesOnDate(DateTime date)
    {
        return DaysOfWeek.Contains(date.DayOfWeek);
    }
}