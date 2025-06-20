# Multi-stage Dockerfile for NLWebNet Demo Application
# Optimized for .NET 9 with security hardening and minimal attack surface

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Configure NuGet to trust certificates and use HTTPS
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_NOLOGO=1

# Copy project files for efficient layer caching
COPY ["demo/NLWebNet.Demo.csproj", "demo/"]
COPY ["src/NLWebNet/NLWebNet.csproj", "src/NLWebNet/"]
COPY ["NLWebNet.sln", "./"]

# Restore dependencies with better error handling
RUN dotnet restore "demo/NLWebNet.Demo.csproj" --verbosity minimal

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/demo"
RUN dotnet build "NLWebNet.Demo.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "NLWebNet.Demo.csproj" -c Release -o /app/publish --no-restore --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r nlwebnet && useradd -r -g nlwebnet nlwebnet

# Copy published application
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R nlwebnet:nlwebnet /app

# Switch to non-root user
USER nlwebnet

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "NLWebNet.Demo.dll"]