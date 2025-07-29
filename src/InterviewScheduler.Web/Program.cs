using InterviewScheduler.Web.Components;
using InterviewScheduler.Infrastructure.Data;
using InterviewScheduler.Infrastructure.Services;
using InterviewScheduler.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MVC controllers for authentication endpoints
builder.Services.AddControllers();

// Add data protection services for OAuth state
builder.Services.AddDataProtection();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=interviewscheduler.db"));

// Add services
builder.Services.AddScoped<ICsvParserService, CsvParserService>();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/signin-google";
    options.LogoutPath = "/signout-google";
    options.SlidingExpiration = true;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["GoogleCalendar:ClientId"] ?? "";
    googleOptions.ClientSecret = builder.Configuration["GoogleCalendar:ClientSecret"] ?? "";
    googleOptions.SaveTokens = true; // Save tokens for API access
    
    // Request calendar scope for Google Calendar API access
    googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar");
    
    // Set explicit callback path to avoid conflict with signin route
    googleOptions.CallbackPath = "/signin-google-callback";
    
    // Add events for debugging
    googleOptions.Events.OnRemoteFailure = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError($"Google authentication failed: {context.Failure?.Message}");
        context.Response.Redirect("/?error=auth_failed");
        context.HandleResponse();
        return Task.CompletedTask;
    };
    
    googleOptions.Events.OnTicketReceived = async context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Google authentication successful");
        
        // Create or update user in database
        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        var googleUserId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        
        if (!string.IsNullOrEmpty(googleUserId) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(name))
        {
            await userService.GetOrCreateUserAsync(googleUserId, email, name);
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map controller endpoints
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created and migrations applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
