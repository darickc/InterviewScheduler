using System;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Helpers;

namespace InterviewScheduler.Core.Interfaces;

public interface ISchedulingService
{
    Task<CreateScheduleResult> CreateSchedule(DateTime date, TimeOnly startTime, TimeOnly endTime, AppointmentType appointmentType, List<Leader> leaders, List<Contact> contacts);
}
