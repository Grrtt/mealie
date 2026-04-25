# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY Mealie.slnx Directory.Build.props Directory.Packages.props global.json ./
COPY src/Mealie.Api/Mealie.Api.csproj src/Mealie.Api/
COPY src/Mealie.Application/Mealie.Application.csproj src/Mealie.Application/
COPY src/Mealie.Domain/Mealie.Domain.csproj src/Mealie.Domain/
COPY src/Mealie.Infrastructure/Mealie.Infrastructure.csproj src/Mealie.Infrastructure/
COPY src/Mealie.Shared/Mealie.Shared.csproj src/Mealie.Shared/

# Restore dependencies
RUN dotnet restore Mealie.slnx -p:AllowMissingPrunePackageData=true

# Copy source
COPY src/ src/

# Build and publish
RUN dotnet publish src/Mealie.Api/Mealie.Api.csproj \
    -c Release \
    -o /app/publish \
    -p:AllowMissingPrunePackageData=true \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create data directory
RUN mkdir -p /app/data

# Copy published output
COPY --from=build /app/publish .

# Expose port 9000 (same as Python backend)
EXPOSE 9000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:9000/healthz || exit 1

# Environment defaults
ENV ASPNETCORE_URLS=http://+:9000
ENV DATA_DIR=/app/data
ENV DB_ENGINE=sqlite
ENV DATABASE_URL="Data Source=/app/data/mealie.db"

ENTRYPOINT ["dotnet", "Mealie.Api.dll"]
