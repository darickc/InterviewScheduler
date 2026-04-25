using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/leaders")]
public class LeadersController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICalendarService _calendar;

    public LeadersController(IUserService userService, ApplicationDbContext db, ICalendarService calendar)
        : base(userService)
    {
        _db = db;
        _calendar = calendar;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leaders = await _db.Leaders
            .Where(l => l.UserId == user.Id)
            .OrderBy(l => l.Title)
            .ToListAsync();

        return Ok(leaders.Select(l => l.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);
        if (leader == null) return NotFound();

        return Ok(leader.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaderDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = new Leader { UserId = user.Id };
        dto.ApplyTo(leader);

        _db.Leaders.Add(leader);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = leader.Id }, leader.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LeaderDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);
        if (leader == null) return NotFound();

        dto.ApplyTo(leader);
        await _db.SaveChangesAsync();

        return Ok(leader.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);
        if (leader == null) return NotFound();

        _db.Leaders.Remove(leader);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("calendars")]
    public async Task<IActionResult> Calendars()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var calendars = await _calendar.GetCalendarsAsync();
        return Ok(calendars.Select(c => c.ToDto()).ToList());
    }
}
