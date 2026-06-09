# MudBlazor Migration + Dark Mode (wasm branch)

## Context

**Is the client using MudBlazor?** No. The current `wasm` branch uses **Bootstrap 5** with a hand-rolled flexbox layout. (A separate `mudblazor` git branch exists, but it targets a different *server-side* architecture — components in `InterviewScheduler.Web/Components`, `InteractiveServer` render mode, MudBlazor 8.10.0 — so it is a reference, not a drop-in.)

This app is a **.NET 10 Blazor WebAssembly** solution:
- `InterviewScheduler.Web` — ASP.NET Core host (static `App.razor`, serves assets, OAuth).
- `InterviewScheduler.Client` — the WASM project where **all components render** (`InteractiveWebAssemblyRenderMode`, no prerender).
- `InterviewScheduler.Shared`, `.Core`, `.Infrastructure`.

**Goal:** Adopt MudBlazor across the whole UI with **always-on dark mode**, convert the layout to MudBlazor, and replace the start/end time `<input type="time">` with **MudTimePicker**.

**Decisions (confirmed with user):** MudBlazor **9.5.0** (latest, supports net10.0) · **Full migration** of all pages · **Always-on dark** (`IsDarkMode="true"`, no toggle).

> Per the user's preference for trackable plans in-repo, copy this file into the repo (e.g. `docs/mudblazor-migration.md`) as the first execution step so progress is committed alongside the code.

---

## Part A — Infrastructure / Shell

### 1. Package references
Add `MudBlazor` **9.5.0** to **both** csproj files (Client for the WASM payload + DI; Web to guarantee `_content/MudBlazor/*` static assets are published/served, version-pinned identically):
- `src/InterviewScheduler.Client/InterviewScheduler.Client.csproj`
- `src/InterviewScheduler.Web/InterviewScheduler.Web.csproj`

```xml
<PackageReference Include="MudBlazor" Version="9.5.0" />
```

### 2. `src/InterviewScheduler.Client/Program.cs`
```csharp
using MudBlazor.Services;
// ...after WebAssemblyHostBuilder.CreateDefault(args):
builder.Services.AddMudServices();
```
(WASM container only — the Web host renders no Mud components.)

### 3. `src/InterviewScheduler.Client/_Imports.razor`
```razor
@using MudBlazor
```

### 4. `src/InterviewScheduler.Web/Components/App.razor`
Add to `<head>` (keep Bootstrap link for now — remove in Part C):
```razor
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
```
Add before `</body>` (after `blazor.web.js`):
```razor
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```
Leave `WasmNoPrerender` render-mode wiring untouched.

### 5. `src/InterviewScheduler.Client/Routes.razor` — providers + dark theme
The four MudBlazor providers must render inside the **interactive WASM** tree. `Routes.razor` is the topmost WASM-rendered component, so it is the correct single home (`MudPopoverProvider` is required by `MudTimePicker`/menus/dialogs). Put the dark theme on the provider here so dialogs/snackbars are themed too.

```razor
@using InterviewScheduler.Client.Layout

<MudThemeProvider Theme="@_theme" IsDarkMode="true" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<Router AppAssembly="typeof(Program).Assembly">
    ... existing Router / AuthorizeRouteView / NotAuthorized / FocusOnNavigate ...
</Router>

@code {
    private readonly MudTheme _theme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#90caf9", Secondary = "#b0bec5",
            Success = "#4caf50", Info = "#2196f3", Warning = "#ff9800", Error = "#f44336",
            AppbarBackground = "#1e1e1e", Background = "#121212", Surface = "#1e1e1e",
        },
        LayoutProperties = new LayoutProperties { DrawerWidthLeft = "260px", DrawerWidthRight = "300px" }
    };
}
```

### 6. `src/InterviewScheduler.Client/Layout/MainLayout.razor` — Mud shell
Convert to `MudLayout` → `MudAppBar` (menu toggle, title, About link, `<UserInfo />`) → `MudDrawer` (`@bind-Open`, `Variant="DrawerVariant.Responsive"`, `Breakpoint="Breakpoint.Md"`) containing `<NavMenu />` → `MudMainContent` → `MudContainer MaxWidth="MaxWidth.ExtraLarge"` wrapping `@Body`. Keep the `#blazor-error-ui` div. No theme here (it lives in Routes.razor). Add `_drawerOpen` bool + `DrawerToggle()`.

### 7. `src/InterviewScheduler.Client/Layout/NavMenu.razor`
Convert to `MudNavMenu` / `MudNavLink` with Material icons, preserving routes and the `AuthorizeView`:
- `""` (Home), and inside `<Authorized>`: `contacts`, `leaders`, `appointment-types`, `appointments`, `sms-preview`.
Use `@`-prefixed icons, e.g. `Icon="@Icons.Material.Filled.People"`. Drop all `bi bi-*` spans.

---

## Part B — Page migration

All pages live in `src/InterviewScheduler.Client/Pages/` (+ `Layout/UserInfo.razor`, `Routes.razor` NotAuthorized card). **Reuse the `mudblazor`-branch markup** as a structural template via `git show mudblazor:src/InterviewScheduler.Web/Components/Pages/<File>.razor`, but:

**Two hard rules when porting from the reference branch:**
1. **Keep the wasm branch's data layer.** The reference injects backend services (`DbContext`, `ICalendarService`, …) directly; the wasm branch uses typed HTTP API clients (`IContactsApiClient`, `ILeadersApiClient`, `IAppointmentTypesApiClient`, `IAppointmentsApiClient`, `ICalendarApiClient`, `IScheduleApiClient`, `IUserApiClient`). **Port only Razor markup + `@using`; preserve every existing `@inject`, `@code` data-fetch body, and DTO→graph projection.**
2. **Fix MudBlazor 9.x API drift** (the reference predates 9.x). Confirm against 9.5.0 docs (use the context7 connector, library `mudblazor/mudblazor`) and apply at minimum:
   - `MudCheckBox`/`MudSwitch`: `@bind-Checked` → **`@bind-Value`**
   - inline `MudDialog`: `@bind-IsVisible` → **`@bind-Visible`**
   - `MudList`/`MudListItem`: **remove `Clickable`** (keep `T="..."` and `@onclick`)
   - `MudChip` requires `T="string"`

Suggested order (low→high risk):
1. **UserInfo.razor** — drop-in from reference (`MudButton`/`MudText` + `AuthorizeView`).
2. **Home.razor** — drop-in (no injected data).
3. **Routes.razor NotAuthorized card** — hand migration (no reference): `MudContainer`/`MudCard` + `MudButton Href="/signin-google"`.
4. **Contacts.razor** — port markup; keep `ContactsApi` calls, `LoadFiles` 10MB import, `isImporting` state (→ `MudProgressCircular`). Checkbox rename.
5. **AppointmentTypes.razor** — port markup; keep `TypesApi` + template classes. Remove `Clickable`.
6. **Leaders.razor** — port markup; keep `LeadersApi`, calendar auth flow (`forceLoad:true`). Binds to DTOs (property-compatible). Checkbox rename.
7. **SmsPreview.razor** — port markup; keep `ISmsService` + clients. Decide datetime control (simplest faithful port: keep single field via `MudTextField InputType="InputType.DateTimeLocal"`, or split into `MudDatePicker`+`MudTimePicker` recombined in `GeneratePreview`).
8. **Appointments.razor** — port list/filter markup; **keep the richer wasm details modal body** (clickable phones, both-parent display, Copy buttons) and only swap the outer modal to `MudDialog @bind-Visible`. Preserve all six clients + 502/JS-confirm handling. *(Highest non-wizard effort — reference modal is a feature regression; do not blindly port it.)*
9. **AppointmentWizard.razor** — see Part B-time below. **Ignore the reference `@code` entirely** (incompatible). Only Step 1 markup + the stepper port from reference; Steps 2–4 are largely net-new MudBlazor authoring preserving all wasm validation/working-hours/alternatives state.
10. **Delete** `Counter.razor` (and `Weather.razor` if present) — unused template leftovers.

### Part B-time — MudTimePicker (the explicit request)
File: `src/InterviewScheduler.Client/Pages/AppointmentWizard.razor`, Step 3 (~lines 292–303). Backing fields `startTime`/`endTime` are `TimeOnly` (~lines 579–580) and are read in 10+ places, several requiring `TimeOnly` (notably `CreateScheduleRequest.StartTime/EndTime` at ~974–975, and `.ToString("h:mm tt")` display).

**Lowest-risk approach: keep the `TimeOnly` fields; add `TimeSpan?` adapter properties.** `MudTimePicker.Time` is `TimeSpan?`. This touches only the two inputs + new adapters — all existing reads stay unchanged.

Replace the two `<input type="time">` with:
```razor
<MudGrid>
    <MudItem xs="6">
        <MudTimePicker Label="Start Time" Time="StartTimeSpan"
                       TimeChanged="OnStartTimeChanged" AmPm="true" Error="@HasTimeError" />
    </MudItem>
    <MudItem xs="6">
        <MudTimePicker Label="End Time" Time="EndTimeSpan"
                       TimeChanged="OnEndTimeChanged" AmPm="true" Error="@HasTimeError" />
    </MudItem>
</MudGrid>
```
Add to `@code` (keep the existing `TimeOnly` fields):
```csharp
private TimeSpan? StartTimeSpan => startTime.ToTimeSpan();
private TimeSpan? EndTimeSpan   => endTime.ToTimeSpan();

private async Task OnStartTimeChanged(TimeSpan? value)
{
    startTime = value.HasValue ? TimeOnly.FromTimeSpan(value.Value) : default;
    await OnTimeRangeChanged();   // existing validator
}
private async Task OnEndTimeChanged(TimeSpan? value)
{
    endTime = value.HasValue ? TimeOnly.FromTimeSpan(value.Value) : default;
    await OnTimeRangeChanged();
}
```
- Using `Time`+`TimeChanged` (not `@bind-Time`) replicates the old `@bind:after="OnTimeRangeChanged"` exactly: update field, then validate.
- Replace `GetTimeInputClass()` with a `HasTimeError` helper (errors → `Error`; warnings already render separately as alerts). Delete `GetTimeInputClass()` + `.is-warning` CSS once inputs are gone.
- Apply the same adapter pattern to the Step 3 `<input type="date">` (`appointmentDate` is `DateTime`) → `MudDatePicker Date="..." DateChanged="..."` calling `OnDateChanged()`.

---

## Part C — Cleanup (after all pages migrated)
1. Remove the Bootstrap `<link>` from `App.razor`.
2. Remove unused Bootstrap utility classes from `MainLayout`/`NavMenu` and prune `MainLayout.razor.css` / `NavMenu.razor.css`.
3. Remove Bootstrap assets in `wwwroot/lib/bootstrap` and Bootstrap-specific rules in `app.css`; confirm no `bi bi-*` icon references remain.

---

## Verification
1. **Build:** `dotnet build` the solution — fixes any 9.x attribute-name compile errors (`@bind-Checked`, `@bind-IsVisible`, `Clickable`).
2. **Run:** `dotnet run --project src/InterviewScheduler.Web` and load the app.
3. **Shell/dark mode:** confirm dark MudBlazor AppBar + responsive drawer; toggle drawer on mobile width.
4. **Time picker (key check):** sign in → Appointments → New → Step 3. Open Start/End `MudTimePicker` (proves `MudPopoverProvider` is live in WASM), pick times, confirm validation/alternatives still fire and the Step 4 summary shows correct `h:mm tt`. Create an appointment and confirm `CreateScheduleRequest` still receives correct `TimeOnly` values.
5. **Per page:** smoke-test Contacts import, Leaders calendar auth, AppointmentTypes CRUD, Appointments details dialog + SMS, SmsPreview generation.
6. Use the Claude Preview / browser MCP tools for a visual pass on dark-mode contrast across pages.
