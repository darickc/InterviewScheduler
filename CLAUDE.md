# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
InterviewScheduler is a Blazor Server application for scheduling appointments between leaders and congregation members. It integrates with Google Calendar for availability checking and generates SMS links for appointment notifications.

## Architecture
- **Clean Architecture**: Core (entities/interfaces) → Infrastructure (data/services) → Web (UI)
- **Tech Stack**: ASP.NET Core 9.0, Blazor Server, Entity Framework Core, SQLite
- **Authentication**: Google OAuth2 with calendar scope for API access
- **Key Services**: GoogleCalendarService, SmsService, CsvParserService, UserService

## Development Commands

### Run Locally
```bash
cd src/InterviewScheduler.Web
dotnet run
# Access at https://localhost:7094 or http://localhost:5269
```

### Database Migrations
```bash
# Create new migration
cd src/InterviewScheduler.Web
dotnet ef migrations add <MigrationName> -p ../InterviewScheduler.Infrastructure -s .

# Apply migrations (auto-applied on startup in Program.cs)
dotnet ef database update -p ../InterviewScheduler.Infrastructure -s .
```

### Docker Development
```bash
# Build and run with Docker Compose
docker-compose up --build

# Production mode with nginx
docker-compose --profile production up --build
```

## Key Implementation Patterns

### Google Calendar Integration
- OAuth2 flow configured in Program.cs with calendar scope
- Tokens saved for API access via `SaveTokens = true`
- GoogleCalendarService handles availability checking and event creation
- Calendar credentials stored in CalendarCredentials/ directory

### Multi-User Support
- Each user authenticated via Google has their own data scope
- User entity links to all user-specific data (contacts, appointments, leaders)
- UserService handles user creation/retrieval on authentication

### SMS Notification System
- Generates SMS links with pre-populated messages using tel: protocol
- Special handling for minors (≤17 years) - notifies parents
- Phone number sanitization in SmsService.SanitizePhoneNumbers()

### Appointment Wizard Flow
1. Contact selection with family grouping support
2. Leader selection with calendar availability
3. Time slot selection from available slots
4. SMS generation with message templates
5. Calendar event creation

### Family Relationships
- Contacts linked via HeadOfHouseId and SpouseId
- Minor detection based on birthdate
- Family grouping display in contact list
- Parent notification for appointments with minors

## Configuration
- **Google API**: Set GoogleCalendar__ClientId and GoogleCalendar__ClientSecret
- **Database**: SQLite by default at interviewscheduler.db
- **User Secrets**: Development uses UserSecretsId for sensitive config

## Testing Approach
When implementing tests:
- Unit tests for services (especially SmsService, GoogleCalendarService)
- Integration tests for appointment scheduling workflow
- Consider mocking Google Calendar API for reliable tests