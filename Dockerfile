# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Copy solution file and restore dependencies
COPY InterviewScheduler.sln ./
COPY src/InterviewScheduler.Core/InterviewScheduler.Core.csproj ./src/InterviewScheduler.Core/
COPY src/InterviewScheduler.Infrastructure/InterviewScheduler.Infrastructure.csproj ./src/InterviewScheduler.Infrastructure/
COPY src/InterviewScheduler.Shared/InterviewScheduler.Shared.csproj ./src/InterviewScheduler.Shared/
COPY src/InterviewScheduler.Web/InterviewScheduler.Web.csproj ./src/InterviewScheduler.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish src/InterviewScheduler.Web/InterviewScheduler.Web.csproj -c Release -o /app/publish --no-restore

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set working directory
WORKDIR /app

# Create app user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=build /app/publish .

# Create directory for SQLite database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "InterviewScheduler.Web.dll"]