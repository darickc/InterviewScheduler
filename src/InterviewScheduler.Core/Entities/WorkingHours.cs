using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// Represents the working hours configuration for scheduling appointments.
/// Defines available time slots, break periods, and business rules.
/// </summary>
public class WorkingHours
{
    /// <summary>
    /// The morning session time range (e.g., 9:00 AM - 12:00 PM).
    /// </summary>
    public TimeRange? MorningSession { get; set; }

    /// <summary>
    /// The afternoon session time range (e.g., 1:00 PM - 5:00 PM).
    /// </summary>
    public TimeRange? AfternoonSession { get; set; }

    /// <summary>
    /// Collection of break times and unavailable periods during the day.
    /// </summary>
    public TimePeriodCollection BreakTimes { get; set; } = new();

    /// <summary>
    /// The days of the week when appointments are available.
    /// </summary>
    public List<DayOfWeek> AvailableDays { get; set; } = new();

    /// <summary>
    /// The time zone for these working hours.
    /// </summary>
    public string TimeZone { get; set; } = TimeZoneInfo.Local.Id;

    /// <summary>
    /// Buffer time required between appointments (in minutes).
    /// </summary>
    public int BufferTimeMinutes { get; set; } = 0;

    /// <summary>
    /// Minimum advance booking time required (in hours).
    /// </summary>
    public int MinimumAdvanceHours { get; set; } = 24;

    /// <summary>
    /// Maximum advance booking time allowed (in days).
    /// </summary>
    public int MaximumAdvanceDays { get; set; } = 90;

    /// <summary>
    /// Initializes a new instance of WorkingHours with default business hours.
    /// </summary>
    public WorkingHours()
    {
        SetDefaultBusinessHours();
    }

    /// <summary>
    /// Sets default business hours (24/7 unrestricted availability).
    /// </summary>
    private void SetDefaultBusinessHours()
    {
        var today = DateTime.Today;
        
        // Set to full 24-hour availability with no gaps
        MorningSession = new TimeRange(
            today,              // 12:00 AM
            today.AddHours(24)  // 11:59 PM (end of day)
        );
        
        // No afternoon session needed since morning covers full day
        AfternoonSession = null;

        // Include all 7 days of the week
        AvailableDays = new List<DayOfWeek>
        {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
        };
    }

    /// <summary>
    /// Gets all available time slots for a specific date.
    /// </summary>
    /// <param name="date">The date to get available slots for.</param>
    /// <returns>A collection of available time periods.</returns>
    public TimePeriodCollection GetAvailableSlots(DateTime date)
    {
        var availableSlots = new TimePeriodCollection();

        // Check if the day is available
        if (!AvailableDays.Contains(date.DayOfWeek))
        {
            return availableSlots;
        }

        // Add morning session if available
        if (MorningSession != null)
        {
            var morningSlot = new TimeRange(
                date.Date.Add(MorningSession.Start.TimeOfDay),
                date.Date.Add(MorningSession.End.TimeOfDay)
            );
            availableSlots.Add(morningSlot);
        }

        // Add afternoon session if available
        if (AfternoonSession != null)
        {
            var afternoonSlot = new TimeRange(
                date.Date.Add(AfternoonSession.Start.TimeOfDay),
                date.Date.Add(AfternoonSession.End.TimeOfDay)
            );
            availableSlots.Add(afternoonSlot);
        }

        // Remove break times
        foreach (var breakTime in BreakTimes)
        {
            var dailyBreak = new TimeRange(
                date.Date.Add(breakTime.Start.TimeOfDay),
                date.Date.Add(breakTime.End.TimeOfDay)
            );
            
            // Remove intersections with break times
            var updatedSlots = new TimePeriodCollection();
            foreach (var slot in availableSlots)
            {
                if (!slot.IntersectsWith(dailyBreak))
                {
                    updatedSlots.Add(slot);
                }
                else
                {
                    // Split the slot around the break time
                    if (slot.Start < dailyBreak.Start)
                    {
                        updatedSlots.Add(new TimeRange(slot.Start, dailyBreak.Start));
                    }
                    if (slot.End > dailyBreak.End)
                    {
                        updatedSlots.Add(new TimeRange(dailyBreak.End, slot.End));
                    }
                }
            }
            availableSlots = updatedSlots;
        }

        return availableSlots;
    }

    /// <summary>
    /// Checks if a time period falls within working hours.
    /// </summary>
    /// <param name="timePeriod">The time period to check.</param>
    /// <returns>True if the time period is within working hours, false otherwise.</returns>
    public bool IsWithinWorkingHours(ITimePeriod timePeriod)
    {
        var date = timePeriod.Start.Date;
        var availableSlots = GetAvailableSlots(date);
        
        return availableSlots.Any(slot => slot.HasInside(timePeriod));
    }

    /// <summary>
    /// Checks if an appointment time is valid based on advance booking rules.
    /// </summary>
    /// <param name="appointmentTime">The proposed appointment time.</param>
    /// <returns>True if the appointment time meets advance booking requirements, false otherwise.</returns>
    public bool IsValidAdvanceBooking(DateTime appointmentTime)
    {
        var now = DateTime.Now;
        var timeDifference = appointmentTime - now;

        // Check minimum advance time
        if (timeDifference.TotalHours < MinimumAdvanceHours)
        {
            return false;
        }

        // Check maximum advance time
        if (timeDifference.TotalDays > MaximumAdvanceDays)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adds a break time to the working hours.
    /// </summary>
    /// <param name="startTime">The start time of the break.</param>
    /// <param name="endTime">The end time of the break.</param>
    public void AddBreakTime(TimeSpan startTime, TimeSpan endTime)
    {
        var today = DateTime.Today;
        var breakTime = new TimeRange(
            today.Add(startTime),
            today.Add(endTime)
        );
        BreakTimes.Add(breakTime);
    }

    /// <summary>
    /// Creates working hours for standard business hours (9 AM - 5 PM, weekdays).
    /// </summary>
    /// <returns>A WorkingHours instance configured for standard business hours.</returns>
    public static WorkingHours CreateStandardBusinessHours()
    {
        return new WorkingHours();
    }

    /// <summary>
    /// Creates working hours for extended hours (24/7, all days).
    /// </summary>
    /// <returns>A WorkingHours instance configured for unrestricted hours.</returns>
    public static WorkingHours CreateExtendedHours()
    {
        var workingHours = new WorkingHours();
        var today = DateTime.Today;
        
        // Set to 24-hour availability
        workingHours.MorningSession = new TimeRange(
            today,              // 12:00 AM
            today.AddHours(24)  // 11:59 PM
        );
        
        // Clear afternoon session since we're using full day
        workingHours.AfternoonSession = null;
        
        // Include all 7 days of the week
        workingHours.AvailableDays = new List<DayOfWeek>
        {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
        };

        return workingHours;
    }

    /// <summary>
    /// Returns a string representation of the working hours configuration.
    /// </summary>
    /// <returns>A string describing the working hours.</returns>
    public override string ToString()
    {
        var days = string.Join(", ", AvailableDays.Select(d => d.ToString()));
        var morning = MorningSession?.ToString() ?? "Not set";
        var afternoon = AfternoonSession?.ToString() ?? "Not set";
        
        return $"Working Days: {days}, Morning: {morning}, Afternoon: {afternoon}, Breaks: {BreakTimes.Count}";
    }
}