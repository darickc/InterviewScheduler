using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Shared.Dtos;

namespace InterviewScheduler.Web.Mappings;

public static class EntityMappings
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name
    };

    public static LeaderDto ToDto(this Leader leader) => new()
    {
        Id = leader.Id,
        Name = leader.Name,
        Title = leader.Title,
        GoogleCalendarId = leader.GoogleCalendarId,
        IsActive = leader.IsActive
    };

    public static void ApplyTo(this LeaderDto dto, Leader leader)
    {
        leader.Name = dto.Name;
        leader.Title = dto.Title;
        leader.GoogleCalendarId = dto.GoogleCalendarId;
        leader.IsActive = dto.IsActive;
    }

    public static AppointmentTypeDto ToDto(this AppointmentType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        Duration = type.Duration,
        MessageTemplate = type.MessageTemplate,
        MinorMessageTemplate = type.MinorMessageTemplate,
        BufferTimeBeforeMinutes = type.BufferTimeBeforeMinutes,
        BufferTimeAfterMinutes = type.BufferTimeAfterMinutes,
        MinimumDurationMinutes = type.MinimumDurationMinutes,
        MaximumDurationMinutes = type.MaximumDurationMinutes,
        MinimumAdvanceBookingHours = type.MinimumAdvanceBookingHours,
        MaximumAdvanceBookingDays = type.MaximumAdvanceBookingDays,
        SchedulingPriority = type.SchedulingPriority,
        RequireStrictBufferTime = type.RequireStrictBufferTime,
        AllowWeekendScheduling = type.AllowWeekendScheduling,
        AllowAfterHoursScheduling = type.AllowAfterHoursScheduling,
        ColorCode = type.ColorCode
    };

    public static void ApplyTo(this AppointmentTypeDto dto, AppointmentType type)
    {
        type.Name = dto.Name;
        type.Duration = dto.Duration;
        type.MessageTemplate = dto.MessageTemplate;
        type.MinorMessageTemplate = dto.MinorMessageTemplate;
        type.BufferTimeBeforeMinutes = dto.BufferTimeBeforeMinutes;
        type.BufferTimeAfterMinutes = dto.BufferTimeAfterMinutes;
        type.MinimumDurationMinutes = dto.MinimumDurationMinutes;
        type.MaximumDurationMinutes = dto.MaximumDurationMinutes;
        type.MinimumAdvanceBookingHours = dto.MinimumAdvanceBookingHours;
        type.MaximumAdvanceBookingDays = dto.MaximumAdvanceBookingDays;
        type.SchedulingPriority = dto.SchedulingPriority;
        type.RequireStrictBufferTime = dto.RequireStrictBufferTime;
        type.AllowWeekendScheduling = dto.AllowWeekendScheduling;
        type.AllowAfterHoursScheduling = dto.AllowAfterHoursScheduling;
        type.ColorCode = dto.ColorCode;
    }

    public static ContactDto ToDto(this Contact contact) => new()
    {
        Id = contact.Id,
        FirstName = contact.FirstName,
        MiddleName = contact.MiddleName,
        LastName = contact.LastName,
        PhoneNumber = contact.PhoneNumber,
        Gender = contact.Gender,
        BirthDate = contact.BirthDate,
        HeadOfHouseId = contact.HeadOfHouseId,
        SpouseId = contact.SpouseId
    };

    public static void ApplyTo(this ContactDto dto, Contact contact)
    {
        contact.FirstName = dto.FirstName;
        contact.MiddleName = dto.MiddleName;
        contact.LastName = dto.LastName;
        contact.PhoneNumber = dto.PhoneNumber;
        contact.Gender = dto.Gender;
        contact.BirthDate = dto.BirthDate;
        contact.HeadOfHouseId = dto.HeadOfHouseId;
        contact.SpouseId = dto.SpouseId;
    }

    public static AppointmentDto ToDto(this Appointment appt) => new()
    {
        Id = appt.Id,
        ContactId = appt.ContactId,
        LeaderId = appt.LeaderId,
        AppointmentTypeId = appt.AppointmentTypeId,
        ScheduledTime = appt.ScheduledTime,
        GoogleEventId = appt.GoogleEventId,
        Status = appt.Status,
        CreatedDate = appt.CreatedDate,
        ContactName = appt.Contact?.DisplayName ?? string.Empty,
        LeaderName = appt.Leader?.Name ?? string.Empty,
        AppointmentTypeName = appt.AppointmentType?.Name ?? string.Empty,
        DurationMinutes = appt.AppointmentType?.Duration ?? 0
    };

    public static CalendarInfoDto ToDto(this CalendarInfo info) => new()
    {
        Id = info.Id,
        Name = info.Name,
        Description = info.Description,
        IsPrimary = info.IsPrimary
    };

    public static TimeSlotDto ToDto(this TimeSlot slot) => new()
    {
        StartTime = slot.StartTime,
        EndTime = slot.EndTime,
        IsAvailable = slot.IsAvailable,
        LeaderId = slot.LeaderId,
        LeaderName = slot.LeaderName
    };
}
