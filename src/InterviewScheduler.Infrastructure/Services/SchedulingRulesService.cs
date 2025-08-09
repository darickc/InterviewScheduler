using Itenso.TimePeriod;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Extensions;
using InterviewScheduler.Core.Helpers;
using InterviewScheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InterviewScheduler.Infrastructure.Services;

/// <summary>
/// Service for managing scheduling business rules and constraints.
/// Provides centralized validation and rule enforcement for appointment scheduling.
/// </summary>
public class SchedulingRulesService : ISchedulingRulesService
{
    private readonly SchedulingConfiguration _configuration;
    private readonly ILogger<SchedulingRulesService> _logger;

    public SchedulingRulesService(
        IOptions<SchedulingConfiguration> configuration,
        ILogger<SchedulingRulesService> logger)
    {
        _configuration = configuration.Value ?? SchedulingConfiguration.CreateStandardConfiguration();
        _logger = logger;
    }

    /// <summary>
    /// Checks if an appointment time falls within the configured working hours.
    /// </summary>
    public bool IsWithinWorkingHours(TimeRange appointment, WorkingHours? workingHours = null)
    {
        // Always return true - all times are within working hours in unrestricted mode
        return true;
    }

    /// <summary>
    /// Gets all blocked time periods for a specific date.
    /// </summary>
    public TimePeriodCollection GetBlockedPeriods(DateTime date, int? leaderId = null, WorkingHours? workingHours = null)
    {
        // Return empty collection - no periods are blocked
        _logger.LogDebug("Returning empty blocked periods for date {Date} - unrestricted scheduling enabled", date);
        return new TimePeriodCollection();
    }

    /// <summary>
    /// Gets all blocked time periods with detailed metadata for a specific date.
    /// </summary>
    public BlockedPeriodCollection GetDetailedBlockedPeriods(DateTime date, int? leaderId = null, WorkingHours? workingHours = null)
    {
        // Return empty collection - no periods are blocked
        _logger.LogDebug("Returning empty detailed blocked periods for date {Date} - unrestricted scheduling enabled", date);
        return new BlockedPeriodCollection();
    }

    /// <summary>
    /// Determines the reason for a blocked period based on time and context.
    /// </summary>
    private BlockedPeriodReason GetBlockedPeriodReason(DateTime start, DateTime end, DateTime date)
    {
        // Check if it's a lunch break (typically 12:00 PM - 1:00 PM)
        if (start.TimeOfDay >= TimeSpan.FromHours(12) && end.TimeOfDay <= TimeSpan.FromHours(13))
        {
            return BlockedPeriodReason.LunchBreak;
        }

        // Check if it's weekend
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return BlockedPeriodReason.Weekend;
        }

        // Default to outside working hours
        return BlockedPeriodReason.OutsideWorkingHours;
    }

    /// <summary>
    /// Gets a human-readable description for a blocked period.
    /// </summary>
    private string GetBlockedPeriodDescription(BlockedPeriodReason reason, DateTime start, DateTime end)
    {
        return reason switch
        {
            BlockedPeriodReason.LunchBreak => "Lunch break",
            BlockedPeriodReason.Weekend => $"{start.DayOfWeek}",
            BlockedPeriodReason.OutsideWorkingHours => "Outside business hours",
            _ => "Blocked time"
        };
    }

    /// <summary>
    /// Gets the required buffer time between appointments for a specific appointment type.
    /// </summary>
    public TimeSpan GetRequiredBufferTime(AppointmentType appointmentType)
    {
        if (appointmentType.HasBufferTimeRequirements)
        {
            // Use the maximum of before/after buffer times as the primary buffer
            var maxBuffer = Math.Max(appointmentType.BufferTimeBeforeMinutes, appointmentType.BufferTimeAfterMinutes);
            return TimeSpan.FromMinutes(maxBuffer);
        }

        // Fall back to system default
        return TimeSpan.FromMinutes(_configuration.DefaultBufferTimeMinutes);
    }

    /// <summary>
    /// Validates all scheduling constraints for a proposed appointment.
    /// </summary>
    public ValidationResult ValidateSchedulingConstraints(
        TimeRange appointment,
        AppointmentType appointmentType,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null)
    {
        var result = new ValidationResult();

        try
        {
            // Basic time validation
            if (appointment.Start >= appointment.End)
            {
                result.AddError("Appointment start time must be before end time.");
                return result;
            }

            if (appointment.Start < DateTime.Now)
            {
                result.AddError("Cannot schedule appointments in the past.");
            }

            // Validate appointment type constraints
            var typeValidation = ValidateAppointmentTypeConstraints(
                appointmentType, 
                appointment.Start, 
                (int)appointment.Duration.TotalMinutes);
            result.Merge(typeValidation);

            // Skip working hours validation - all times are available
            // Skip weekend validation - all days are available
            // Skip date availability validation - all dates are available
            // In unrestricted mode, we allow scheduling at any time

            // Conflict validation with existing appointments
            var leaderAppointments = existingAppointments.Where(a => a.LeaderId == leaderId);
            var conflicts = appointment.GetConflicts(leaderAppointments);
            if (conflicts.Any())
            {
                if (_configuration.AllowHighPriorityDoubleBooking && 
                    appointmentType.SchedulingPriority <= _configuration.DoubleBookingPriorityThreshold)
                {
                    result.AddWarning($"High-priority appointment conflicts with {conflicts.Count} existing appointment(s) but is allowed to double-book.");
                }
                else
                {
                    result.AddError($"Time slot conflicts with {conflicts.Count} existing appointment(s).");
                    result.ConflictingAppointments = conflicts;
                }
            }

            // Buffer time validation
            if (!ValidateBufferTime(appointment, appointmentType, existingAppointments, leaderId))
            {
                if (appointmentType.RequireStrictBufferTime || _configuration.EnforceStrictValidation)
                {
                    result.AddError($"Appointment does not meet required buffer time of {GetRequiredBufferTime(appointmentType).TotalMinutes} minutes.");
                }
                else
                {
                    result.AddWarning($"Appointment is within {GetRequiredBufferTime(appointmentType).TotalMinutes} minutes of another appointment.");
                }
            }

            // Skip blocked periods validation - no periods are blocked in unrestricted mode
            // All times are available for scheduling

            _logger.LogDebug("Validation completed for appointment at {Time} with {ErrorCount} errors and {WarningCount} warnings",
                appointment.Start, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scheduling constraints for appointment at {Time}", appointment.Start);
            result.AddError("An error occurred while validating scheduling constraints.");
        }

        return result;
    }

    /// <summary>
    /// Checks if an appointment can be scheduled with the required buffer time.
    /// </summary>
    public bool ValidateBufferTime(
        TimeRange appointment,
        AppointmentType appointmentType,
        IEnumerable<Appointment> existingAppointments,
        int leaderId)
    {
        try
        {
            var requiredBuffer = GetRequiredBufferTime(appointmentType);
            if (requiredBuffer.TotalMinutes == 0)
            {
                return true; // No buffer time required
            }

            var leaderAppointments = existingAppointments.Where(a => a.LeaderId == leaderId);

            // Check buffer before appointment
            if (appointmentType.BufferTimeBeforeMinutes > 0)
            {
                var bufferBefore = TimeSpan.FromMinutes(appointmentType.BufferTimeBeforeMinutes);
                var bufferRangeBefore = new TimeRange(appointment.Start.Subtract(bufferBefore), appointment.Start);
                
                if (leaderAppointments.Any(a => a.TimeRange.IntersectsWith(bufferRangeBefore)))
                {
                    return false;
                }
            }

            // Check buffer after appointment
            if (appointmentType.BufferTimeAfterMinutes > 0)
            {
                var bufferAfter = TimeSpan.FromMinutes(appointmentType.BufferTimeAfterMinutes);
                var bufferRangeAfter = new TimeRange(appointment.End, appointment.End.Add(bufferAfter));
                
                if (leaderAppointments.Any(a => a.TimeRange.IntersectsWith(bufferRangeAfter)))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating buffer time for appointment at {Time}", appointment.Start);
            return false;
        }
    }

    /// <summary>
    /// Gets available time slots for a specific date, considering all business rules.
    /// </summary>
    public TimePeriodCollection GetAvailableSlots(
        DateTime date,
        AppointmentType appointmentType,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        WorkingHours? workingHours = null)
    {
        try
        {
            var availableSlots = new TimePeriodCollection();
            
            // In unrestricted mode, the entire day is available (24 hours)
            var fullDaySlot = new TimeRange(date.Date, date.Date.AddDays(1));
            var adjustedSlots = new TimePeriodCollection { fullDaySlot };

            // Skip blocked periods check - there are no blocked periods in unrestricted mode
            
            // Only remove intersections with existing appointments
            var leaderAppointments = existingAppointments.Where(a => a.LeaderId == leaderId);
            foreach (var appointment in leaderAppointments)
            {
                var tempSlots = new TimePeriodCollection();
                foreach (var slot in adjustedSlots)
                {
                    if (!slot.IntersectsWith(appointment.TimeRange))
                    {
                        tempSlots.Add(slot);
                    }
                    else
                    {
                        // Split slot around appointment
                        if (slot.Start < appointment.TimeRange.Start)
                        {
                            tempSlots.Add(new TimeRange(slot.Start, appointment.TimeRange.Start));
                        }
                        if (slot.End > appointment.TimeRange.End)
                        {
                            tempSlots.Add(new TimeRange(appointment.TimeRange.End, slot.End));
                        }
                    }
                }
                adjustedSlots = tempSlots;
            }

            // Add buffer time around existing appointments
            var bufferTime = GetRequiredBufferTime(appointmentType);
            if (bufferTime.TotalMinutes > 0)
            {
                foreach (var appointment in leaderAppointments)
                {
                    var bufferRange = new TimeRange(
                        appointment.TimeRange.Start.Subtract(bufferTime),
                        appointment.TimeRange.End.Add(bufferTime)
                    );

                    var tempSlots = new TimePeriodCollection();
                    foreach (var slot in adjustedSlots)
                    {
                        if (!slot.IntersectsWith(bufferRange))
                        {
                            tempSlots.Add(slot);
                        }
                        else
                        {
                            // Split slot around buffer zone
                            if (slot.Start < bufferRange.Start)
                            {
                                tempSlots.Add(new TimeRange(slot.Start, bufferRange.Start));
                            }
                            if (slot.End > bufferRange.End)
                            {
                                tempSlots.Add(new TimeRange(bufferRange.End, slot.End));
                            }
                        }
                    }
                    adjustedSlots = tempSlots;
                }
            }

            // Convert large time periods into appointment-sized slots
            var appointmentSlots = new TimePeriodCollection();
            var slotDuration = TimeSpan.FromMinutes(appointmentType.Duration);
            
            foreach (var period in adjustedSlots)
            {
                var currentTime = period.Start;
                while (currentTime.Add(slotDuration) <= period.End)
                {
                    var slotEnd = currentTime.Add(slotDuration);
                    appointmentSlots.Add(new TimeRange(currentTime, slotEnd));
                    currentTime = currentTime.Add(slotDuration); // Move to next slot
                }
            }
            
            availableSlots.AddAll(appointmentSlots);

            _logger.LogDebug("Generated {Count} appointment-sized slots (duration: {Duration} min) for date {Date} and leader {LeaderId}",
                availableSlots.Count, appointmentType.Duration, date, leaderId);

            return availableSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available slots for date {Date} and leader {LeaderId}", date, leaderId);
            return new TimePeriodCollection();
        }
    }

    /// <summary>
    /// Checks if a specific date is available for scheduling (not a holiday or blackout date).
    /// </summary>
    public bool IsDateAvailable(DateTime date, int? leaderId = null)
    {
        // Always return true - all dates are available in unrestricted mode
        return true;
    }

    /// <summary>
    /// Gets the system default working hours configuration.
    /// </summary>
    public WorkingHours GetDefaultWorkingHours()
    {
        return _configuration.DefaultWorkingHours;
    }

    /// <summary>
    /// Gets leader-specific working hours if configured, otherwise returns system defaults.
    /// </summary>
    public WorkingHours GetWorkingHoursForLeader(int leaderId)
    {
        // TODO: In future implementation, could load leader-specific working hours from database
        // For now, return system defaults
        return _configuration.DefaultWorkingHours;
    }

    /// <summary>
    /// Validates appointment type constraints (duration, advance booking, etc.).
    /// </summary>
    public ValidationResult ValidateAppointmentTypeConstraints(
        AppointmentType appointmentType,
        DateTime proposedTime,
        int duration)
    {
        var result = new ValidationResult();

        try
        {
            // Duration constraints
            var minDuration = appointmentType.EffectiveMinimumDuration;
            if (duration < minDuration)
            {
                result.AddError($"Appointment duration ({duration} minutes) is less than the minimum required ({minDuration} minutes).");
            }

            var maxDuration = appointmentType.EffectiveMaximumDuration;
            if (maxDuration.HasValue && duration > maxDuration.Value)
            {
                result.AddError($"Appointment duration ({duration} minutes) exceeds the maximum allowed ({maxDuration.Value} minutes).");
            }

            // System-wide duration limits
            if (duration < _configuration.MinimumAppointmentDurationMinutes)
            {
                result.AddError($"Appointment duration ({duration} minutes) is less than the system minimum ({_configuration.MinimumAppointmentDurationMinutes} minutes).");
            }

            if (duration > _configuration.MaximumAppointmentDurationMinutes)
            {
                result.AddError($"Appointment duration ({duration} minutes) exceeds the system maximum ({_configuration.MaximumAppointmentDurationMinutes} minutes).");
            }

            // Advance booking constraints
            if (appointmentType.HasCustomAdvanceBookingRules)
            {
                var timeDifference = proposedTime - DateTime.Now;

                if (appointmentType.MinimumAdvanceBookingHours > 0 && 
                    timeDifference.TotalHours < appointmentType.MinimumAdvanceBookingHours)
                {
                    result.AddError($"Appointment must be scheduled at least {appointmentType.MinimumAdvanceBookingHours} hours in advance.");
                }

                if (appointmentType.MaximumAdvanceBookingDays > 0 && 
                    timeDifference.TotalDays > appointmentType.MaximumAdvanceBookingDays)
                {
                    result.AddError($"Appointment cannot be scheduled more than {appointmentType.MaximumAdvanceBookingDays} days in advance.");
                }
            }
            else
            {
                // Use system defaults
                var timeDifference = proposedTime - DateTime.Now;

                if (timeDifference.TotalHours < _configuration.DefaultMinimumAdvanceBookingHours)
                {
                    result.AddError($"Appointment must be scheduled at least {_configuration.DefaultMinimumAdvanceBookingHours} hours in advance.");
                }

                if (timeDifference.TotalDays > _configuration.DefaultMaximumAdvanceBookingDays)
                {
                    result.AddError($"Appointment cannot be scheduled more than {_configuration.DefaultMaximumAdvanceBookingDays} days in advance.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating appointment type constraints for type {TypeId}", appointmentType.Id);
            result.AddError("An error occurred while validating appointment type constraints.");
        }

        return result;
    }

    /// <summary>
    /// Suggests alternative time slots when the requested time is not available.
    /// </summary>
    public IEnumerable<TimeRange> SuggestAlternativeTimeSlots(
        DateTime requestedTime,
        AppointmentType appointmentType,
        int leaderId,
        IEnumerable<Appointment> existingAppointments,
        int searchDays = 7,
        WorkingHours? workingHours = null)
    {
        var alternatives = new List<TimeRange>();

        try
        {
            if (!_configuration.EnableAutomaticAlternativeSuggestions)
            {
                return alternatives;
            }

            var searchLimit = Math.Min(searchDays, _configuration.AlternativeSearchDays);
            var durationMinutes = appointmentType.Duration;

            // Search for alternatives starting from the requested date
            for (int dayOffset = 0; dayOffset <= searchLimit; dayOffset++)
            {
                var searchDate = requestedTime.Date.AddDays(dayOffset);
                
                if (!IsDateAvailable(searchDate, leaderId))
                {
                    continue;
                }

                var availableSlots = GetAvailableSlots(searchDate, appointmentType, leaderId, existingAppointments, workingHours);

                // Generate time slots of the required duration
                foreach (var slot in availableSlots)
                {
                    var slotDuration = (int)slot.Duration.TotalMinutes;
                    if (slotDuration >= durationMinutes)
                    {
                        // Use intelligent increments that consider existing appointment end times
                        var timeIncrements = GetIntelligentTimeIncrements(searchDate, slot, appointmentType, existingAppointments, leaderId);
                        
                        foreach (var currentTime in timeIncrements)
                        {
                            if (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= slot.End)
                            {
                                var proposedSlot = new TimeRange(currentTime, currentTime.AddMinutes(durationMinutes));
                                
                                // Validate this proposed slot
                                var validation = ValidateSchedulingConstraints(
                                    proposedSlot, appointmentType, leaderId, existingAppointments, workingHours);
                                
                                if (validation.IsValid)
                                {
                                    alternatives.Add(proposedSlot);
                                    
                                    // Limit the number of suggestions
                                    if (alternatives.Count >= 10)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (alternatives.Count >= 10)
                        {
                            break;
                        }
                    }
                }

                if (alternatives.Count >= 10)
                {
                    break;
                }
            }

            _logger.LogDebug("Generated {Count} alternative time slots for requested time {RequestedTime}",
                alternatives.Count, requestedTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting alternative time slots for requested time {RequestedTime}", requestedTime);
        }

        return alternatives.OrderBy(a => a.Start);
    }

    /// <summary>
    /// Generates intelligent time increments that consider existing appointment end times
    /// instead of using fixed intervals, providing more accurate next available slots.
    /// </summary>
    private IEnumerable<DateTime> GetIntelligentTimeIncrements(
        DateTime searchDate, 
        ITimePeriod availableSlot, 
        AppointmentType appointmentType, 
        IEnumerable<Appointment> existingAppointments, 
        int leaderId)
    {
        var timeIncrements = new SortedSet<DateTime>();
        var durationMinutes = appointmentType.Duration;
        var bufferTime = GetRequiredBufferTime(appointmentType);
        
        // Start with the beginning of the available slot
        timeIncrements.Add(availableSlot.Start);
        
        // Add potential start times based on existing appointment end times
        var dayAppointments = existingAppointments
            .Where(a => a.LeaderId == leaderId && a.ScheduledTime.Date == searchDate.Date)
            .OrderBy(a => a.ScheduledTime);
            
        foreach (var appointment in dayAppointments)
        {
            var appointmentEnd = appointment.ScheduledTime.AddMinutes(appointment.AppointmentType?.Duration ?? durationMinutes);
            
            // Add the end time of the appointment (plus buffer if required) as a potential start time
            var potentialStart = appointmentEnd.Add(bufferTime);
            
            // Only add if it's within the available slot and allows for full appointment duration
            if (potentialStart >= availableSlot.Start && 
                potentialStart.AddMinutes(durationMinutes) <= availableSlot.End)
            {
                timeIncrements.Add(potentialStart);
            }
        }
        
        // Fill in gaps with regular intervals to ensure we don't miss any opportunities
        var increment = TimeSpan.FromMinutes(Math.Min(durationMinutes, _configuration.DefaultTimeSlotIncrementMinutes));
        var currentTime = availableSlot.Start;
        
        while (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= availableSlot.End)
        {
            timeIncrements.Add(currentTime);
            currentTime = currentTime.Add(increment);
        }
        
        return timeIncrements;
    }

    /// <summary>
    /// Gets detailed information about why a time slot conflicts with blocked periods.
    /// </summary>
    public string GetConflictDetails(TimeRange appointment, DateTime date, int? leaderId = null, WorkingHours? workingHours = null)
    {
        try
        {
            var detailedBlockedPeriods = GetDetailedBlockedPeriods(date, leaderId, workingHours);
            return detailedBlockedPeriods.GetConflictDescription(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conflict details for appointment at {Time}", appointment.Start);
            return "Unable to determine conflict details.";
        }
    }
}