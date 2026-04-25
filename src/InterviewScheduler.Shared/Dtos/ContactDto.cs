using InterviewScheduler.Core.Enums;

namespace InterviewScheduler.Shared.Dtos;

public class ContactDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public int? HeadOfHouseId { get; set; }
    public int? SpouseId { get; set; }
}
