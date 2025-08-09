using System;
using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Helpers;

public class CreateScheduleResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Appointment> Appointments { get; set; } = new ();
    public int AppointmentsCreated { get; set; }
    public int CalendarEventsCreated { get; set; }
    public List<string> UnscheduledContacts { get; set; } = new ();
}
