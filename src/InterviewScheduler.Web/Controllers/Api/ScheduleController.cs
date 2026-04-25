using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/schedule")]
public class ScheduleController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISchedulingService _scheduling;

    public ScheduleController(IUserService userService, ApplicationDbContext db, ISchedulingService scheduling)
        : base(userService)
    {
        _db = db;
        _scheduling = scheduling;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        if (request.LeaderIds.Count == 0 || request.ContactIds.Count == 0)
            return BadRequest("LeaderIds and ContactIds are required.");

        var appointmentType = await _db.AppointmentTypes
            .FirstOrDefaultAsync(t => t.Id == request.AppointmentTypeId && t.UserId == user.Id);
        if (appointmentType == null) return BadRequest("AppointmentType not found.");

        var leaders = await _db.Leaders
            .Where(l => request.LeaderIds.Contains(l.Id) && l.UserId == user.Id)
            .ToListAsync();
        if (leaders.Count == 0) return BadRequest("No matching leaders found.");

        var contacts = await _db.Contacts
            .Include(c => c.HeadOfHouse)
            .Include(c => c.Spouse)
            .Where(c => request.ContactIds.Contains(c.Id) && c.UserId == user.Id)
            .ToListAsync();
        if (contacts.Count == 0) return BadRequest("No matching contacts found.");

        var result = await _scheduling.CreateSchedule(
            request.Date,
            request.StartTime,
            request.EndTime,
            appointmentType,
            leaders,
            contacts);

        return Ok(new CreateScheduleResult
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            AppointmentsCreated = result.AppointmentsCreated,
            CalendarEventsCreated = result.CalendarEventsCreated,
            UnscheduledContacts = result.UnscheduledContacts,
            Appointments = result.Appointments.Select(a => a.ToDto()).ToList()
        });
    }
}
