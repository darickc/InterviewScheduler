using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Entities;

/// <summary>
/// Represents a time range for an appointment with additional metadata.
/// Extends the TimePeriodLibrary TimeRange class to include appointment-specific information.
/// </summary>
public class AppointmentTimeRange : TimeRange
{
    /// <summary>
    /// The unique identifier of the appointment.
    /// </summary>
    public int AppointmentId { get; set; }

    /// <summary>
    /// The unique identifier of the leader conducting the appointment.
    /// </summary>
    public int LeaderId { get; set; }

    /// <summary>
    /// The type/category of the appointment.
    /// </summary>
    public string AppointmentType { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of AppointmentTimeRange.
    /// </summary>
    public AppointmentTimeRange() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of AppointmentTimeRange with the specified time range.
    /// </summary>
    /// <param name="start">The start time of the appointment.</param>
    /// <param name="end">The end time of the appointment.</param>
    public AppointmentTimeRange(DateTime start, DateTime end) : base(start, end)
    {
    }

    /// <summary>
    /// Initializes a new instance of AppointmentTimeRange with the specified time range and metadata.
    /// </summary>
    /// <param name="start">The start time of the appointment.</param>
    /// <param name="end">The end time of the appointment.</param>
    /// <param name="appointmentId">The appointment identifier.</param>
    /// <param name="leaderId">The leader identifier.</param>
    /// <param name="appointmentType">The appointment type.</param>
    public AppointmentTimeRange(DateTime start, DateTime end, int appointmentId, int leaderId, string appointmentType) 
        : base(start, end)
    {
        AppointmentId = appointmentId;
        LeaderId = leaderId;
        AppointmentType = appointmentType;
    }

    /// <summary>
    /// Creates an AppointmentTimeRange from an existing Appointment entity.
    /// </summary>
    /// <param name="appointment">The appointment to create the time range from.</param>
    /// <returns>A new AppointmentTimeRange instance.</returns>
    public static AppointmentTimeRange FromAppointment(Appointment appointment)
    {
        var endTime = appointment.ScheduledTime.AddMinutes(appointment.AppointmentType.Duration);
        
        return new AppointmentTimeRange(
            appointment.ScheduledTime,
            endTime,
            appointment.Id,
            appointment.LeaderId,
            appointment.AppointmentType.Name
        );
    }

    /// <summary>
    /// Returns a string representation of the appointment time range.
    /// </summary>
    /// <returns>A string describing the appointment time range.</returns>
    public override string ToString()
    {
        return $"{AppointmentType} appointment (ID: {AppointmentId}) for Leader {LeaderId}: {base.ToString()}";
    }
}