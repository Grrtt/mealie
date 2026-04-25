# Implementation Plan: Mealie Backend Rewrite — C# .NET 10

**Branch**: `001-csharp-backend-migration` | **Date**: 2025-07-14 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/001-csharp-backend-migration/spec.md`

## Summary

Replace the Python/FastAPI Mealie backend with a fully API-compatible C# .NET 10 implementation using ASP.NET Core 10 MVC Controllers, EF Core 10 (SQLite + PostgreSQL), Mapperly, FluentValidation, Serilog, Swashbuckle, and MailKit. The frontend (Nuxt 4) is completely unchanged. A one-time migration utility carries all existing data — preserving every UUID, integer PK, and recipe slug verbatim — from the Python SQLAlchemy schema to the EF Core schema. Background jobs run as single-instance `IHostedService` workers. Webhooks are best-effort fire-and-forget. Observability is Serilog structured logging only.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10 MVC Controllers, EF Core 10 (Npgsql + SQLite), Mapperly, FluentValidation, Serilog, Swashbuckle.AspNetCore, MailKit, HtmlAgilityPack, AngleSharp  
**Storage**: SQLite (default, single-node) or PostgreSQL (configured via `DATABASE_URL` / `DB_ENGINE`)  
**Testing**: `dotnet test` — xUnit + `WebApplicationFactory<Program>` for integration tests; NSubstitute for mocking  
**Target Platform**: Linux server (Docker); same image naming convention as Python backend  
**Project Type**: Web service (REST API) + CLI migration tool  
**Performance Goals**: Common operations (recipe list/detail/search) ≤ 200ms p95 at 50 concurrent users; sustain 200 concurrent users on typical read/write without 5xx (SC-004, SC-005)  
**Constraints**: Full test suite ≤ 10 min CI (SC-006); developer onboarding ≤ 15 min (SC-008); migration of 10k+ recipes ≤ 30 min (SC-003); recipe scraper ≥ 80% site coverage vs. Python baseline (SC-010)  
**Scale/Scope**: Self-hosted deployments ranging from single-user SQLite to multi-household PostgreSQL; ~150 API endpoints; 16 core domain entities; 1 migration CLI tool

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`constitution.md`) contains placeholder template text and has not been ratified for this project. **No active constitution constraints apply.** The following domain-appropriate gates are applied from the spec itself:

| Gate | Status | Notes |
|------|--------|-------|
| API compatibility: zero path/schema regressions vs. Python | ✅ PASS | Enforced by automated OpenAPI diff in CI (SC-002) |
| Multi-tenant data isolation | ✅ PASS | EF Core global query filters + integration tests (FR-011, FR-013) |
| Zero data loss migration | ✅ PASS | ID-preserving migration utility with per-record error handling (FR-034–037) |
| Full test suite < 10 min | ✅ PASS | WebApplicationFactory in-memory integration tests, no external services |
| Structured logging (Serilog only) | ✅ PASS | No OpenTelemetry or Prometheus in scope (clarification #5) |
| Single-instance scheduler | ✅ PASS | `IHostedService` pattern, no distributed locking (clarification #2) |

**Post-Phase 1 re-check**: All gates remain PASS after data-model and contract design review. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-csharp-backend-migration/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions, rationale, resolved clarifications
├── data-model.md        # Phase 1 output — entity schema, relationships, migration order
├── quickstart.md        # Phase 1 output — developer onboarding guide
├── contracts/
│   └── api-contract.md  # Phase 1 output — full REST API contract (all ~150 endpoints)
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created here)
```

### Source Code (repository root)

```text
Mealie.sln

src/
├── Mealie.Api/                        # ASP.NET Core entry point
│   ├── Controllers/                   # MVC controllers grouped by domain
│   │   ├── Auth/                      #   AuthController.cs
│   │   ├── Users/                     #   UsersController.cs
│   │   ├── Groups/                    #   GroupsController.cs, LabelsController.cs, ...
│   │   ├── Households/                #   HouseholdsController.cs, ShoppingListsController.cs, ...
│   │   ├── Recipes/                   #   RecipesController.cs, CommentsController.cs, ...
│   │   ├── Organizers/                #   TagsController.cs, CategoriesController.cs, ...
│   │   ├── Foods/                     #   FoodsController.cs
│   │   ├── Units/                     #   UnitsController.cs
│   │   ├── Parser/                    #   ParserController.cs
│   │   ├── Explore/                   #   ExploreController.cs (public)
│   │   ├── Admin/                     #   AdminController.cs, BackupsController.cs, ...
│   │   └── Utility/                   #   HealthController.cs, AppInfoController.cs
│   ├── Middleware/                    # Error handling, tenant context, request logging
│   ├── Filters/                       # Action filters (auth, validation)
│   └── Program.cs                     # DI wiring, middleware pipeline

├── Mealie.Application/                # Use cases, no framework dependencies
│   ├── Services/                      # IRecipeService, IShoppingListService, etc.
│   ├── Dtos/                          # Request/Response DTOs per domain
│   ├── Mappers/                       # Mapperly compile-time mapper classes
│   └── Validators/                    # FluentValidation validators

├── Mealie.Domain/                     # Pure domain — entities, no EF Core
│   ├── Entities/                      # Recipe, User, Group, Household, ...
│   └── Events/                        # Domain event types (for webhook bus)

├── Mealie.Infrastructure/             # EF Core, external services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/            # IEntityTypeConfiguration per entity
│   │   └── Migrations/                # EF Core generated migrations
│   ├── Auth/                          # LDAP handler, OIDC config, API key handler
│   ├── Email/                         # MailKit IEmailService implementation
│   ├── Scheduler/                     # BackgroundService scheduled jobs
│   ├── Scraper/                       # Recipe scraper (JSON-LD, microdata, heuristic)
│   ├── Parser/                        # Ingredient parser (brute-force port)
│   └── Webhooks/                      # IWebhookDeliveryService (fire-and-forget)

└── Mealie.Shared/                     # Utilities, constants, extension methods

tests/
├── Mealie.UnitTests/                  # Fast unit tests — services, validators, parser
└── Mealie.IntegrationTests/           # API-level — WebApplicationFactory + in-memory SQLite

tools/
└── Mealie.Migration/                  # One-time Python → C# migration CLI
    ├── SourceReaders/                 # Dapper readers for SQLAlchemy tables
    ├── TargetWriters/                 # EF Core writers with identity insert
    └── MigrationRunner.cs             # Orchestration, report generation
```

**Structure Decision**: Multi-project C# solution (5 src + 2 test + 1 tools project) as specified in the spec assumptions. Clean architecture layering: `Domain` ← `Application` ← `Infrastructure` ← `Api`. The `Mealie.Migration` tool is a separate CLI project under `tools/` to keep it out of the main runtime.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*(No active constitution — no violations to track.)*
