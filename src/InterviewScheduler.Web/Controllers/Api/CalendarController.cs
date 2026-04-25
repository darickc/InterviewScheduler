using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/calendar")]
public class CalendarController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICalendarService _calendar;

    public CalendarController(IUserService userService, ApplicationDbContext db, ICalendarService calendar)
        : base(userService)
    {
        _db = db;
        _calendar = calendar;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int leaderId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == leaderId && l.UserId == user.Id);
        if (leader == null) return NotFound();
        if (string.IsNullOrEmpty(leader.GoogleCalendarId)) return Ok(new List<TimeSlotDto>());

        var events = await _calendar.GetCalendarEventsAsync(leader.GoogleCalendarId, leader.Name, leader.Id, start, end);

        // Calendar events are busy ranges → IsAvailable=false. Reuse TimeSlotDto so the
        // client doesn't need a separate "busy range" type.
        var dtos = events.Select(e => new TimeSlotDto
        {
            StartTime = e.Start,
            EndTime = e.End,
            IsAvailable = false,
            LeaderId = e.LeaderId,
            LeaderName = e.LeaderName
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] int leaderId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int durationMinutes)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == leaderId && l.UserId == user.Id);
        if (leader == null) return NotFound();
        if (string.IsNullOrEmpty(leader.GoogleCalendarId)) return Ok(new List<TimeSlotDto>());

        var slots = await _calendar.GetAvailableTimeSlotsForLeaderAsync(
            leader.GoogleCalendarId, leader.Id, leader.Name, start, end, durationMinutes);

        return Ok(slots.Select(s => s.ToDto()).ToList());
    }

    [HttpDelete("events/{eventId}")]
    public async Task<IActionResult> DeleteEvent(string eventId, [FromQuery] int leaderId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == leaderId && l.UserId == user.Id);
        if (leader == null) return NotFound();

        var ok = await _calendar.DeleteEventAsync(leader.GoogleCalendarId, eventId);
        return ok ? NoContent() : StatusCode(502, "Failed to delete event from Google Calendar.");
    }
}
