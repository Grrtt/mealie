# Mealie C# Backend

A reimplementation of the Mealie recipe manager backend in C# / ASP.NET Core 10. This provides full API compatibility with the existing Python/FastAPI backend while enabling better performance and type safety.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see `global.json`)
- [PostgreSQL 15+](https://www.postgresql.org/) (or SQLite for local dev)
- Optional: Docker + Docker Compose

Verify your setup:
```bash
dotnet --version  # should be 10.0.x
```

## Quick Start (15 minutes)

### 1. Clone and Build

```bash
git clone https://github.com/Grrtt/mealie.git
cd mealie/backend-dotnet
dotnet build Mealie.slnx
```

### 2. Configure Environment

```bash
cp .env.example .env
# Edit .env with your database connection string
```

Key settings in `.env`:
```
DB_ENGINE=sqlite                          # or postgres
DATABASE_URL=Data Source=./data/mealie.db  # for sqlite
SECRET=change-me-to-a-random-32-char-string
```

### 3. Initialize Database

```bash
# Apply EF Core migrations
dotnet ef database update --project src/Mealie.Infrastructure --startup-project src/Mealie.Api

# Or use dotnet run shorthand
dotnet run --project src/Mealie.Api -- migrate
```

### 4. Seed Sample Data

```bash
dotnet run --project src/Mealie.Api -- seed
```

This creates:
- Group: `Home`
- Household: `Family`
- Admin user: `admin@example.com` / `admin`
- 5 sample recipes

### 5. Run the Backend

```bash
dotnet run --project src/Mealie.Api
# API available at http://localhost:9000
# Swagger UI: http://localhost:9000/swagger
```

### 6. Run Tests

```bash
dotnet test Mealie.slnx
```

## Hot Reload (Development)

```bash
cd src/Mealie.Api
dotnet watch run
```

## PostgreSQL Configuration

```bash
# Update .env
DATABASE_URL=Host=localhost;Database=mealie;Username=mealie;Password=mealie
DB_ENGINE=postgres

# Re-run migrations
dotnet ef database update --project src/Mealie.Infrastructure --startup-project src/Mealie.Api
```

## Project Structure

```
backend-dotnet/
├── src/
│   ├── Mealie.Api/           # ASP.NET Core web host — controllers, middleware, DI wiring
│   ├── Mealie.Application/   # Services, DTOs, validators (business logic)
│   ├── Mealie.Domain/        # Entities, value objects (no dependencies)
│   ├── Mealie.Infrastructure/ # EF Core, JWT, LDAP, email, scraper
│   └── Mealie.Shared/        # Pagination helpers shared across layers
├── tests/
│   ├── Mealie.UnitTests/     # Fast unit tests (no DB, no HTTP)
│   └── Mealie.IntegrationTests/ # End-to-end tests via WebApplicationFactory
├── tools/
│   └── Mealie.Migration/     # CLI tool for migrating Python DB → C# DB
├── scripts/
│   └── validate-openapi-compat.sh  # OpenAPI diff validation script
├── docker/
│   └── Dockerfile            # Multi-stage build
├── .env.example              # All supported environment variables
├── Directory.Build.props     # Shared MSBuild properties
├── Directory.Packages.props  # Central Package Management
├── global.json               # SDK version pin
└── Mealie.slnx               # Solution file (.NET 10 format)
```

## TypeScript API Client Regeneration

After any schema change, regenerate the frontend TypeScript client:

```bash
# Requires running backend on port 9000
cd ../frontend
npx openapi-typescript http://localhost:9000/swagger/v1/swagger.json -o src/lib/api/types.ts
```

This is a **required step** in the PR checklist for any API-breaking change.

## Common Issues

| Problem | Solution |
|---------|----------|
| `NETSDK1226` build error | `Directory.Build.props` already sets `AllowMissingPrunePackageData=true` |
| `dotnet-ef` not found | Run: `export PATH="$PATH:$HOME/.dotnet/tools"` then `dotnet tool restore` |
| DB migration fails | Ensure `DATABASE_URL` env var is set and DB server is running |
| Port 9000 in use | Set `ASPNETCORE_URLS=http://localhost:9001` |
| Cannot connect to LDAP | Set `LDAP_AUTH_ENABLED=false` to disable |

## Environment Variables

See `.env.example` for the full list with descriptions.

## Architecture Notes

- **Tenant isolation**: All queries are filtered by `GroupId`/`HouseholdId` via EF Core global query filters
- **Authentication**: JWT (48h access tokens) + API key authentication (BCrypt-hashed)
- **Pagination**: All list endpoints use cursor-based `PaginatedResponse<T>` matching Python shape
- **OpenAPI**: Swashbuckle at `/swagger` — schema IDs match Python Pydantic model names
