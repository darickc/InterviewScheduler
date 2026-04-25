using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/appointment-types")]
public class AppointmentTypesController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public AppointmentTypesController(IUserService userService, ApplicationDbContext db)
        : base(userService)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var types = await _db.AppointmentTypes
            .Where(t => t.UserId == user.Id)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(types.Select(t => t.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var type = await _db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);
        if (type == null) return NotFound();

        return Ok(type.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentTypeDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var type = new AppointmentType { UserId = user.Id };
        dto.ApplyTo(type);

        _db.AppointmentTypes.Add(type);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = type.Id }, type.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentTypeDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var type = await _db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);
        if (type == null) return NotFound();

        dto.ApplyTo(type);
        await _db.SaveChangesAsync();

        return Ok(type.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var type = await _db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);
        if (type == null) return NotFound();

        _db.AppointmentTypes.Remove(type);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
