using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Shared.Dtos;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/contacts")]
public class ContactsController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICsvParserService _csv;

    public ContactsController(IUserService userService, ApplicationDbContext db, ICsvParserService csv)
        : base(userService)
    {
        _db = db;
        _csv = csv;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var contacts = await _db.Contacts
            .Include(c => c.HeadOfHouse)
            .Include(c => c.Spouse)
            .Where(c => c.UserId == user.Id)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();

        return Ok(contacts.Select(c => c.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var contact = await _db.Contacts
            .Include(c => c.HeadOfHouse)
            .Include(c => c.Spouse)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

        if (contact == null) return NotFound();

        return Ok(contact.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var contact = new Contact { UserId = user.Id };
        dto.ApplyTo(contact);

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = contact.Id }, contact.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContactDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (contact == null) return NotFound();

        dto.ApplyTo(contact);
        await _db.SaveChangesAsync();

        return Ok(contact.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (contact == null) return NotFound();

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        if (file.ContentType != "text/csv" && !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("File must be a CSV.");

        // Buffer the file so we can read it twice (parse + relationship link).
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var csvBytes = memory.ToArray();

        using var parseStream = new MemoryStream(csvBytes);
        var importedContacts = await _csv.ParseContactsCsvAsync(parseStream);

        if (importedContacts.Count == 0)
            return BadRequest("No contacts found in the CSV file.");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Phase 1: break circular FKs on existing contacts so they can be deleted.
        var existingContacts = await _db.Contacts
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        foreach (var contact in existingContacts)
        {
            contact.HeadOfHouseId = null;
            contact.SpouseId = null;
        }
        await _db.SaveChangesAsync();

        _db.Contacts.RemoveRange(existingContacts);

        foreach (var contact in importedContacts)
        {
            contact.UserId = user.Id;
        }

        await _db.Contacts.AddRangeAsync(importedContacts);
        await _db.SaveChangesAsync();

        // Phase 2: re-read the CSV to wire up HeadOfHouse / Spouse FKs now that IDs exist.
        using var linkStream = new MemoryStream(csvBytes);
        await _csv.LinkRelationshipsAsync(linkStream, importedContacts);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(importedContacts.Select(c => c.ToDto()).ToList());
    }
}
