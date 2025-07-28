# Interview Scheduler Application Plan

## Overview
A comprehensive appointment scheduling application using ASP.NET Core with Blazor, containerized with Docker, and integrated with Google Calendar. The app will facilitate scheduling meetings between leaders (Bishop, Counselors) and congregation members via SMS links.

## Architecture & Technology Stack
- **Backend**: ASP.NET Core 8.0 Web API
- **Frontend**: Blazor Server or WebAssembly
- **Database**: SQLite for development, SQL Server option for production
- **Authentication**: ASP.NET Core Identity
- **Calendar Integration**: Google Calendar API
- **Containerization**: Docker with docker-compose
- **SMS**: SMS links using tel: protocol with pre-populated messages

## Database Schema

### Contacts
- Id (int, PK)
- FirstName (string)
- LastName (string)
- PhoneNumber (string)
- Gender (enum: Male/Female)
- BirthDate (DateTime)
- HeadOfHouseId (int, FK to Contacts)
- SpouseId (int, FK to Contacts)

### Leaders
- Id (int, PK)
- Name (string)
- Title (string) // Bishop, 1st Counselor, etc.
- GoogleCalendarId (string)
- IsActive (bool)

### AppointmentTypes
- Id (int, PK)
- Name (string) // Generic Meeting, Temple Recommend, etc.
- Duration (int) // minutes
- MessageTemplate (string)

### Appointments
- Id (int, PK)
- ContactId (int, FK)
- LeaderId (int, FK)
- AppointmentTypeId (int, FK)
- ScheduledTime (DateTime)
- GoogleEventId (string)
- Status (enum: Pending, Confirmed, Cancelled)
- CreatedDate (DateTime)

## Key Features Implementation

### 1. Contact Management
- CSV import functionality
- Family relationship linking (parents/children/spouses)
- Age calculation for minor detection
- Gender-based salutation (Brother/Sister)

### 2. Appointment Wizard
- Step 1: Select contacts with filtering (name, age, gender)
- Step 2: Select leader(s) to meet with
- Step 3: Choose appointment type and time range
- Step 4: Auto-schedule based on calendar availability
- Step 5: Generate SMS links with pre-populated messages

### 3. Google Calendar Integration
- OAuth2 authentication for calendar access
- Check availability across multiple calendars
- Create calendar events with appointment details
- Handle timezone considerations

### 4. SMS Link Generation
- Create SMS links with URL encoding
- Message templates with variable substitution
- Parent notification for minors (≤17 years)
- Bulk message generation

## Project Structure
```
InterviewScheduler/
├── src/
│   ├── InterviewScheduler.Web/          # Blazor UI
│   ├── InterviewScheduler.Api/          # Web API
│   ├── InterviewScheduler.Core/         # Domain models & interfaces
│   ├── InterviewScheduler.Infrastructure/ # Data access & external services
│   └── InterviewScheduler.Shared/       # Shared DTOs
├── tests/
│   ├── InterviewScheduler.Tests/
│   └── InterviewScheduler.IntegrationTests/
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## Implementation Steps

### Phase 1: Project Setup ✅
- [x] Create solution with Clean Architecture
- [x] Configure Entity Framework with SQLite
- [x] Set up dependency injection
- [x] Create initial project structure

### Phase 2: Contact Management ✅
- [x] Create models and database migrations
- [x] Implement CSV parser with relationship linking
- [x] Build contact CRUD operations
- [x] Add advanced contact filtering and search
- [x] Implement family relationship display with grouping
- [x] Add minor detection and parent notifications

### Phase 3: Calendar Integration ✅
- [x] Configure Google Calendar API credentials
- [x] Implement OAuth2 flow
- [x] Create calendar service for availability checking
- [x] Add calendar event creation
- [x] Build authentication UI components
- [x] Implement time slot generation

### Phase 4: Appointment Wizard UI ✅
- [x] Design multi-step wizard component (5 steps)
- [x] Implement contact selection with filters
- [x] Create scheduling algorithm with calendar integration
- [x] Build SMS link generator service
- [x] Add appointment management page
- [x] Implement appointment status tracking

### Phase 5: SMS Integration ✅
- [x] Create SMS service with phone validation
- [x] Implement message template system
- [x] Build SMS preview functionality
- [x] Add parent notification for minors
- [x] Create SMS testing interface

### Phase 6: Appointment Types Management ✅
- [x] Create appointment types CRUD interface
- [x] Implement message template system
- [x] Add default appointment type templates
- [x] Build template variable substitution

### Phase 7: Docker Configuration ✅
- [x] Create multi-stage Dockerfile
- [x] Configure docker-compose with app and database
- [x] Set up environment variables
- [x] Add health checks
- [x] Implement NGINX reverse proxy
- [x] Add SSL/HTTPS support for production
- [x] Create deployment documentation

### Phase 8: Testing & Polish ⏳
- [ ] Write unit tests for core functionality
- [ ] Add integration tests
- [ ] Implement comprehensive error handling
- [ ] Add logging and monitoring
- [ ] Performance optimization

## Security Considerations
- Secure storage of Google API credentials
- Phone number validation and sanitization
- Authorization for leader accounts
- Data encryption for sensitive information
- HTTPS enforcement
- Input validation and sanitization

## Progress Tracking
- **Status**: Core Implementation Complete
- **Current Phase**: Phase 8 - Testing & Polish
- **Last Updated**: 2025-07-28
- **Completion**: 85% (7 of 8 phases complete)

### Implemented Features Summary

#### ✅ Core Application Features
- **Contact Management**: CSV import, advanced filtering, family relationships
- **Leader Management**: CRUD operations, Google Calendar integration
- **Appointment Scheduling**: 5-step wizard with calendar integration
- **SMS Integration**: Link generation, phone validation, message templates
- **Appointment Types**: Configurable types with message templates
- **Calendar Integration**: OAuth2, availability checking, event creation

#### ✅ Technical Implementation
- **Architecture**: Clean Architecture with ASP.NET Core 8.0 + Blazor Server
- **Database**: Entity Framework with SQLite, relationship mapping
- **Authentication**: Google OAuth2 for calendar access
- **Deployment**: Docker containerization with production configuration
- **Security**: Input validation, phone sanitization, secure configuration

#### ✅ User Interface
- **Navigation**: Responsive menu with all major sections
- **Contact Views**: List view with family grouping option
- **Appointment Wizard**: Progressive 5-step scheduling process
- **SMS Preview**: Real-time message generation and testing
- **Appointment Management**: Status tracking and bulk operations

### Legend
- ⏳ Not Started
- 🚧 In Progress
- ✅ Completed
- ❌ Blocked

## Notes
- Consider implementing a notification system for appointment reminders
- May need to handle different time zones if leaders/members are in different locations
- Should implement audit logging for compliance
- Consider adding export functionality for appointment history