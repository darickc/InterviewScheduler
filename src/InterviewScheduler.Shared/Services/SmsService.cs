using InterviewScheduler.Core.Entities;
using InterviewScheduler.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace InterviewScheduler.Shared.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public string GenerateSmsLink(string phoneNumber, string message)
    {
        var sanitizedPhone = SanitizePhoneNumbers(phoneNumber);
        var encodedMessage = Uri.EscapeDataString(message);

        // Use the standard SMS URI scheme
        // Format: sms:+1234567890,+1234567891?body=encoded_message (for multiple recipients)
        return $"sms:{sanitizedPhone}?body={encodedMessage}";
    }

    public string FormatMessage(AppointmentType appointmentType, Contact contact, Leader leader, DateTime scheduledTime)
    {
        // Select appropriate template based on whether contact is a minor
        var template = contact.IsMinor && !string.IsNullOrEmpty(appointmentType.MinorMessageTemplate)
            ? appointmentType.MinorMessageTemplate
            : appointmentType.MessageTemplate;

        if (string.IsNullOrEmpty(template))
        {
            return GetDefaultMessage(contact, leader, scheduledTime, appointmentType.Duration);
        }

        var message = template
            .Replace("{ContactName}", GetContactSalutation(contact))
            .Replace("{Salutation}", contact.Salutation)
            .Replace("{FirstName}", contact.FirstName)
            .Replace("{MiddleName}", contact.MiddleName ?? string.Empty)
            .Replace("{LastName}", contact.LastName)
            .Replace("{LeaderName}", leader.Name)
            .Replace("{LeaderTitle}", leader.Title)
            .Replace("{DateTime}", scheduledTime.ToString("dddd, MMMM dd 'at' h:mm tt"))
            .Replace("{Date}", scheduledTime.ToString("dddd, MMMM dd"))
            .Replace("{Day}", scheduledTime.ToString("dddd, MMMM dd"))
            .Replace("{Time}", scheduledTime.ToString("h:mm tt"))
            .Replace("{Duration}", appointmentType.Duration.ToString())
            .Replace("{AppointmentType}", appointmentType.Name);

        return message;
    }

    public List<SmsMessage> GenerateAppointmentMessages(List<Contact> contacts, Leader leader, AppointmentType appointmentType, DateTime scheduledTime)
    {
        var messages = new List<SmsMessage>();

        foreach (var contact in contacts)
        {
            var hasValidContactPhone = IsValidPhoneNumber(contact.PhoneNumber);
            var hasValidParentPhone = contact.IsMinor && contact.HeadOfHouse != null &&
                                    IsValidPhoneNumber(contact.HeadOfHouse.PhoneNumber);
            var hasValidAdultFallbackPhone = !contact.IsMinor && contact.HeadOfHouse != null &&
                                           IsValidPhoneNumber(contact.HeadOfHouse.PhoneNumber);

            // Skip contacts that have no valid way to reach them
            if (!hasValidContactPhone && !hasValidParentPhone && !hasValidAdultFallbackPhone)
            {
                _logger.LogWarning("No valid phone number for contact {ContactName}: Contact Phone: {ContactPhone}, Head of House Phone: {HeadOfHousePhone}",
                    contact.FullName, contact.PhoneNumber, contact.HeadOfHouse?.PhoneNumber);
                continue;
            }

            // Generate primary message for the contact if they have a valid phone number
            if (hasValidContactPhone)
            {
                var primaryMessage = FormatMessage(appointmentType, contact, leader, scheduledTime);

                messages.Add(new SmsMessage
                {
                    ContactName = contact.FullName,
                    PhoneNumber = contact.PhoneNumber!,
                    Message = primaryMessage,
                    SmsLink = GenerateSmsLink(contact.PhoneNumber!, primaryMessage),
                    IsMinorNotification = false
                });
            }

            // Generate fallback message for adults without phone numbers using head of household phone
            if (!hasValidContactPhone && hasValidAdultFallbackPhone)
            {
                var fallbackMessage = FormatMessage(appointmentType, contact, leader, scheduledTime);

                messages.Add(new SmsMessage
                {
                    ContactName = $"{contact.FullName} (via {contact.HeadOfHouse!.FullName})",
                    PhoneNumber = contact.HeadOfHouse!.PhoneNumber!,
                    Message = fallbackMessage,
                    SmsLink = GenerateSmsLink(contact.HeadOfHouse!.PhoneNumber!, fallbackMessage),
                    IsMinorNotification = false
                });
            }

            // Generate parent notification for minors
            if (contact.IsMinor && contact.HeadOfHouse != null)
            {
                var parentMessage = GenerateParentNotificationMessage(contact, leader, appointmentType, scheduledTime);
                var parentPhoneNumbers = new List<string>();
                var parentNames = new List<string>();

                // Add head of house phone if valid and different from contact's
                if (IsValidPhoneNumber(contact.HeadOfHouse.PhoneNumber) &&
                    contact.HeadOfHouse.PhoneNumber != contact.PhoneNumber)
                {
                    parentPhoneNumbers.Add(contact.HeadOfHouse.PhoneNumber!);
                    parentNames.Add(contact.HeadOfHouse.FullName);
                }

                // Add spouse phone if valid and not duplicate
                if (contact.HeadOfHouse.Spouse != null &&
                    IsValidPhoneNumber(contact.HeadOfHouse.Spouse.PhoneNumber) &&
                    contact.HeadOfHouse.Spouse.PhoneNumber != contact.PhoneNumber &&
                    !parentPhoneNumbers.Contains(contact.HeadOfHouse.Spouse.PhoneNumber!))
                {
                    parentPhoneNumbers.Add(contact.HeadOfHouse.Spouse.PhoneNumber!);
                    parentNames.Add(contact.HeadOfHouse.Spouse.FullName);
                }

                // Create single message for all parents if any valid phone numbers found
                if (parentPhoneNumbers.Any())
                {
                    var combinedPhoneNumbers = string.Join(",", parentPhoneNumbers);
                    var combinedParentNames = string.Join(" & ", parentNames);

                    messages.Add(new SmsMessage
                    {
                        ContactName = combinedParentNames,
                        PhoneNumber = combinedPhoneNumbers,
                        Message = parentMessage,
                        SmsLink = GenerateSmsLink(combinedPhoneNumbers, parentMessage),
                        IsMinorNotification = true,
                        ParentName = combinedParentNames
                    });
                }
            }
        }

        return messages;
    }

    public string SanitizePhoneNumber(string phoneNumber) => PhoneNumberHelper.SanitizePhoneNumber(phoneNumber);

    public string SanitizePhoneNumbers(string phoneNumbers) => PhoneNumberHelper.SanitizePhoneNumbers(phoneNumbers);

    public bool IsValidPhoneNumber(string? phoneNumber) => PhoneNumberHelper.IsValidPhoneNumber(phoneNumber);

    private string GetContactSalutation(Contact contact)
    {
        return $"{contact.Salutation} {contact.FullName}";
    }

    private string GetDefaultMessage(Contact contact, Leader leader, DateTime scheduledTime, int duration)
    {
        return $"Hello {GetContactSalutation(contact)}, " +
               $"you have been scheduled for a meeting with {leader.Name} " +
               $"on {scheduledTime:dddd, MMMM dd} at {scheduledTime:h:mm tt}. " +
               $"The meeting will last approximately {duration} minutes. " +
               $"Please confirm your attendance. Thank you!";
    }

    public string GenerateParentNotificationMessage(Contact child, Leader leader, AppointmentType appointmentType, DateTime scheduledTime)
    {
        // Use minor template if available, otherwise generate default parent notification
        if (!string.IsNullOrEmpty(appointmentType.MinorMessageTemplate))
        {
            var message = appointmentType.MinorMessageTemplate
                .Replace("{ContactName}", child.FullName)
                .Replace("{ChildName}", child.FullName)
                .Replace("{ParentName}", GetContactSalutation(child.HeadOfHouse!))
                .Replace("{Salutation}", child.Salutation)
                .Replace("{FirstName}", child.FirstName)
                .Replace("{MiddleName}", child.MiddleName ?? string.Empty)
                .Replace("{LastName}", child.LastName)
                .Replace("{LeaderName}", leader.Name)
                .Replace("{LeaderTitle}", leader.Title)
                .Replace("{DateTime}", scheduledTime.ToString("dddd, MMMM dd 'at' h:mm tt"))
                .Replace("{Date}", scheduledTime.ToString("dddd, MMMM dd"))
                .Replace("{Day}", scheduledTime.ToString("dddd, MMMM dd"))
                .Replace("{Time}", scheduledTime.ToString("h:mm tt"))
                .Replace("{Duration}", appointmentType.Duration.ToString())
                .Replace("{AppointmentType}", appointmentType.Name);

            return message;
        }

        // Default parent notification if no template provided
        return $"Hello {GetContactSalutation(child.HeadOfHouse!)}, " +
               $"your child {child.FullName} has been scheduled for a {appointmentType.Name.ToLower()} " +
               $"with {leader.Name} ({leader.Title}) " +
               $"on {scheduledTime:dddd, MMMM dd} at {scheduledTime:h:mm tt}. " +
               $"The appointment will last approximately {appointmentType.Duration} minutes. " +
               $"Please ensure they are available at this time. Thank you!";
    }
}
