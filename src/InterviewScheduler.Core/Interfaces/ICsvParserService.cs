using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Interfaces;

public interface ICsvParserService
{
    Task<List<Contact>> ParseContactsCsvAsync(Stream csvStream);
    Task LinkRelationshipsAsync(Stream csvStream, List<Contact> savedContacts);
}