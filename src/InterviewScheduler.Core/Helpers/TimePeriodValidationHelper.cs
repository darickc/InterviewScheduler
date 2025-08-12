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