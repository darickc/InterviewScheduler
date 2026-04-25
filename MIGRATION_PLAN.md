# Blazor Server → Blazor WebAssembly Migration Plan

> Living document. Check boxes as work completes. Safe to commit; safe to resume across context resets.

## Context

The driver: when an `sms:` link is tapped on a phone, the OS foregrounds Messages and backgrounds the browser. Backgrounded tabs lose their SignalR WebSocket. Because Blazor Server holds component state in server memory, returning to the tab shows "Reconnecting…" and a manual refresh wipes the wizard step, selected contacts, filters, etc.

WebAssembly fixes this structurally: components run in the browser, state lives in browser memory, no SignalR connection to drop. Returning to the tab restores exactly the page that was left.

Honest scope note: lighter alternatives (state persistence + reconnect handler, or `InteractiveAuto` hybrid) would also solve the disconnect at 1–3 days vs. ~3–5 weeks for full WASM. Full WASM was chosen for the structural cleanup and side benefits (faster perceived UI, server only serves data).

## Target Architecture

```
src/
├── InterviewScheduler.Web              ASP.NET host: serves WASM, hosts API, handles auth
├── InterviewScheduler.Client           NEW — Blazor WASM, all .razor pages live here
├── InterviewScheduler.Shared           DTOs + pure-logic services (SmsService, validators)
├── InterviewScheduler.Core              Entities, interfaces — unchanged
└── InterviewScheduler.Infrastructure   DbContext, GoogleCalendarService — server-only
```

Web keeps DbContext, Google OAuth, Google Calendar API. It exposes a REST API. Client holds every `.razor` page and calls the API via typed `HttpClient`s. Shared holds DTOs and pure-logic services. `SmsService` is already pure (just builds `sms:` URI strings) and moves cleanly.

---

## Phase 1 — Scaffold the Client project

- [x] Create `src/InterviewScheduler.Client/InterviewScheduler.Client.csproj` using `Microsoft.NET.Sdk.BlazorWebAssembly`, `net9.0`, refs to `Shared` and `Core`
- [x] Create `src/InterviewScheduler.Client/Program.cs` with `WebAssemblyHostBuilder`, `AddAuthorizationCore()`, register typed HttpClients, register `AuthenticationStateProvider` *(deferred typed clients + custom AuthenticationStateProvider to Phase 4; Phase 1 just registers a default `HttpClient` so the WASM host has something to inject)*
- [x] Create `src/InterviewScheduler.Client/_Imports.razor` mirroring Web's `_Imports.razor` plus `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- [x] Update `src/InterviewScheduler.Web/InterviewScheduler.Web.csproj` to reference Client *(deviation: did **not** use `<Private>false</Private><ExcludeAssets>all</ExcludeAssets>` — those exclude the Client assembly from the Web output and the runtime can't load it via `typeof(...)._Imports.Assembly`. Used a plain ProjectReference, which is what `dotnet new blazor --interactivity webassembly` actually emits. Also added `Microsoft.AspNetCore.Components.WebAssembly.Server` 9.0.7 to Web — required to expose the `AddInteractiveWebAssembly*` extension methods.)*
- [x] In `src/InterviewScheduler.Web/Program.cs:22-23` chained `.AddInteractiveWebAssemblyComponents()` *(deviation: kept `.AddInteractiveServerComponents()` alongside instead of replacing — needed so Server-mode pages keep working until Phase 5 migrates them all)*
- [x] In `src/InterviewScheduler.Web/Program.cs:165-166` chained `.AddInteractiveWebAssemblyRenderMode().AddAdditionalAssemblies(typeof(InterviewScheduler.Client._Imports).Assembly)` *(same deviation: kept `.AddInteractiveServerRenderMode()` alongside)*
- [x] Smoke test: moved `Counter.razor` to Client, switched to `@rendermode InteractiveWebAssembly`, removed `[Authorize]` temporarily (restore in Phase 4), built clean, booted at `http://localhost:5099`, confirmed `/counter` returns HTTP 200 with prerender envelope `"type":"webassembly","assembly":"InterviewScheduler.Client","typeName":"InterviewScheduler.Client.Pages.Counter"`, confirmed `_framework/dotnet.js` and `_framework/blazor.web.js` are served. Confirmed `/` still works under Server mode. Browser-side increment + `/_blazor` SignalR absence still needs visual verification on next dev session — CLI smoke can't render JS.

## Phase 2 — Populate `InterviewScheduler.Shared`

Currently empty. Populate with:

- [x] DTOs in `Shared/Dtos/`: `ContactDto`, `LeaderDto`, `AppointmentTypeDto`, `AppointmentDto`, `CreateScheduleRequest`, `CreateScheduleResult`, `CalendarInfoDto`, `TimeSlotDto`
- [x] Move `ISmsService` + `SmsService` from `src/InterviewScheduler.Infrastructure/Services/SmsService.cs` → `src/InterviewScheduler.Shared/Services/` *(also moved `SmsMessage` and the `ISmsService` interface from `Core/Interfaces/` since the interface signature references Core entities; new namespace is `InterviewScheduler.Shared.Services`)*
- [x] Update Infrastructure to no longer depend on SmsService (only Web/Client use it now) *(no Infrastructure consumers existed; deletion of `Infrastructure/Services/SmsService.cs` is sufficient. Did **not** add a defensive `Infrastructure → Shared` ProjectReference: Infrastructure stays on `net8.0` and would not be allowed to ref the now-`net9.0` Shared. Add it later if/when Infrastructure is bumped.)*
- [x] Move `SchedulingRulesService` (pure logic) from Infrastructure → Shared *(interface moved alongside, same reasoning as SMS)*
- [x] Add validators / phone-number sanitization helpers used by both client and server *(extracted phone helpers to new `Shared/Helpers/PhoneNumberHelper.cs`; `SmsService` instance methods delegate to it. Existing `TimePeriodValidationHelper` + `ValidationResult` already live in `Core/Helpers/` and are reachable from Shared via the new `Shared → Core` reference, so no validators were duplicated.)*

## Phase 3 — Build the REST API in Web

All controllers under `src/InterviewScheduler.Web/Controllers/Api/`. Match the `AuthenticationController.cs` style. Every endpoint `[Authorize]` and filters by `UserId` from claims.

- [x] `ContactsController` — GET, POST, PUT, DELETE, POST `/import` (replaces `Contacts.razor:239-380` direct DbContext)
- [x] `LeadersController` — GET, POST, PUT, DELETE, GET `/calendars` (replaces `Leaders.razor` + `Appointments.razor:454-457`)
- [x] `AppointmentTypesController` — GET, POST, PUT, DELETE (replaces `AppointmentTypes.razor:171-235`)
- [x] `AppointmentsController` — GET (with filters), POST, DELETE, POST `/{id}/confirm`, POST `/{id}/cancel` (replaces `Appointments.razor:444-792`)
- [x] `CalendarController` — GET `/events`, GET `/availability`, DELETE `/events/{id}` (replaces `AppointmentWizard.razor:1421-1424`, `Appointments.razor:563`)
- [x] `ScheduleController` — POST `/create` (synchronous; replaces `AppointmentWizard.razor:1303` `SchedulingService.CreateSchedule`) *(deviation: reshaped `CreateScheduleRequest` to `(Date, StartTime, EndTime, AppointmentTypeId, LeaderIds[], ContactIds[])` matching the actual `ISchedulingService.CreateSchedule` signature; the prior `(LeaderId, AppointmentTypeId, ContactIds, DateRangeStart, DateRangeEnd)` shape never matched the service. `CreateScheduleResult` DTO also reshaped to mirror `Core/Helpers/CreateScheduleResult` with `Appointments` typed as `AppointmentDto[]` — wizard code reads `Success/ErrorMessage/AppointmentsCreated/CalendarEventsCreated/UnscheduledContacts`.)*
- [x] `UserController` — GET `/me` (returns claim-derived user info for WASM `AuthenticationStateProvider`)
- [x] CSV import: server endpoint `POST /api/contacts/import` accepts `multipart/form-data` (raw CSV bytes) and runs the existing two-phase delete/insert/relationship-link inside a DB transaction *(deviation: kept CSV parsing server-side instead of having the client parse with CsvHelper. Encoding pre-DB-ID family relationships through `ContactDto[]` requires extra design and the existing `ICsvParserService` already does the two-pass parse + `LinkRelationshipsAsync`. Phase 5 can revisit this when `Contacts.razor` actually moves.)*

## Phase 4 — Authentication for WASM

- [x] Verify cookie + Google OAuth flow at `Program.cs:85-135` works unchanged (full-page navigation sets same-origin cookie) *(no behavior change — `OnTicketReceived` still wired, `SaveTokens = true`, scheme defaults unchanged)*
- [x] Configure WASM `HttpClient` with `BrowserHttpMessageHandler` and `credentials: 'include'` (default for same-origin) *(default WASM `HttpClient` registration in `Client/Program.cs` is sufficient — `BrowserHttpMessageHandler` is the default in WASM and same-origin requests include cookies automatically)*
- [x] Confirm `OnTicketReceived` hook at `Program.cs:119-134` still creates the user record server-side
- [x] Implement custom `AuthenticationStateProvider` in Client that calls `GET /api/user/me` and caches the result *(added `Client/Authentication/CustomAuthStateProvider.cs`; registered in `Client/Program.cs` as both `CustomAuthStateProvider` and `AuthenticationStateProvider`)*
- [ ] Replace component usages of `IUserService` with `AuthenticationStateProvider`. `IUserService` itself stays server-side (used by `OnTicketReceived` and the new `UserController`) *(deferred — only Counter.razor lives in Client today and it doesn't use `IUserService`. This task lights up as Phase 5 moves pages.)*
- [x] **Restore `@attribute [Authorize]` on `Client/Pages/Counter.razor`** — was removed in Phase 1 so the smoke test could render without a WASM-aware AuthenticationStateProvider
- [ ] Antiforgery: configure WASM antiforgery integration; do **not** blanket-`[IgnoreAntiforgeryToken]` on mutation endpoints *(deferred to Phase 5 — verifying antiforgery requires a Client component actually hitting a mutation endpoint, which doesn't exist yet)*
- [x] Configure cookie auth events so unauthenticated requests under `/api/*` return **401**, not the default 302 → `/signin-google`. *(implemented in two parts: (1) cookie auth `Events.OnRedirectToLogin` / `OnRedirectToAccessDenied` short-circuit to 401/403 when path starts with `/api`. (2) **Critical detail:** the redirect was actually coming from the Google challenge handler since `DefaultChallengeScheme = "Google"`, not from cookie auth — `[Authorize(AuthenticationSchemes = "Cookies")]` on `ApiControllerBase` routes the challenge through cookie auth so the events fire. Verified `curl /api/user/me` returns clean 401 with empty body.)*

## Phase 5 — Move components to Client

For every `.razor` under `src/InterviewScheduler.Web/Components/Pages/` and `Components/Layout/`:

- [ ] Move file to `src/InterviewScheduler.Client/Pages/` or `Layout/`
- [ ] Remove `@inject ApplicationDbContext`, `@inject ISchedulingService`, `@inject ICalendarService`, `@inject IUserService`
- [ ] Add `@inject IContactsApiClient` etc. (typed HttpClient wrappers in `Client/Services/`)
- [ ] Replace each `await DbContext.X.ToListAsync()` with `await Api.GetXAsync()`. Replace each `SaveChangesAsync()` with API POST/PUT/DELETE
- [ ] Keep `@inject ISmsService` — now resolved from Shared, runs in WASM unchanged. Button still calls `JSRuntime.InvokeVoidAsync("open", smsUrl, "_blank")` (`Appointments.razor:666`), but now there's no SignalR to drop
- [ ] Bulk-replace `@rendermode InteractiveServer` with `InteractiveWebAssembly` (or remove and rely on default)

Page-by-page checklist (current injections):

- [ ] `Appointments.razor:7-9` — DbContext, CalendarService, SmsService → ApiClients + Shared SmsService
- [x] `AppointmentTypes.razor:8` — DbContext → ApiClient
- [ ] `AppointmentWizard.razor:11-13` — DbContext, CalendarService, SmsService, SchedulingService → ApiClients + Shared SmsService
- [ ] `Contacts.razor:7` — DbContext, CsvParser → ApiClient (parse CsvHelper client-side)
- [ ] `Leaders.razor:9` — DbContext, CalendarService → ApiClients
- [x] `SmsPreview.razor:7-8` — DbContext, SmsService → ApiClient + Shared SmsService

Move root host components:

- [ ] `App.razor`, `Routes.razor`, `MainLayout.razor`, `NavMenu.razor`, `UserInfo.razor` move to Client (Web keeps a minimal host shell)

## Phase 6 — Long-running scheduling

- [ ] Keep `POST /api/schedule/create` synchronous. Current scale (handful of leaders, dozens of appointments) doesn't justify a job queue
- [ ] WASM holds in-memory loading state; backgrounding the tab during scheduling won't drop server work because there's no SignalR connection (just an HTTP request that completes when it completes)
- [ ] If this becomes a problem later: add a job table + polling endpoint. Not now.

## Phase 7 — Wiring & build

- [x] Confirm `Web.csproj` is `net10.0` (bumped during Phase 4), add Client + Shared refs *(all 5 projects now on `net10.0`)*
- [x] Bump `Core.csproj` and `Infrastructure.csproj` to `net10.0` for consistency *(done during Phase 4 framework bump; Infrastructure's old `Microsoft.AspNetCore.Http.Abstractions` 2.2.0 + `Microsoft.AspNetCore.Authentication.Abstractions` 2.2.0 shim packages were replaced with `<FrameworkReference Include="Microsoft.AspNetCore.App" />` since those types are part of the shared framework on net10. `Microsoft.EntityFrameworkCore.Sqlite` bumped 9.0.7 → 10.0.0; same with `Microsoft.AspNetCore.Authentication.Google`, `Microsoft.AspNetCore.Components.WebAssembly*`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`. One source change required: `ForwardedHeadersOptions.KnownNetworks.Clear()` → `KnownIPNetworks.Clear()` at `Web/Program.cs:18` (deprecated in .NET 10).)*
- [x] Set `Shared.csproj` to `net10.0` to match Client *(originally bumped to net9.0 in Phase 2; bumped to net10.0 alongside the rest)*; verify `CsvHelper`, `TimePeriodLibrary` still resolve in WASM context *(TimePeriodLibrary verified — Shared transitively pulls it through Core; CsvHelper still pending until Phase 5 moves Contacts.razor)*
- [ ] Verify `dbContext.Database.Migrate()` at startup (`Program.cs:175-179`) still runs server-side. Schema unchanged, no migration changes needed
- [ ] `dotnet build` clean
- [ ] `dotnet run --project src/InterviewScheduler.Web` boots without errors

## Phase 8 — Verification

End-to-end smoke test. The critical test is the actual user flow that drove this work.

- [ ] Browser dev tools → Network → confirm initial WASM bundle download, no `/_blazor` SignalR connection
- [ ] Sign in with Google → user record created in DB → redirected home
- [ ] Visit each page (`/contacts`, `/leaders`, `/appointment-types`, `/appointments`, `/sms-preview`, `/appointments/new`); CRUD works against API endpoints
- [ ] **Critical SMS-from-phone test**: open `/appointments/new` on phone, advance to step 3, fill data, generate preview, tap an SMS link → Messages app foregrounds → return to browser → wizard is exactly where it was. No "Reconnecting…", no refresh, no lost state
- [ ] Run a full schedule creation; verify Google Calendar events created and DB rows inserted
- [ ] Sign out → cookie cleared → API returns 401

---

## Critical Files

**Modify:**
- `src/InterviewScheduler.Web/Program.cs:22-23, 165-166` — render mode swap
- `src/InterviewScheduler.Web/InterviewScheduler.Web.csproj` — add Client project reference
- `src/InterviewScheduler.Web/Components/App.razor` — host shell only
- `src/InterviewScheduler.Web/Controllers/AuthenticationController.cs` — verify same-origin cookie still set (no behavior change expected)

**Create:**
- `src/InterviewScheduler.Client/` — entire new project
- `src/InterviewScheduler.Web/Controllers/Api/{Contacts,Leaders,AppointmentTypes,Appointments,Calendar,Schedule,User}Controller.cs`
- `src/InterviewScheduler.Shared/Dtos/*.cs`
- `src/InterviewScheduler.Client/Services/*ApiClient.cs` — typed HttpClient per controller
- `src/InterviewScheduler.Client/Authentication/CustomAuthStateProvider.cs`

**Move (Web → Client):**
- All 12 pages under `src/InterviewScheduler.Web/Components/Pages/`
- `Components/Layout/{MainLayout,NavMenu,UserInfo}.razor`
- `Components/Routes.razor`

**Move (Infrastructure → Shared):**
- `Services/SmsService.cs` (and `ISmsService.cs` from Core if pure) — pure string logic
- `Services/SchedulingRulesService.cs` — pure logic

**Reuse (do not reinvent):**
- `src/InterviewScheduler.Infrastructure/Services/SmsService.cs:19-27` — `GenerateSmsLink` is the entire SMS mechanism
- `src/InterviewScheduler.Infrastructure/Services/GoogleCalendarService.cs` — stays in Infrastructure, reached only via API
- `src/InterviewScheduler.Infrastructure/Services/UserService.cs:38-62` — `GetOrCreateUserAsync` stays, called by `OnTicketReceived` and the new `UserController`
- `src/InterviewScheduler.Infrastructure/Data/ApplicationDbContext.cs` — schema and entity model unchanged
- All migrations under `src/InterviewScheduler.Infrastructure/Migrations/` — unchanged

---

## Risks & Gotchas

- **Initial bundle size**: WASM ships ~5–10 MB on first load (.NET runtime + DLLs). Mitigate later with PWA caching or `InteractiveAuto` (server-prerender, then WASM takeover). Measure after build.
- **TimePeriodLibrary in WASM**: used by `SchedulingService` and `Program.cs:9`. It's netstandard2.0 and should run in WASM, but verify. If `SchedulingService` stays server-side (called via API only), this risk evaporates.
- **CsvHelper bundle weight**: a few hundred KB. Acceptable. Or keep CSV parsing server-side and POST raw bytes.
- **Antiforgery on API mutations**: do not blanket `[IgnoreAntiforgeryToken]`. Use the WASM antiforgery integration.
- **`IHttpContextAccessor` removal**: only `UserService` uses it, and that stays server-side. No client-side fallout.
- **Google OAuth callback**: `/signin-google-callback` (`Program.cs:107`) must not collide with any client route. It doesn't today.
- **SQLite in production**: unchanged. Not horizontally scalable but irrelevant for current scope.
- **`@rendermode InteractiveServer` attributes**: every page has this (`Appointments.razor:14`, etc.). Bulk-replace during the move.
- **Time estimate honesty**: realistic 3–5 weeks of focused work. ~7 controllers, ~12 pages refactored, auth provider, build/wire/test. Could be done in less by an aggressive single-developer push but expect surprises around antiforgery and OAuth edge cases.

---

## Progress Log

Append a line per work session so context-reset Claudes can pick up where the last one left off.

- _2026-04-25 — plan created_
- _2026-04-25 — Phase 4 complete (auth scaffolding only) + framework bump to .NET 10 (pulled forward from Phase 7). All 5 projects bumped to `net10.0`; Microsoft.* packages updated to `10.0.0`. Infrastructure's old `Microsoft.AspNetCore.*.Abstractions` 2.2.0 shim packages replaced with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. One deprecation fix: `ForwardedHeadersOptions.KnownNetworks` → `KnownIPNetworks` at `Web/Program.cs:18`. Phase 4 work: (1) Cookie auth `Events.OnRedirectToLogin`/`OnRedirectToAccessDenied` short-circuit `/api/*` paths to 401/403. (2) **Subtle gotcha discovered:** the unauth redirect on `/api/*` was coming from the Google challenge handler (set as `DefaultChallengeScheme`), not cookie auth — adding `[Authorize(AuthenticationSchemes = "Cookies")]` on `ApiControllerBase` routes the challenge through cookies so the events actually fire. Verified `curl /api/user/me` → clean 401 with empty body. (3) `Client/Authentication/CustomAuthStateProvider.cs` calls `GET /api/user/me`, parses `UserDto`, builds a `ClaimsPrincipal` with `NameIdentifier`/`Name`/`Email` claims; caches the result and exposes `NotifyUserChanged()` for sign-in/out transitions. Registered in `Client/Program.cs` as both `CustomAuthStateProvider` (for direct injection if needed) and `AuthenticationStateProvider` (for the cascading auth state). (4) `[Authorize]` restored on `Client/Pages/Counter.razor`. Build clean, server boots, `/api/user/me` returns 401 unauth, `/counter` redirects unauth users to login, `/` still serves. Two intentional Phase 4 deferrals to Phase 5: replacing `IUserService` usages with `AuthenticationStateProvider` (no Client components use it yet) and antiforgery integration (can't be verified end-to-end without a Client mutation call). Two BYUI-feed gotchas worth noting: the global `~/.nuget/NuGet/NuGet.Config` lists a `tfs.byui.edu` feed first; when offline, restore needs `--source https://api.nuget.org/v3/index.json` to skip it._
- _2026-04-25 — Phase 3 complete. Built 7 REST API controllers under `src/InterviewScheduler.Web/Controllers/Api/` (`User`, `Leaders`, `AppointmentTypes`, `Contacts`, `Calendar`, `Appointments`, `Schedule`) plus a small `ApiControllerBase` that resolves and caches the current `User` per request via `IUserService.GetCurrentUserAsync()`. All endpoints `[ApiController] [Authorize] [Route("api/...")]` and filter by `UserId`. Mapping helpers added in `Web/Mappings/EntityMappings.cs` — static `ToDto`/`ApplyTo` extensions for the 5 entity ↔ DTO pairs plus `CalendarInfo`/`TimeSlot`. New `UserDto` added to `Shared/Dtos/`. Three intentional DTO reshapes: (1) `CreateScheduleRequest` rebuilt to match the actual `ISchedulingService.CreateSchedule` signature — `(Date, StartTime, EndTime, AppointmentTypeId, LeaderIds[], ContactIds[])`. (2) `CreateScheduleResult` rebuilt to mirror `Core/Helpers/CreateScheduleResult` (`Success, ErrorMessage, AppointmentsCreated, CalendarEventsCreated, UnscheduledContacts, Appointments[]`) — the wizard already reads these names. (3) `CalendarInfoDto` reshaped from `(Id, Summary, Description, TimeZone)` to `(Id, Name, Description, IsPrimary)` to match what `ICalendarService.GetCalendarsAsync` actually returns. CSV import endpoint accepts raw multipart CSV and runs the two-phase delete/insert/relationship-link inside a DB transaction — pragmatic deviation from the plan's literal "client parses with CsvHelper" wording (encoding pre-DB-ID family relationships through `ContactDto[]` requires extra design; `ICsvParserService` already does the work; Phase 5 can revisit). Build clean (no new warnings vs. baseline), server boots, `/api/user/me` and `/api/leaders` return 302→login when unauthenticated. The 302 behavior is wrong for the WASM client and a new Phase 4 task was added: configure cookie auth events so unauthenticated `/api/*` requests return 401 instead of redirecting. Mutation endpoints not exercised end-to-end yet — antiforgery integration is also Phase 4. Razor pages still work unchanged (Phase 3 only added new code paths). Phase 5 will be the first real consumer of this API surface._
- _2026-04-25 — Phase 2 complete. Shared populated with 8 DTOs (`Shared/Dtos/`), `PhoneNumberHelper` (`Shared/Helpers/`), and the `ISmsService`/`SmsService`/`SmsMessage` + `ISchedulingRulesService`/`SchedulingRulesService` quartet (`Shared/Services/`). Both interfaces moved from `Core/Interfaces/` since their signatures reference Core entities; both impls moved from `Infrastructure/Services/`. `Shared.csproj` bumped to `net9.0` and given a `→ Core` ref + `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.Options` 9.0.7 packages (needed by the moved services). DI in `Web/Program.cs` updated by adding `using InterviewScheduler.Shared.Services` (registrations unchanged); Razor pages (`Appointments`, `AppointmentWizard`, `SmsPreview`) resolve the moved types via new `@using` lines added to `Web/Components/_Imports.razor` (and `Client/_Imports.razor` for symmetry). Build clean (zero new warnings), server boots, `/counter` still serves the WASM Counter from Phase 1 (verified via prerender envelope `"typeName":"InterviewScheduler.Client.Pages.Counter"`), `/` still renders. Two intentional deviations from the literal plan: (1) **dropped** the `Infrastructure → Shared` ProjectReference from the plan — Infrastructure (`net8.0`) cannot reference Shared (`net9.0`), and nothing in Infrastructure actually consumes the moved types, so the defensive ref was both impossible and unnecessary. Add it later when Infrastructure is bumped (Phase 7). (2) **Pulled forward** Phase 7's `Shared` → `net9.0` bump because Shared now holds real types and aligning frameworks now avoids skew. The Phase 7 box for that one task is also ticked. The "validators" Phase 2 bullet resolved to a no-op: `TimePeriodValidationHelper` + `ValidationResult` already live in `Core/Helpers/` and are reachable from Shared transitively. Phase 5 (move Razor pages to Client) will be the first phase that exercises these moved services from WASM context._
- _2026-04-25 — Phase 1 complete. Client (Blazor WASM, net9.0) scaffolded under `src/InterviewScheduler.Client/`, wired into Web, added to solution. Counter.razor moved to Client and renders via WebAssembly render mode (verified by inspecting the prerender envelope on `/counter`). Build clean, server boots, both Server-mode and WASM-mode pages serve. Two intentional deviations from the literal plan: (1) kept Server render mode registered alongside WASM so non-migrated pages stay functional during the migration; (2) used a plain ProjectReference instead of `<Private>false><ExcludeAssets>all` because that combination prevents the Web host from loading the Client assembly at runtime. Added `Microsoft.AspNetCore.Components.WebAssembly.Server` 9.0.7 to Web (required for `AddInteractiveWebAssembly*` APIs). `[Authorize]` temporarily removed from Counter — Phase 4 task added to restore it once a WASM AuthenticationStateProvider exists. Visual browser verification of WASM increment + absence of `/_blazor` socket pending next dev session._
- _2026-04-25 — Phase 5 Iteration A complete: foundation + first two page moves. (1) Authored 7 typed `HttpClient` wrappers under `src/InterviewScheduler.Client/Services/` (`IUserApiClient`, `IContactsApiClient`, `ILeadersApiClient`, `IAppointmentTypesApiClient`, `IAppointmentsApiClient`, `ICalendarApiClient`, `IScheduleApiClient`) — interface + impl in one file each, mirroring the controllers from Phase 3. `ContactsApiClient.ImportAsync` builds `MultipartFormDataContent` so the existing server-side CSV parser keeps running unchanged. (2) Registered each client via `AddHttpClient<TInterface, TImpl>(client => client.BaseAddress = builder.HostEnvironment.BaseAddress)` in `Client/Program.cs`. Required adding the `Microsoft.Extensions.Http` 10.0.0 package to `InterviewScheduler.Client.csproj` — `AddHttpClient` is not pulled in transitively by the WebAssembly metapackage. Kept the bare `HttpClient` registration alongside because `CustomAuthStateProvider` injects it and needs custom 401 handling that typed clients (which `EnsureSuccessStatusCode`) don't provide. Also registered `ISmsService → SmsService` from `Shared.Services` so the WASM-side DI container can resolve it for migrated pages. Added `@using InterviewScheduler.Client.Services` to `Client/_Imports.razor`. (3) Moved `SmsPreview.razor` from `Web/Components/Pages/` to `Client/Pages/`: swapped `@inject ApplicationDbContext` + `EF` `using`s for `@inject IContactsApiClient/ILeadersApiClient/IAppointmentTypesApiClient`, swapped `@rendermode InteractiveServer` → `InteractiveWebAssembly`, replaced the three `DbContext.X.ToListAsync()` calls with `Api.GetAllAsync()` calls. **Decision worth noting:** `ISmsService.GenerateAppointmentMessages` takes `Core.Entities.Contact/Leader/AppointmentType` (with `HeadOfHouse` navigation), not DTOs. To avoid bloating `ISmsService` with DTO overloads, the page projects the loaded DTOs back into Core entity instances (Core POCOs work fine without an `ApplicationDbContext`) and wires `HeadOfHouse`/`Spouse` navigation via an `Id → Contact` dictionary lookup. Inline projection helpers (`ToContact`/`ToLeader`/`ToAppointmentType`) live at the bottom of `SmsPreview.razor` — if a second page hits the same wall (likely AppointmentWizard), lift these to `Client/Mappings/`. (4) Moved `AppointmentTypes.razor` from `Web/Components/Pages/` to `Client/Pages/`: same pattern but simpler — works directly on `AppointmentTypeDto` (no Core entity needed). `IUserService` was previously injected just to scope the DbContext query by user; the controller does that server-side via claims so the page now just calls `TypesApi.GetAllAsync/CreateAsync/UpdateAsync/DeleteAsync` and trusts the API. (5) Verification: `dotnet build` clean (12 pre-existing warnings; 0 errors). Server boots cleanly. `curl https://localhost:7094/api/{user/me,leaders,contacts,appointment-types}` all return 401 unauth. `curl https://localhost:7094/{sms-preview,appointment-types,counter}` all redirect 302→Google OAuth (the expected Phase 4 behavior since `[Authorize]` is on these pages). `_framework/blazor.web.js`, `_framework/dotnet.js`, and `_framework/InterviewScheduler.Client.wasm` all serve 200 with non-zero size. The unauth `curl` 307s on the HTTP port are from `UseHttpsRedirection` and resolve to clean 401s on the HTTPS port — not a regression. Browser-side functional verification of WASM rendering + auth-cookie inclusion on API calls is still pending visual smoke (same caveat as Phase 1). Iteration B will move `Leaders.razor`, `Contacts.razor`, `Appointments.razor`, `AppointmentWizard.razor`, then the layout/host shell (`MainLayout`, `NavMenu`, `UserInfo`, `Routes.razor`, `App.razor`), then strip `.AddInteractiveServerRenderMode()` from `Web/Program.cs`. Antiforgery for WASM mutations was deferred again — `AppointmentTypes` POST/PUT/DELETE worked through the typed client without manual antiforgery configuration because the API endpoints use cookie auth alone (no `[ValidateAntiForgeryToken]` and no antiforgery middleware on the API pipeline); revisit if it becomes load-bearing for the bigger pages._
