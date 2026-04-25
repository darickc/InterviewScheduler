using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Enums;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/appointments")]
public class AppointmentsController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICalendarService _calendar;

    public AppointmentsController(IUserService userService, ApplicationDbContext db, ICalendarService calendar)
        : base(userService)
    {
        _db = db;
        _calendar = calendar;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? leaderId,
        [FromQuery] int? contactId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AppointmentStatus? status)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _db.Appointments
            .Include(a => a.Contact)
                .ThenInclude(c => c!.HeadOfHouse)
                    .ThenInclude(h => h!.Spouse)
            .Include(a => a.Leader)
            .Include(a => a.AppointmentType)
            .Where(a => a.UserId == user.Id);

        if (leaderId.HasValue) query = query.Where(a => a.LeaderId == leaderId.Value);
        if (contactId.HasValue) query = query.Where(a => a.ContactId == contactId.Value);
        if (from.HasValue) query = query.Where(a => a.ScheduledTime >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ScheduledTime < to.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var appointments = await query.OrderBy(a => a.ScheduledTime).ToListAsync();
        return Ok(appointments.Select(a => a.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var appt = await LoadFullAppointment(id, user.Id);
        if (appt == null) return NotFound();

        return Ok(appt.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var appt = new Appointment
        {
            UserId = user.Id,
            ContactId = dto.ContactId,
            LeaderId = dto.LeaderId,
            AppointmentTypeId = dto.AppointmentTypeId,
            ScheduledTime = dto.ScheduledTime,
            GoogleEventId = dto.GoogleEventId,
            Status = dto.Status == default ? AppointmentStatus.Pending : dto.Status,
            CreatedDate = DateTime.UtcNow
        };

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var loaded = await LoadFullAppointment(appt.Id, user.Id);
        return CreatedAtAction(nameof(Get), new { id = appt.Id }, loaded!.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
        if (appt == null) return NotFound();

        // Mirror Razor behavior: only allow hard-delete after a cancellation.
        if (appt.Status != AppointmentStatus.Cancelled)
            return BadRequest("Only cancelled appointments can be deleted.");

        _db.Appointments.Remove(appt);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
        if (appt == null) return NotFound();

        appt.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync();

        var loaded = await LoadFullAppointment(appt.Id, user.Id);
        return Ok(loaded!.ToDto());
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var appt = await _db.Appointments
            .Include(a => a.Leader)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

        if (appt == null) return NotFound();

        // If a Google event exists, delete it first; only mark cancelled when Google succeeds.
        if (!string.IsNullOrEmpty(appt.GoogleEventId) && !string.IsNullOrEmpty(appt.Leader?.GoogleCalendarId))
        {
            var deleted = await _calendar.DeleteEventAsync(appt.Leader.GoogleCalendarId, appt.GoogleEventId);
            if (!deleted)
                return StatusCode(502, "Failed to delete event from Google Calendar; appointment not cancelled.");
        }

        appt.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        var loaded = await LoadFullAppointment(appt.Id, user.Id);
        return Ok(loaded!.ToDto());
    }

    private Task<Appointment?> LoadFullAppointment(int id, int userId)
    {
        return _db.Appointments
            .Include(a => a.Contact)
                .ThenInclude(c => c!.HeadOfHouse)
                    .ThenInclude(h => h!.Spouse)
            .Include(a => a.Leader)
            .Include(a => a.AppointmentType)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }
}
