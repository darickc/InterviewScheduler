using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Interfaces;

public interface ISmsService
{
    string GenerateSmsLink(string phoneNumber, string message);
    string FormatMessage(AppointmentType appointmentType, Contact contact, Leader leader, DateTime scheduledTime);
    string GenerateParentNotificationMessage(Contact child, Leader leader, AppointmentType appointmentType, DateTime scheduledTime);
    List<SmsMessage> GenerateAppointmentMessages(List<Contact> contacts, Leader leader, AppointmentType appointmentType, DateTime scheduledTime);
    string SanitizePhoneNumber(string phoneNumber);
    bool IsValidPhoneNumber(string phoneNumber);
}

public class SmsMessage
{
    public string ContactName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Message { get; set; } = "";
    public string SmsLink { get; set; } = "";
    public bool IsMinorNotification { get; set; } = false;
    public string? ParentName { get; set; }
}