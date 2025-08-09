using System;
using Itenso.TimePeriod;

namespace InterviewScheduler.Core.Helpers;

public class LeaderTimeRange : TimeRange
{
    public int LeaderId { get; set; }
    public string LeaderName { get; set; }
    public string CalendarId { get; set; }

    public LeaderTimeRange(DateTime start, DateTime end, int leaderId, string leaderName, string calendarId) : base(start, end)
    {
        LeaderId = leaderId;
        LeaderName = leaderName;
        CalendarId = calendarId;
    }
}
