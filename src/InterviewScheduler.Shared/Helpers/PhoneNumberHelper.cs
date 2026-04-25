using System.Text.RegularExpressions;

namespace InterviewScheduler.Shared.Helpers;

public static class PhoneNumberHelper
{
    public static string SanitizePhoneNumber(string phoneNumber)
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

    public static string SanitizePhoneNumbers(string phoneNumbers)
    {
        if (string.IsNullOrWhiteSpace(phoneNumbers))
            return "";

        // Split by comma or semicolon, sanitize each number, then join with comma
        var numbers = phoneNumbers.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(num => SanitizePhoneNumber(num.Trim()))
                                 .Where(num => !string.IsNullOrEmpty(num))
                                 .Distinct(); // Remove duplicates

        return string.Join(",", numbers);
    }

    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var sanitized = SanitizePhoneNumber(phoneNumber);

        // Check if it's a valid US phone number (+1 + 10 digits)
        return Regex.IsMatch(sanitized, @"^\+1\d{10}$") ||
               Regex.IsMatch(sanitized, @"^\+\d{10,15}$"); // International format
    }
}
