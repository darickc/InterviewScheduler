using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Enums;
using InterviewScheduler.Core.Interfaces;

namespace InterviewScheduler.Infrastructure.Services;

public class CsvParserService : ICsvParserService
{
    public async Task<List<Contact>> ParseContactsCsvAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Context.RegisterClassMap<ContactCsvMap>();
        
        var records = new List<ContactCsvRecord>();
        await foreach (var record in csv.GetRecordsAsync<ContactCsvRecord>())
        {
            records.Add(record);
        }
        var contacts = new List<Contact>();
        var seenNames = new HashSet<string>();
        
        // First pass: Create all contacts without relationships, avoiding duplicates
        foreach (var record in records)
        {
            // Skip if we've already processed this name
            if (seenNames.Contains(record.Name))
                continue;
                
            seenNames.Add(record.Name);
            
            var contact = new Contact
            {
                FirstName = ExtractFirstName(record.Name),
                LastName = ExtractLastName(record.Name),
                PhoneNumber = record.IndividualPhone,
                Gender = ParseGender(record.Gender),
                BirthDate = ParseBirthDate(record.BirthDate)
            };
            
            contacts.Add(contact);
        }
        
        return contacts;
    }

    public async Task LinkRelationshipsAsync(Stream csvStream, List<Contact> savedContacts)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Context.RegisterClassMap<ContactCsvMap>();
        
        var records = new List<ContactCsvRecord>();
        await foreach (var record in csv.GetRecordsAsync<ContactCsvRecord>())
        {
            records.Add(record);
        }

        var contactMap = savedContacts
            .GroupBy(c => c.FullName)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Group records by head of house for more efficient processing
        var householdGroups = records
            .Where(r => !string.IsNullOrEmpty(r.HeadOfHouse))
            .GroupBy(r => r.HeadOfHouse)
            .ToList();

        // Process each household group
        foreach (var household in householdGroups)
        {
            var headOfHouseName = household.Key;
            
            // Find the head of house contact
            if (!contactMap.TryGetValue(headOfHouseName, out var headOfHouseContact))
                continue;

            // Link all family members to the head of house (excluding the head themselves)
            foreach (var record in household.Where(r => r.Name != headOfHouseName))
            {
                if (contactMap.TryGetValue(record.Name, out var familyMember))
                {
                    familyMember.HeadOfHouseId = headOfHouseContact.Id;
                }
            }
        }

        // Link spouses for all contacts
        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.SpouseName) && 
                contactMap.TryGetValue(record.Name, out var contact) &&
                contactMap.TryGetValue(record.SpouseName, out var spouse))
            {
                contact.SpouseId = spouse.Id;
            }
        }
    }
    
    private string ExtractFirstName(string fullName)
    {
        var parts = fullName.Split(',');
        return parts.Length > 1 ? parts[1].Trim() : string.Empty;
    }
    
    private string ExtractLastName(string fullName)
    {
        var parts = fullName.Split(',');
        return parts.Length > 0 ? parts[0].Trim() : fullName;
    }
    
    private Gender ParseGender(string gender)
    {
        return gender?.ToUpperInvariant() == "M" ? Gender.Male : Gender.Female;
    }
    
    private DateTime ParseBirthDate(string birthDate)
    {
        if (DateTime.TryParse(birthDate, out var date))
            return date;
        
        // Try parsing with custom formats
        var formats = new[] { "dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "d MMMM yyyy" };
        if (DateTime.TryParseExact(birthDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;
        
        return DateTime.Today; // Default if parsing fails
    }
}

public class ContactCsvRecord
{
    public string Name { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string IndividualPhone { get; set; } = string.Empty;
    public string HeadOfHouse { get; set; } = string.Empty;
    public string SpouseName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
}

public class ContactCsvMap : ClassMap<ContactCsvRecord>
{
    public ContactCsvMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.BirthDate).Name("Birth Date");
        Map(m => m.IndividualPhone).Name("Individual Phone");
        Map(m => m.HeadOfHouse).Name("Head of House");
        Map(m => m.SpouseName).Name("Spouse Name");
        Map(m => m.Gender).Name("Gender");
    }
}