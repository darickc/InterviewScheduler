using InterviewScheduler.Core.Entities;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Client.Mappings;

/// <summary>
/// Projects Shared DTOs back into Core POCO entities so client-side code can reuse
/// services that take entities (notably <c>ISmsService.GenerateAppointmentMessages</c>)
/// and computed entity members (<c>Contact.IsMinor</c>, <c>FullName</c>, etc.).
/// </summary>
public static class DtoMappings
{
    public static Contact ToContact(this ContactDto d) => new()
    {
        Id = d.Id,
        FirstName = d.FirstName,
        MiddleName = d.MiddleName,
        LastName = d.LastName,
        PhoneNumber = d.PhoneNumber,
        Gender = d.Gender,
        BirthDate = d.BirthDate,
        HeadOfHouseId = d.HeadOfHouseId,
        SpouseId = d.SpouseId
    };

    public static Leader ToLeader(this LeaderDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Title = d.Title,
        GoogleCalendarId = d.GoogleCalendarId,
        IsActive = d.IsActive
    };

    public static AppointmentType ToAppointmentType(this AppointmentTypeDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Duration = d.Duration,
        MessageTemplate = d.MessageTemplate,
        MinorMessageTemplate = d.MinorMessageTemplate,
        BufferTimeBeforeMinutes = d.BufferTimeBeforeMinutes,
        BufferTimeAfterMinutes = d.BufferTimeAfterMinutes,
        MinimumDurationMinutes = d.MinimumDurationMinutes,
        MaximumDurationMinutes = d.MaximumDurationMinutes,
        MinimumAdvanceBookingHours = d.MinimumAdvanceBookingHours,
        MaximumAdvanceBookingDays = d.MaximumAdvanceBookingDays,
        SchedulingPriority = d.SchedulingPriority,
        RequireStrictBufferTime = d.RequireStrictBufferTime,
        AllowWeekendScheduling = d.AllowWeekendScheduling,
        AllowAfterHoursScheduling = d.AllowAfterHoursScheduling,
        ColorCode = d.ColorCode
    };

    /// <summary>
    /// Projects a list of ContactDtos into Contact entities and wires up the
    /// HeadOfHouse / Spouse navigation properties from the Id references in the DTOs.
    /// </summary>
    public static Dictionary<int, Contact> ToContactGraph(this IEnumerable<ContactDto> dtos)
    {
        var dtosList = dtos as IList<ContactDto> ?? dtos.ToList();
        var byId = dtosList.ToDictionary(d => d.Id, d => d.ToContact());

        foreach (var dto in dtosList)
        {
            var c = byId[dto.Id];
            if (dto.HeadOfHouseId.HasValue && byId.TryGetValue(dto.HeadOfHouseId.Value, out var hoh))
                c.HeadOfHouse = hoh;
            if (dto.SpouseId.HasValue && byId.TryGetValue(dto.SpouseId.Value, out var sp))
                c.Spouse = sp;
        }

        return byId;
    }

    public static ContactDto ToDto(this Contact c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        MiddleName = c.MiddleName,
        LastName = c.LastName,
        PhoneNumber = c.PhoneNumber,
        Gender = c.Gender,
        BirthDate = c.BirthDate,
        HeadOfHouseId = c.HeadOfHouseId,
        SpouseId = c.SpouseId
    };

    public static LeaderDto ToDto(this Leader l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Title = l.Title,
        GoogleCalendarId = l.GoogleCalendarId,
        IsActive = l.IsActive
    };

    /// <summary>
    /// Builds an <see cref="Appointment"/> entity from an <see cref="AppointmentDto"/>
    /// using the supplied lookup dictionaries to wire up Contact / Leader / AppointmentType
    /// navigation properties. Returns null if any referenced entity is missing.
    /// </summary>
    public static Appointment? ToAppointment(
        this AppointmentDto d,
        IReadOnlyDictionary<int, Contact> contactsById,
        IReadOnlyDictionary<int, Leader> leadersById,
        IReadOnlyDictionary<int, AppointmentType> typesById)
    {
        if (!contactsById.TryGetValue(d.ContactId, out var contact)) return null;
        if (!leadersById.TryGetValue(d.LeaderId, out var leader)) return null;
        if (!typesById.TryGetValue(d.AppointmentTypeId, out var type)) return null;

        return new Appointment
        {
            Id = d.Id,
            ContactId = d.ContactId,
            Contact = contact,
            LeaderId = d.LeaderId,
            Leader = leader,
            AppointmentTypeId = d.AppointmentTypeId,
            AppointmentType = type,
            ScheduledTime = d.ScheduledTime,
            GoogleEventId = d.GoogleEventId,
            Status = d.Status,
            CreatedDate = d.CreatedDate
        };
    }
}
