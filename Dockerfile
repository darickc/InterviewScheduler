# Use the official .NET SDK image for building.
# Pinned to the 10.0.1xx band: the 10.0.3xx SDK band has a regression that drops
# the host-side blazor.web.js framework asset from publish, causing a runtime 404
# on /_framework/blazor.web.js. See dotnet/aspnetcore#65353 and #63962.
FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build

# Set working directory
WORKDIR /app

# Copy solution file and restore dependencies
COPY InterviewScheduler.sln ./
COPY src/InterviewScheduler.Core/InterviewScheduler.Core.csproj ./src/InterviewScheduler.Core/
COPY src/InterviewScheduler.Infrastructure/InterviewScheduler.Infrastructure.csproj ./src/InterviewScheduler.Infrastructure/
COPY src/InterviewScheduler.Shared/InterviewScheduler.Shared.csproj ./src/InterviewScheduler.Shared/
COPY src/InterviewScheduler.Web/InterviewScheduler.Web.csproj ./src/InterviewScheduler.Web/
COPY src/InterviewScheduler.Client/InterviewScheduler.Client.csproj ./src/InterviewScheduler.Client/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish src/InterviewScheduler.Web/InterviewScheduler.Web.csproj -c Release -o /app/publish --no-restore

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create data directory and give the built-in non-root 'app' user
# ownership of /app so SQLite can create interviewscheduler.db at runtime
RUN mkdir -p /app/data && chown -R $APP_UID /app

# Run as the pre-created non-root user (UID via $APP_UID, .NET 8+ images)
USER $APP_UID

# # Set environment variables
# ENV ASPNETCORE_URLS=http://+:8080
# ENV ASPNETCORE_ENVIRONMENT=Production

# # Expose port
# EXPOSE 8080

# # Health check
# HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
#     CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "InterviewScheduler.Web.dll"]