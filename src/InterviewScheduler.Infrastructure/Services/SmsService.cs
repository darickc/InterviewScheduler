using System.Text.RegularExpressions;
using System.Web;
using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InterviewScheduler.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private static readonly Regex PhoneRegex = new(@"^\+?1?[-.\s]?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})$", RegexOptions.Compiled);

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public string GenerateSmsLink(string phoneNumber, string message)
    {
        var sanitizedPhone = SanitizePhoneNumber(phoneNumber);
        var encodedMessage = Uri.EscapeDataString(message);
        
        // Use the standard SMS URI scheme
        // Format: sms:+1234567890?body=encoded_message
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

            // Skip contacts that have no valid way to reach them
            if (!hasValidContactPhone && !hasValidParentPhone)
            {
                _logger.LogWarning("No valid phone number for contact {ContactName}: Contact Phone: {ContactPhone}, Parent Phone: {ParentPhone}", 
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

            // Generate parent notification for minors
            if (hasValidParentPhone && contact.HeadOfHouse.PhoneNumber != contact.PhoneNumber)
            {
                var parentMessage = GenerateParentNotificationMessage(contact, leader, appointmentType, scheduledTime);
                
                messages.Add(new SmsMessage
                {
                    ContactName = contact.HeadOfHouse.FullName,
                    PhoneNumber = contact.HeadOfHouse.PhoneNumber!,
                    Message = parentMessage,
                    SmsLink = GenerateSmsLink(contact.HeadOfHouse.PhoneNumber!, parentMessage),
                    IsMinorNotification = true,
                    ParentName = contact.HeadOfHouse.FullName
                });
            }
        }

        return messages;
    }

    public string SanitizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "";

        // Remove all non-numeric characters except + at the start
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");
        
        // Handle US phone numbers
        if (cleaned.Length == 10)
        {
            // Add +1 for US numbers
            return "+1" + cleaned;
        }
        else if (cleaned.Length == 11 && cleaned.StartsWith("1"))
        {
            // Add + if missing
            return "+" + cleaned;
        }
        else if (cleaned.StartsWith("+1") && cleaned.Length == 12)
        {
            // Already properly formatted
            return cleaned;
        }
        else if (cleaned.StartsWith("+"))
        {
            // International number
            return cleaned;
        }
        
        // Default: try to add +1 for US
        return "+1" + Regex.Replace(cleaned, @"[^\d]", "");
    }

    public bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var sanitized = SanitizePhoneNumber(phoneNumber);
        
        // Check if it's a valid US phone number (+1 + 10 digits)
        return Regex.IsMatch(sanitized, @"^\+1\d{10}$") || 
               Regex.IsMatch(sanitized, @"^\+\d{10,15}$"); // International format
    }

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

    private string GenerateParentNotificationMessage(Contact child, Leader leader, AppointmentType appointmentType, DateTime scheduledTime)
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