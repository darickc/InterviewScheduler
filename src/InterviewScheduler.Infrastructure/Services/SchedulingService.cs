using System;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Enums;
using InterviewScheduler.Core.Helpers;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using Itenso.TimePeriod;
using Microsoft.Extensions.Logging;

namespace InterviewScheduler.Infrastructure.Services;

public class SchedulingService(ICalendarService calendarService, IUserService userService, ApplicationDbContext dbContext, ILogger<SchedulingService> logger) : ISchedulingService
{
    public async Task<CreateScheduleResult> CreateSchedule(DateTime date, TimeOnly startTime, TimeOnly endTime, AppointmentType appointmentType, List<Leader> leaders, List<Contact> contacts)
    {
        CreateScheduleResult result = new CreateScheduleResult();
        var currentUser = await userService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            result.ErrorMessage = "User not found";
            return result;
        }
        var plan = new List<Appointment>();
        var startDate = date.Date.Add(startTime.ToTimeSpan());
        var endDate = date.Date.Add(endTime.ToTimeSpan());
        var duration = TimeSpan.FromMinutes(appointmentType.Duration);
        ITimePeriodCollection leaderFreeTimeSlots = new TimePeriodCollection();

        CalendarTimeRange searchLimits = new CalendarTimeRange(startDate, endDate);

        foreach (var leader in leaders)
        {
            var leaderTimeSlots = await calendarService.GetCalendarEventsAsync(leader.GoogleCalendarId, leader.Name, leader.Id, startDate, endDate);
            TimePeriodCollection timeSlots = new(leaderTimeSlots);

            // create a list of open time slots
            TimeGapCalculator<TimeRange> gapCalculator = new TimeGapCalculator<TimeRange>(new TimeCalendar());
            ITimePeriodCollection freeTimes = gapCalculator.GetGaps(timeSlots, searchLimits);
            leaderFreeTimeSlots.AddAll(freeTimes.Select(f => new LeaderTimeRange(f.Start, f.End, leader.Id, leader.Name, leader.GoogleCalendarId)));
        }

        while (true)
        {
            // find first time slot that is of duration or more
            leaderFreeTimeSlots.SortByStart();
            var firstFreeTimeSlot = leaderFreeTimeSlots.FirstOrDefault(f => f.Duration >= duration) as LeaderTimeRange;
            var contact = contacts.FirstOrDefault();
            if (firstFreeTimeSlot != null && contact != null)
            {
                var appointment = new Appointment();
                appointment.LeaderId = firstFreeTimeSlot.LeaderId;
                appointment.ContactId = contact.Id;
                appointment.ScheduledTime = firstFreeTimeSlot.Start;
                appointment.Status = AppointmentStatus.Pending;
                appointment.CreatedDate = DateTime.Now;
                appointment.UserId = currentUser.Id;
                appointment.AppointmentTypeId = appointmentType.Id;
                appointment.AppointmentType = appointmentType;
                plan.Add(appointment);
                dbContext.Appointments.Add(appointment);

                // remove the contact from the list
                contacts.Remove(contact);
                // remove the time slot from the leader free time slots and calculate the new free time slots
                TimePeriodCollection sourcePeriods = new TimePeriodCollection { firstFreeTimeSlot };
                TimePeriodCollection subtractingPeriods = new TimePeriodCollection { new TimeRange(appointment.ScheduledTime, appointment.ScheduledTime.Add(duration)) };

                TimePeriodSubtractor<TimeRange> subtractor = new TimePeriodSubtractor<TimeRange>();
                ITimePeriodCollection subtractedPeriods = subtractor.SubtractPeriods(sourcePeriods, subtractingPeriods);

                leaderFreeTimeSlots.Remove(firstFreeTimeSlot);
                foreach (var period in subtractedPeriods)
                {
                    leaderFreeTimeSlots.Add(new LeaderTimeRange(period.Start, period.End, firstFreeTimeSlot.LeaderId, firstFreeTimeSlot.LeaderName, firstFreeTimeSlot.CalendarId));
                }
            }
            else
            {
                break;
            }
        }
        result.CalendarEventsCreated = await CreateCalendarEvents(plan, leaders);
        await dbContext.SaveChangesAsync();

        result.Success = true;
        result.Appointments = plan;
        result.AppointmentsCreated = plan.Count;
        result.UnscheduledContacts = contacts.Select(c => c.DisplayName).ToList();
        return result;
    }

    private async Task<int> CreateCalendarEvents(List<Appointment> appointments, List<Leader> leaders)
    {
        int calendarEventsCreated = 0;
        foreach (var appointment in appointments)
        {
            var leader = leaders.First(l => l.Id == appointment.LeaderId);

            if (!string.IsNullOrEmpty(leader.GoogleCalendarId))
            {
                try
                {
                    if (appointment != null)
                    {
                        var eventId = await calendarService.CreateEventAsync(leader.GoogleCalendarId, appointment);
                        if (!string.IsNullOrEmpty(eventId))
                        {
                            appointment.GoogleEventId = eventId;
                            calendarEventsCreated++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log calendar creation failure but continue
                    logger.LogError(ex, $"Failed to create calendar event for {leader.Name}");
                }
            }
        }
        return calendarEventsCreated;
    }

}

