using Itenso.TimePeriod;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Extensions;

namespace InterviewScheduler.Core.Helpers;

/// <summary>
/// Provides validation methods for time periods and appointments.
/// </summary>
public static class TimePeriodValidationHelper
{
    /// <summary>
    /// Validates if an appointment can be scheduled at the specified time.
    /// </summary>
    /// <param name="startTime">The proposed start time.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    /// <param name="leaderId">The leader ID.</param>
    /// <param name="existingAppointments">Existing appointments to check for conflicts.</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <returns>A validation result with success status and any error messages.</returns>
    public static ValidationResult ValidateAppointmentTime(
        DateTime startTime,
        int durationMinutes,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null)
    {
        var result = new ValidationResult();
        var proposedTimeRange = startTime.ToTimeRange(durationMinutes);

        // Basic validation
        if (startTime < DateTime.Now)
        {
            result.AddError("Cannot schedule appointments in the past.");
        }

        if (durationMinutes <= 0)
        {
            result.AddError("Appointment duration must be greater than zero.");
        }

        if (durationMinutes > 480) // 8 hours
        {
            result.AddError("Appointment duration cannot exceed 8 hours.");
        }

        // Working hours validation
        if (workingHours != null)
        {
            if (!proposedTimeRange.IsWithinWorkingHours(workingHours))
            {
                result.AddError("Appointment is outside of working hours.");
            }

            if (!workingHours.IsValidAdvanceBooking(startTime))
            {
                var minHours = workingHours.MinimumAdvanceHours;
                var maxDays = workingHours.MaximumAdvanceDays;
                result.AddError($"Appointment must be scheduled between {minHours} hours and {maxDays} days in advance.");
            }

            if (!workingHours.AvailableDays.Contains(startTime.DayOfWeek))
            {
                result.AddError($"Appointments are not available on {startTime.DayOfWeek}s.");
            }
        }

        // Conflict validation
        var conflicts = proposedTimeRange.GetConflicts(existingAppointments.ForLeader(leaderId));
        if (conflicts.Any())
        {
            result.AddError($"Time slot conflicts with {conflicts.Count} existing appointment(s).");
            result.ConflictingAppointments = conflicts;
        }

        // Buffer time validation (if working hours specify buffer time)
        if (workingHours != null && workingHours.BufferTimeMinutes > 0)
        {
            var bufferTime = TimeSpan.FromMinutes(workingHours.BufferTimeMinutes);
            var bufferedRange = new TimeRange(
                proposedTimeRange.Start.Subtract(bufferTime),
                proposedTimeRange.End.Add(bufferTime)
            );

            var nearbyAppointments = bufferedRange.GetConflicts(existingAppointments.ForLeader(leaderId));
            if (nearbyAppointments.Any())
            {
                result.AddWarning($"Appointment is within {workingHours.BufferTimeMinutes} minutes of another appointment.");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates if a time slot is available for booking.
    /// </summary>
    /// <param name="timeSlot">The time slot to validate.</param>
    /// <param name="existingAppointments">Existing appointments to check against.</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <returns>A validation result with success status and any error messages.</returns>
    public static ValidationResult ValidateTimeSlot(
        TimeSlot timeSlot,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null)
    {
        var result = new ValidationResult();

        if (timeSlot == null)
        {
            result.AddError("Time slot cannot be null.");
            return result;
        }

        if (timeSlot.StartTime >= timeSlot.EndTime)
        {
            result.AddError("Start time must be before end time.");
        }

        if (!timeSlot.IsAvailable)
        {
            result.AddError("Time slot is marked as unavailable.");
        }

        // Validate using the appointment validation logic
        var durationMinutes = (int)timeSlot.Duration.TotalMinutes;
        var appointmentValidation = ValidateAppointmentTime(
            timeSlot.StartTime,
            durationMinutes,
            timeSlot.LeaderId,
            existingAppointments,
            workingHours);

        result.Merge(appointmentValidation);

        return result;
    }

    /// <summary>
    /// Validates working hours configuration.
    /// </summary>
    /// <param name="workingHours">The working hours to validate.</param>
    /// <returns>A validation result with success status and any error messages.</returns>
    public static ValidationResult ValidateWorkingHours(WorkingHours workingHours)
    {
        var result = new ValidationResult();

        if (workingHours == null)
        {
            result.AddError("Working hours configuration cannot be null.");
            return result;
        }

        // Validate sessions don't overlap
        if (workingHours.MorningSession != null && workingHours.AfternoonSession != null)
        {
            if (workingHours.MorningSession.IntersectsWith(workingHours.AfternoonSession))
            {
                result.AddError("Morning and afternoon sessions cannot overlap.");
            }
        }

        // Validate break times are within working hours
        foreach (var breakTime in workingHours.BreakTimes)
        {
            var isWithinMorning = workingHours.MorningSession?.HasInside(breakTime) == true;
            var isWithinAfternoon = workingHours.AfternoonSession?.HasInside(breakTime) == true;

            if (!isWithinMorning && !isWithinAfternoon)
            {
                result.AddWarning($"Break time {breakTime.ToFriendlyString()} is outside of working hours.");
            }
        }

        // Validate advance booking rules
        if (workingHours.MinimumAdvanceHours < 0)
        {
            result.AddError("Minimum advance hours cannot be negative.");
        }

        if (workingHours.MaximumAdvanceDays <= 0)
        {
            result.AddError("Maximum advance days must be greater than zero.");
        }

        if (workingHours.MinimumAdvanceHours > (workingHours.MaximumAdvanceDays * 24))
        {
            result.AddError("Minimum advance hours cannot exceed maximum advance days.");
        }

        // Validate buffer time
        if (workingHours.BufferTimeMinutes < 0)
        {
            result.AddError("Buffer time cannot be negative.");
        }

        if (workingHours.BufferTimeMinutes > 120) // 2 hours
        {
            result.AddWarning("Buffer time exceeds 2 hours, which may significantly reduce available slots.");
        }

        // Validate available days
        if (!workingHours.AvailableDays.Any())
        {
            result.AddError("At least one day must be available for appointments.");
        }

        return result;
    }

    /// <summary>
    /// Finds available time slots within a date range.
    /// </summary>
    /// <param name="startDate">The start date to search from.</param>
    /// <param name="endDate">The end date to search to.</param>
    /// <param name="durationMinutes">The required duration in minutes.</param>
    /// <param name="leaderId">The leader ID to check availability for.</param>
    /// <param name="existingAppointments">Existing appointments to avoid conflicts with.</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <returns>A list of available time slots.</returns>
    public static List<TimeSlot> FindAvailableSlots(
        DateTime startDate,
        DateTime endDate,
        int durationMinutes,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours workingHours,
        string leaderName = "")
    {
        var availableSlots = new List<TimeSlot>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            // Get working hours for this date
            var daySlots = workingHours.GetAvailableSlots(currentDate);

            foreach (var workingSlot in daySlots)
            {
                // Split the working slot into appointment-sized slots
                var timeSlots = workingSlot.SplitIntoSlots(durationMinutes);

                foreach (var slot in timeSlots)
                {
                    var timeSlot = TimeSlot.FromTimeRange(slot, true, leaderId, leaderName);
                    var validation = ValidateTimeSlot(timeSlot, existingAppointments, workingHours);

                    if (validation.IsValid)
                    {
                        availableSlots.Add(timeSlot);
                    }
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return availableSlots;
    }

    /// <summary>
    /// Suggests alternative time slots if the requested time is not available.
    /// </summary>
    /// <param name="preferredStartTime">The preferred start time.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    /// <param name="leaderId">The leader ID.</param>
    /// <param name="existingAppointments">Existing appointments to avoid conflicts with.</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return.</param>
    /// <returns>A list of suggested alternative time slots.</returns>
    public static List<TimeSlot> SuggestAlternativeSlots(
        DateTime preferredStartTime,
        int durationMinutes,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours workingHours,
        int maxSuggestions = 5,
        string leaderName = "")
    {
        var suggestions = new List<TimeSlot>();
        var searchDate = preferredStartTime.Date;
        var daysSearched = 0;
        const int maxDaysToSearch = 14; // Search up to 2 weeks

        while (suggestions.Count < maxSuggestions && daysSearched < maxDaysToSearch)
        {
            var daySlots = FindAvailableSlots(
                searchDate,
                searchDate,
                durationMinutes,
                leaderId,
                existingAppointments,
                workingHours,
                leaderName);

            // For the preferred date, look for slots closest to preferred time
            if (searchDate == preferredStartTime.Date)
            {
                daySlots = daySlots
                    .OrderBy(slot => Math.Abs((slot.StartTime - preferredStartTime).TotalMinutes))
                    .ToList();
            }

            suggestions.AddRange(daySlots.Take(maxSuggestions - suggestions.Count));
            
            searchDate = searchDate.AddDays(1);
            daysSearched++;
        }

        return suggestions.Take(maxSuggestions).ToList();
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed without errors.
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Gets any appointments that conflict with the validated item.
    /// </summary>
    public List<Appointment> ConflictingAppointments { get; set; } = new();

    /// <summary>
    /// Adds an error message to the validation result.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void AddError(string message)
    {
        Errors.Add(message);
    }

    /// <summary>
    /// Adds a warning message to the validation result.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public void AddWarning(string message)
    {
        Warnings.Add(message);
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The other validation result to merge.</param>
    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        ConflictingAppointments.AddRange(other.ConflictingAppointments);
    }

    /// <summary>
    /// Returns a summary of the validation result.
    /// </summary>
    /// <returns>A string describing the validation result.</returns>
    public override string ToString()
    {
        if (IsValid && !Warnings.Any())
        {
            return "Validation passed.";
        }

        var summary = new List<string>();
        
        if (Errors.Any())
        {
            summary.Add($"{Errors.Count} error(s): {string.Join("; ", Errors)}");
        }
        
        if (Warnings.Any())
        {
            summary.Add($"{Warnings.Count} warning(s): {string.Join("; ", Warnings)}");
        }

        return string.Join(" | ", summary);
    }
}