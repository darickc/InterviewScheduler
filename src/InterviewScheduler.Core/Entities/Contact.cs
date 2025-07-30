using InterviewScheduler.Core.Enums;

namespace InterviewScheduler.Core.Entities;

public class Contact
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    
    // User relationship
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Family relationships
    public int? HeadOfHouseId { get; set; }
    public Contact? HeadOfHouse { get; set; }
    
    public int? SpouseId { get; set; }
    public Contact? Spouse { get; set; }
    
    // Navigation properties
    public ICollection<Contact> HouseholdMembers { get; set; } = new List<Contact>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    
    // Computed properties
    public string FullName => string.IsNullOrEmpty(MiddleName) 
        ? $"{LastName}, {FirstName}" 
        : $"{LastName}, {FirstName} {MiddleName}";
    public string DisplayName => $"{FirstName} {LastName}";
    public int Age => CalculateAge();
    public bool IsMinor => Age <= 17;
    public string Salutation => Gender == Gender.Male ? "Brother" : "Sister";
    
    private int CalculateAge()
    {
        var today = DateTime.Today;
        var age = today.Year - BirthDate.Year;
        if (BirthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}