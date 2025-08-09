using Itenso.TimePeriod;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Helpers;

namespace InterviewScheduler.Core.Interfaces;

/// <summary>
/// Service for managing scheduling business rules and constraints.
/// Provides centralized validation and rule enforcement for appointment scheduling.
/// </summary>
public interface ISchedulingRulesService
{
    /// <summary>
    /// Checks if an appointment time falls within the configured working hours.
    /// </summary>
    /// <param name="appointment">The time range of the appointment to validate.</param>
    /// <param name="workingHours">The working hours configuration to check against. If null, uses system defaults.</param>
    /// <returns>True if the appointment is within working hours, false otherwise.</returns>
    bool IsWithinWorkingHours(TimeRange appointment, WorkingHours? workingHours = null);

    /// <summary>
    /// Gets all blocked time periods for a specific date.
    /// Includes break times, holidays, and other unavailable periods.
    /// </summary>
    /// <param name="date">The date to get blocked periods for.</param>
    /// <param name="leaderId">Optional leader ID to get leader-specific blocked periods.</param>
    /// <param name="workingHours">Working hours configuration. If null, uses system defaults.</param>
    /// <returns>A collection of blocked time periods.</returns>
    TimePeriodCollection GetBlockedPeriods(DateTime date, int? leaderId = null, WorkingHours? workingHours = null);

    /// <summary>
    /// Gets all blocked time periods with detailed metadata for a specific date.
    /// Includes break times, holidays, and other unavailable periods with reasoning.
    /// </summary>
    /// <param name="date">The date to get blocked periods for.</param>
    /// <param name="leaderId">Optional leader ID to get leader-specific blocked periods.</param>
    /// <param name="workingHours">Working hours configuration. If null, uses system defaults.</param>
    /// <returns>A collection of blocked time periods with detailed metadata.</returns>
    BlockedPeriodCollection GetDetailedBlockedPeriods(DateTime date, int? leaderId = null, WorkingHours? workingHours = null);

    /// <summary>
    /// Gets the required buffer time between appointments for a specific appointment type.
    /// </summary>
    /// <param name="appointmentType">The appointment type to get buffer time for.</param>
    /// <returns>The required buffer time as a TimeSpan.</returns>
    TimeSpan GetRequiredBufferTime(AppointmentType appointmentType);

    /// <summary>
    /// Validates all scheduling constraints for a proposed appointment.
    /// </summary>
    /// <param name="appointment">The appointment time range to validate.</param>
    /// <param name="appointmentType">The type of appointment being scheduled.</param>
    /// <param name="leaderId">The ID of the leader for the appointment.</param>
    /// <param name="existingAppointments">Existing appointments to check for conflicts.</param>
    /// <param name="workingHours">Working hours configuration. If null, uses system defaults.</param>
    /// <returns>A validation result indicating success or failure with detailed messages.</returns>
    ValidationResult ValidateSchedulingConstraints(
        TimeRange appointment, 
        AppointmentType appointmentType, 
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null);

    /// <summary>
    /// Checks if an appointment can be scheduled with the required buffer time.
    /// </summary>
    /// <param name="appointment">The proposed appointment time range.</param>
    /// <param name="appointmentType">The appointment type.</param>
    /// <param name="existingAppointments">Existing appointments to check buffer time against.</param>
    /// <param name="leaderId">The leader ID for the appointment.</param>
    /// <returns>True if buffer time requirements are met, false otherwise.</returns>
    bool ValidateBufferTime(
        TimeRange appointment, 
        AppointmentType appointmentType, 
        IEnumerable<Appointment> existingAppointments,
        int leaderId);

    /// <summary>
    /// Gets available time slots for a specific date, considering all business rules.
    /// </summary>
    /// <param name="date">The date to get available slots for.</param>
    /// <param name="appointmentType">The type of appointment to schedule.</param>
    /// <param name="leaderId">The leader ID.</param>
    /// <param name="existingAppointments">Existing appointments to avoid conflicts with.</param>
    /// <param name="workingHours">Working hours configuration. If null, uses system defaults.</param>
    /// <returns>A collection of available time slots that meet all business rules.</returns>
    TimePeriodCollection GetAvailableSlots(
        DateTime date,
        AppointmentType appointmentType,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null);

    /// <summary>
    /// Checks if a specific date is available for scheduling (not a holiday or blackout date).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="leaderId">Optional leader ID for leader-specific availability.</param>
    /// <returns>True if the date is available for scheduling, false otherwise.</returns>
    bool IsDateAvailable(DateTime date, int? leaderId = null);

    /// <summary>
    /// Gets the system default working hours configuration.
    /// </summary>
    /// <returns>The default working hours configuration.</returns>
    WorkingHours GetDefaultWorkingHours();

    /// <summary>
    /// Gets leader-specific working hours if configured, otherwise returns system defaults.
    /// </summary>
    /// <param name="leaderId">The leader ID to get working hours for.</param>
    /// <returns>The working hours configuration for the leader.</returns>
    WorkingHours GetWorkingHoursForLeader(int leaderId);

    /// <summary>
    /// Validates appointment type constraints (duration, advance booking, etc.).
    /// </summary>
    /// <param name="appointmentType">The appointment type to validate constraints for.</param>
    /// <param name="proposedTime">The proposed appointment time.</param>
    /// <param name="duration">The proposed duration in minutes.</param>
    /// <returns>A validation result indicating if the appointment type constraints are met.</returns>
    ValidationResult ValidateAppointmentTypeConstraints(
        AppointmentType appointmentType,
        DateTime proposedTime,
        int duration);

    /// <summary>
    /// Suggests alternative time slots when the requested time is not available.
    /// </summary>
    /// <param name="requestedTime">The originally requested time.</param>
    /// <param name="appointmentType">The appointment type.</param>
    /// <param name="leaderId">The leader ID.</param>
    /// <param name="existingAppointments">Existing appointments to avoid.</param>
    /// <param name="searchDays">Number of days to search for alternatives (default 7).</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <returns>A collection of alternative available time slots.</returns>
    IEnumerable<TimeRange> SuggestAlternativeTimeSlots(
        DateTime requestedTime,
        AppointmentType appointmentType,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        int searchDays = 7,
        WorkingHours? workingHours = null);

    /// <summary>
    /// Gets detailed information about why a time slot conflicts with blocked periods.
    /// </summary>
    /// <param name="appointment">The appointment time to analyze.</param>
    /// <param name="date">The date to check blocked periods for.</param>
    /// <param name="leaderId">Optional leader ID.</param>
    /// <param name="workingHours">Working hours configuration.</param>
    /// <returns>A detailed description of the conflict or empty string if no conflict.</returns>
    string GetConflictDetails(TimeRange appointment, DateTime date, int? leaderId = null, WorkingHours? workingHours = null);
}