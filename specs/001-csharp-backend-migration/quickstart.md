# Quickstart: Mealie C# Backend — Developer Guide

**Feature**: `001-csharp-backend-migration`  
**Target**: Developer with .NET SDK installed, unfamiliar with the C# codebase.  
**Goal**: Build, run, and test the backend within 15 minutes (SC-008).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (optional, for PostgreSQL)
- Git

Verify:
```bash
dotnet --version   # should print 10.x.x
```

---

## 1. Clone & Build

```bash
git clone https://github.com/Grrtt/mealie.git
cd mealie
git checkout 001-csharp-backend-migration

# Build the solution (all projects)
dotnet build Mealie.sln
```

Expected output: `Build succeeded. 0 Error(s)`

---

## 2. Configure Environment

Copy the development environment template:
```bash
cp .env.example .env
```

Minimum `.env` for local development (SQLite, no external services):
```env
# Database — SQLite default for local dev
DB_ENGINE=sqlite
DATA_DIR=./dev/data

# Auth
SECRET=dev-secret-change-in-production-minimum-32-chars

# Logging
LOG_LEVEL=DEBUG

# Optional: disable features not needed locally
LDAP_AUTH_ENABLED=false
OIDC_AUTH_ENABLED=false
SMTP_HOST=
OPENAI_API_KEY=
```

---

## 3. Apply Database Migrations

```bash
dotnet ef database update \
  --project src/Mealie.Infrastructure \
  --startup-project src/Mealie.Api
```

This creates `./dev/data/mealie.db` (SQLite) with the full schema.

---

## 4. Seed Development Data

```bash
dotnet run --project src/Mealie.Api -- seed
```

Creates:
- Default group `"Home"`
- Default household `"Family"`
- Admin user: `admin@example.com` / `admin` (change password immediately)
- 20 sample recipes

---

## 5. Run the Backend

```bash
dotnet run --project src/Mealie.Api
```

Output:
```
[Information] Mealie v2.0.0 starting on http://localhost:9000
[Information] Database: SQLite @ ./dev/data/mealie.db
[Information] OpenAPI: http://localhost:9000/api/docs
```

Browse to:
- **API Docs**: http://localhost:9000/api/docs
- **Health check**: http://localhost:9000/healthz
- **OpenAPI JSON**: http://localhost:9000/api/openapi.json

---

## 6. Run the Test Suite

```bash
# All tests
dotnet test Mealie.sln

# Unit tests only
dotnet test src/Mealie.UnitTests

# Integration tests only (spins up an in-memory SQLite DB per test class)
dotnet test src/Mealie.IntegrationTests

# With coverage report
dotnet test Mealie.sln --collect:"XPlat Code Coverage"
```

Integration tests use `WebApplicationFactory<Program>` — no external services required.

---

## 7. Hot Reload (Development)

```bash
dotnet watch run --project src/Mealie.Api
```

Code changes to controllers, services, and validators reload without restarting.

---

## 8. PostgreSQL (Optional)

Start a local PostgreSQL container:
```bash
docker run -d \
  --name mealie-pg \
  -e POSTGRES_DB=mealie \
  -e POSTGRES_USER=mealie \
  -e POSTGRES_PASSWORD=mealie \
  -p 5432:5432 \
  postgres:16
```

Update `.env`:
```env
DB_ENGINE=postgres
DATABASE_URL=Host=localhost;Database=mealie;Username=mealie;Password=mealie
```

Then re-run migrations:
```bash
dotnet ef database update \
  --project src/Mealie.Infrastructure \
  --startup-project src/Mealie.Api
```

---

## 9. Project Structure

```
Mealie.sln
├── src/
│   ├── Mealie.Api/              # ASP.NET Core — controllers, middleware, startup
│   │   ├── Controllers/         # Route handlers grouped by domain
│   │   ├── Middleware/          # Auth, tenant context, error handling
│   │   └── Program.cs
│   ├── Mealie.Application/      # Use cases, service interfaces, DTOs
│   │   ├── Services/            # IRecipeService, IShoppingListService, etc.
│   │   ├── Mappers/             # Mapperly mapper classes
│   │   └── Validators/          # FluentValidation validators
│   ├── Mealie.Domain/           # Core domain models, no EF Core dependency
│   │   ├── Entities/            # Recipe, User, Group, etc.
│   │   └── Events/              # Domain events
│   ├── Mealie.Infrastructure/   # EF Core, DB context, external service adapters
│   │   ├── Data/                # ApplicationDbContext, entity configs
│   │   ├── Migrations/          # EF Core migrations
│   │   ├── Email/               # MailKit SMTP implementation
│   │   ├── Scheduler/           # IHostedService background jobs
│   │   └── Scraper/             # Recipe scraper implementation
│   └── Mealie.Shared/           # Shared utilities, constants, extension methods
├── tests/
│   ├── Mealie.UnitTests/        # Fast unit tests, no I/O
│   └── Mealie.IntegrationTests/ # API-level tests via WebApplicationFactory
└── tools/
    └── Mealie.Migration/        # One-time Python → C# data migration CLI
```

---

## 10. Adding a New Field to an Existing Endpoint

Example: Add `source_url` field to the recipe summary response.

1. **Domain entity** (`Mealie.Domain/Entities/Recipe.cs`):
   ```csharp
   public string? SourceUrl { get; set; }
   ```

2. **EF Core configuration** (`Mealie.Infrastructure/Data/Configurations/RecipeConfiguration.cs`):
   ```csharp
   builder.Property(r => r.SourceUrl).HasColumnName("org_url");
   ```
   *(column already exists as `org_url`, just expose it in DTO)*

3. **DTO** (`Mealie.Application/Services/Recipes/Dtos/RecipeSummaryResponse.cs`):
   ```csharp
   public string? SourceUrl { get; set; }
   ```

4. **Mapper** (`Mealie.Application/Mappers/RecipeMapper.cs`):
   The Mapperly partial mapper picks up the new property automatically if names match.  
   For remapped names, add:
   ```csharp
   [MapProperty(nameof(Recipe.SourceUrl), nameof(RecipeSummaryResponse.SourceUrl))]
   public partial RecipeSummaryResponse ToSummary(Recipe recipe);
   ```

5. **Run tests** — the OpenAPI spec is auto-generated, so the new field appears automatically:
   ```bash
   dotnet test src/Mealie.IntegrationTests
   ```

---

## 11. Running the Data Migration Tool

After completing the C# backend setup, migrate an existing Python database:

```bash
dotnet run --project tools/Mealie.Migration -- \
  --source "Data Source=/path/to/source/mealie.db" \
  --target "Data Source=./dev/data/mealie.db" \
  --source-engine sqlite \
  --target-engine sqlite
```

For PostgreSQL source:
```bash
dotnet run --project tools/Mealie.Migration -- \
  --source "Host=old-host;Database=mealie;Username=mealie;Password=secret" \
  --target "Host=localhost;Database=mealie;Username=mealie;Password=mealie" \
  --source-engine postgres \
  --target-engine postgres
```

The tool prints a migration report to stdout and exits with code 0 on success, 1 on failure.

---

## 12. Common Issues

| Problem | Solution |
|---------|----------|
| `dotnet: command not found` | Install .NET 10 SDK from Microsoft |
| EF Core migration fails | Ensure `DATA_DIR` exists and is writable |
| Port 9000 in use | Set `API_PORT=9001` in `.env` |
| JWT `401` on all requests | Ensure `SECRET` in `.env` is ≥ 32 characters |
| Integration tests fail with DB errors | Delete `tests/.temp/` and re-run |
