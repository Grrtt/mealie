# Research: Mealie C# Backend Migration

**Phase**: Phase 0 — Outline & Research  
**Feature**: `001-csharp-backend-migration`  
**Generated**: 2025-07-14

---

## 1. ASP.NET Core 10 MVC Controller Patterns

**Decision**: Use attribute-routed MVC Controllers (not Minimal APIs) grouped by domain area.

**Rationale**: Spec calls this out explicitly. Controllers integrate cleanly with Swashbuckle for OpenAPI generation, support action filters for cross-cutting concerns (auth, tenant scoping, validation), and provide a discoverable structure that mirrors the existing Python router layout (`routes/recipe`, `routes/admin`, etc.). Minimal APIs would require manual OpenAPI annotations and produce more boilerplate for the volume of endpoints Mealie exposes (~150+).

**Alternatives Considered**:
- Minimal APIs with `IEndpointRouteBuilder` groups — rejected because Swashbuckle support is weaker, and the controller pattern produces more self-documenting code aligned to team expectations.

**Pattern**:
```
Mealie.Api/Controllers/
  Recipes/RecipesController.cs
  Organizers/TagsController.cs
  ...
```
Each controller inherits `MealieControllerBase` (sets tenant context, common error handling via problem details).

---

## 2. EF Core 10 with Dual Provider (SQLite + PostgreSQL)

**Decision**: Single `ApplicationDbContext`, provider selected at startup via `DATABASE_URL` or `DB_ENGINE` env var. Use `IDesignTimeDbContextFactory` for migrations.

**Rationale**: EF Core 10 supports both Npgsql and `Microsoft.EntityFrameworkCore.Sqlite` providers with the same context. Schema differences (UUID types, JSON columns) are handled with `UseNpgsql()` / `UseSqlite()` builder methods. Migrations are generated against PostgreSQL as the canonical provider; SQLite compatibility is verified in CI.

**Key Considerations**:
- SQLite does not support `ALTER COLUMN`, so generated migrations must use `PRAGMA writable_schema` workaround or recreate tables. Use `ReplaceService<IMigrationsModelDiffer, FixedMigrationsModelDiffer>()` pattern to suppress unsupported operations.
- UUID primary keys: Use `Guid` in C# (`HasColumnType("TEXT")` for SQLite, `uuid` for PostgreSQL). **All UUIDs from the Python schema must be preserved verbatim** (clarification #1).
- Integer PKs: Some association tables use integer surrogate keys — preserve exactly.
- Recipe slugs: unique constraint `(slug, group_id)` — must be preserved and enforced identically.

**Alternatives Considered**:
- Two separate `DbContext` implementations — rejected as it doubles migration maintenance burden.
- DAPPER + hand-written SQL — rejected because EF Core's change tracking is required for optimistic concurrency and the multi-tenant query filter pattern.

---

## 3. Multi-Tenant Query Filtering Pattern

**Decision**: Implement household-scoped global query filters on all tenant-owned entities via EF Core's `HasQueryFilter`. A `IHouseholdContext` service (injected per-request via `IHttpContextAccessor`) supplies the current `HouseholdId`.

**Rationale**: Global query filters are the idiomatic EF Core pattern for row-level security. They eliminate the risk of accidentally omitting a `WHERE household_id = ?` clause. Filters can be disabled with `IgnoreQueryFilters()` for admin and cross-household operations.

**Pattern**:
```csharp
// In ApplicationDbContext.OnModelCreating:
builder.Entity<Recipe>()
    .HasQueryFilter(r => r.HouseholdId == _householdContext.HouseholdId);
```

**Edge Cases**:
- Public/explore endpoints bypass tenant filter (`IgnoreQueryFilters()` + explicit `IsPublic` predicate).
- Admin endpoints bypass completely.
- Cross-household recipe sharing (Group shared cookbooks) bypasses household filter but applies group filter.

**Alternatives Considered**:
- Repository pattern with tenant parameters — rejected because it requires every repository method to accept and propagate a tenant ID, creating noise and a surface for errors.

---

## 4. OpenAPI Compatibility Strategy

**Decision**: Use Swashbuckle.AspNetCore with XML doc comments and custom `IOperationFilter` implementations to emit the exact same JSON shapes as the Python Pydantic models.

**Rationale**: The Python backend generates an OpenAPI spec that the Nuxt 4 frontend's TypeScript client is generated from. The C# spec must be structurally identical (same path names, same field names in `camelCase`, same nullable semantics, same error schemas).

**Key Alignment Steps**:
1. All JSON property names serialized with `camelCase` (`JsonNamingPolicy.CamelCase`) — matching FastAPI/Pydantic default.
2. HTTP 422 validation errors must return a body matching Pydantic's `ValidationError` shape:
   ```json
   { "detail": [{ "loc": ["body", "field"], "msg": "...", "type": "..." }] }
   ```
   Implement a `FluentValidationExceptionMiddleware` that transforms `ValidationException` to this shape.
3. Pagination envelope: `{ "page": 1, "per_page": 50, "total": 100, "items": [...] }` — implement `PaginatedResponse<T>`.
4. OpenAPI spec served at `/api/openapi.json` — same path as Python backend.
5. Swashbuckle configuration must set `SchemaId` to produce the same component names as Pydantic models.

**Alternatives Considered**:
- NSwag — rejected because team chose Swashbuckle (already decided, per spec).

---

## 5. Authentication Architecture

**Decision**: ASP.NET Core's built-in `AddAuthentication` with multiple schemes:
- **JWT Bearer** (primary): `Microsoft.AspNetCore.Authentication.JwtBearer` — new tokens, new signing key.
- **API Key**: Custom `AuthenticationHandler<ApiKeyAuthenticationOptions>` reading `Authorization: Bearer` header and matching against `long_live_tokens` table.
- **LDAP**: Custom handler using `Novell.Directory.Ldap.NETStandard` (or `System.DirectoryServices.Protocols`) — enabled only when `LDAP_AUTH_ENABLED=true`.
- **OIDC**: `Microsoft.AspNetCore.Authentication.OpenIdConnect` — enabled only when `OIDC_AUTH_ENABLED=true`.

**JWT Token Incompatibility** (clarification #1 / spec assumption): Existing Python-issued JWTs are NOT valid in the new backend. Users must re-authenticate. No migration of tokens. New tokens issued with a new secret key from `SECRET` env var.

**API Key Scheme**: Must be the same scheme name as Python. Keys stored bcrypt-hashed (matching Python behavior, clarification #4). Scheme resolves household context from the `group_id` / `household_id` stored on the API key record.

**LDAP Timeout**: LDAP server unreachable → connection timeout (configurable via `LDAP_QUERY_TIMEOUT`, default 5s) → return HTTP 401 with `{"detail": "LDAP server unavailable"}`.

**Alternatives Considered**:
- ASP.NET Core Identity for user management — rejected because it imposes its own schema that conflicts with the existing Mealie user table structure.

---

## 6. Background Scheduler (IHostedService)

**Decision**: Use `BackgroundService` base class with a single-instance timer loop. No distributed locking — single-instance deployment matches APScheduler behavior (clarification #2).

**Jobs to Implement**:
| Job | Schedule |
|-----|----------|
| Scheduled Backup | Configurable cron (default: nightly) |
| Meal Plan Notifications | Daily at configured time |
| Webhook Cleanup | Hourly |
| Shopping List Cleanup | Configurable |

**Pattern**:
```csharp
public class SchedulerHostedService : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await RunDueJobsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

**Job State**: No state migrated from APScheduler. Jobs re-read schedules from config/DB at startup. Next-run is computed from "now" on startup. (Per spec assumption: `"No job state is migrated."`)

**Alternatives Considered**:
- Hangfire — rejected (adds SQL schema dependency, distributed lock complexity not needed for single-instance).
- Quartz.NET — rejected (same reasons, overkill for single-instance).

---

## 7. Webhook Delivery

**Decision**: Best-effort fire-and-forget using `HttpClient` within the event handler. No retry, no dead-letter queue (clarification #3).

**Pattern**:
```csharp
// EventBus publishes to IWebhookDeliveryService
// Delivery is async void — caller does not await; failures are logged via Serilog.
_ = _webhookService.DeliverAsync(webhook, payload, CancellationToken.None);
```

**Payload Shape**: Must match the Python event payload structure (JSON-serialized event data). Use `System.Text.Json` with `camelCase` naming.

**Failure Logging**: Serilog `Warning` log with `{WebhookId}`, `{Url}`, `{StatusCode}`, `{Exception}` properties.

**Alternatives Considered**:
- Retry with exponential backoff — explicitly out of scope (clarification #3).
- Message queue (RabbitMQ, Redis Streams) — out of scope.

---

## 8. Recipe Scraper Strategy

**Decision**: Two-stage scraper:
1. **JSON-LD First**: Parse `<script type="application/ld+json">` for `schema.org/Recipe` objects (covers ~60-70% of modern recipe sites).
2. **Microdata Fallback**: Parse `itemprop` attributes for schema.org/Recipe properties.
3. **Heuristic Fallback**: Common CSS selector patterns for title, ingredients, instructions (covers a long tail of sites without structured data).

**Library Choice**: HtmlAgilityPack for DOM parsing (already decided per spec). AngleSharp as secondary for sites requiring CSS selector query.

**JavaScript-Rendering Sites**: Not supported. Return a structured `ScraperError` response with `{"detail": "This site requires JavaScript rendering and is not supported."}` (HTTP 200 with populated `scraping_not_supported=true` flag, matching Python behavior). NOT a 500.

**Site Coverage Target**: SC-010 requires ≥80% of the Python test corpus. JSON-LD parsing alone should achieve ~65%; microdata + heuristics should close the gap to 80%+.

**Alternatives Considered**:
- Headless browser (Playwright) — rejected as heavyweight dependency for self-hosted Docker image.
- Porting `recipe-scrapers` Python library site-by-site — accepted as incremental follow-up work, not MVP requirement.

---

## 9. Ingredient Parser

**Decision**: Port the Python brute-force ingredient parser logic to C#. The parser tokenizes ingredient strings using regex patterns for quantities (fractions, decimals, Unicode fractions like ½), units (matched against the canonical units table), and food (remainder after quantity/unit extraction).

**Approach**:
- Regex-based tokenizer with the same patterns as `mealie/services/parser_services/brute/`.
- OpenAI integration: when `OPENAI_API_KEY` is set, optionally invoke GPT-4o with a structured output schema to parse ambiguous ingredient strings (FR-032).

**Locale Support**: Match Python behavior — parser uses the locale of the request (from `Accept-Language` or explicit parameter). Non-English parsing falls through to English patterns if locale-specific patterns are absent.

**Alternatives Considered**:
- NLP library (ML.NET) — rejected as overkill; the Python implementation is regex-based and results must be equivalent.

---

## 10. Data Migration Utility

**Decision**: Standalone CLI tool (`Mealie.Migration`) that reads the source Python SQLAlchemy schema and writes to the EF Core schema, entity-by-entity, preserving all IDs verbatim (clarification #1).

**Architecture**:
- Source: ADO.NET / Dapper raw SQL queries against source DB (avoids needing SQLAlchemy models in C#).
- Target: EF Core `ApplicationDbContext` with identity insert enabled.
- Idempotency check: Query `__mealie_migration_log` table (created on first run); if record exists, exit with code 1 and message.
- Error handling: Per-record try/catch; malformed records logged with full detail and skipped; migration continues. Final report printed to stdout (FR-037).

**ID Preservation**:
- UUIDs: Read as `string` from source, write as `Guid` to target — same bytes.
- Integer PKs: Read as `long`, write directly.
- Recipe slugs: Copy verbatim; unique constraint violations on slug+group_id are treated as "already exists, skip."
- Media file references: Copy file paths verbatim; no file movement needed (files already on disk, per spec assumption).

**Migration Report Format** (stdout):
```
Entity             Migrated   Skipped   Errors
------             --------   -------   ------
Groups             12         0         0
Households         18         0         0
Users              234        1         0
...
```

**Alternatives Considered**:
- EF Core migration scripts — rejected because the source schema is SQLAlchemy-managed and diverges from EF Core schema.
- Liquibase / Flyway — rejected as Java dependencies out of place here.

---

## 11. Observability

**Decision**: Serilog with structured logging only. No OpenTelemetry, no Prometheus metrics (clarification #5).

**Sinks**: Console (JSON format in production, human-readable in development). File sink optional via env var.

**Log Levels**: Configurable via `LOG_LEVEL` env var (same as Python backend). Mapping: `DEBUG → Debug`, `INFO → Information`, `WARNING → Warning`, `ERROR → Error`.

**Request Logging**: Serilog's `UseSerilogRequestLogging()` middleware with enrichment: `{Method}`, `{Path}`, `{StatusCode}`, `{Elapsed}`, `{UserId}`, `{HouseholdId}`.

**Structured Properties** on all log statements: `{GroupId}`, `{HouseholdId}`, `{UserId}`, `{RequestId}` (from correlation ID middleware).

---

## 12. Docker Image & Configuration

**Decision**: Single Docker image using `mcr.microsoft.com/dotnet/aspnet:10.0` runtime base. Configuration via environment variables with the same names as the Python backend (FR-040).

**Key Environment Variables Preserved**:
| Variable | Purpose |
|----------|---------|
| `DATABASE_URL` / `DB_ENGINE` | Database connection |
| `SECRET` | JWT signing key |
| `BASE_URL` | Public-facing URL |
| `DATA_DIR` | Media file storage path |
| `LOG_LEVEL` | Logging verbosity |
| `LDAP_*` | LDAP configuration |
| `OIDC_*` | OIDC configuration |
| `SMTP_*` | Email configuration |
| `OPENAI_API_KEY` | AI integration |
| `ALLOW_SIGNUP` | Registration policy |

**Static File Serving** (FR-005): `UseStaticFiles()` serving from `DATA_DIR/frontend`; `MapFallbackToFile("index.html")` for SPA routing.

---

## 13. Mapperly Object Mapping

**Decision**: Use Mapperly (`Riok.Mapperly`) for all DTO ↔ Domain ↔ EF Entity mapping. Compile-time generated mappers (no reflection at runtime).

**Pattern per domain**:
```csharp
[Mapper]
public partial class RecipeMapper {
    public partial RecipeResponse ToResponse(Recipe entity);
    public partial Recipe ToDomain(CreateRecipeRequest request);
}
```

**Null Handling**: Mapperly `MapperIgnoreObsoleteMembers` and `UseReferenceHandling` for circular reference graphs (Recipe → Ingredient → Food → Recipe).

---

## 14. FluentValidation Integration

**Decision**: Register `AddFluentValidation()` in DI; validators auto-discovered by assembly scanning. `ValidationException` → HTTP 422 via middleware (to match Pydantic error shape — see §4).

**Validation Pattern per DTO**:
```csharp
public class CreateRecipeValidator : AbstractValidator<CreateRecipeRequest> {
    public CreateRecipeValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        ...
    }
}
```

---

## 15. Email (MailKit / SMTP)

**Decision**: Use MailKit (`MailKit.Net.Smtp`) for SMTP delivery. `IEmailService` abstraction wraps MailKit; enabled only when `SMTP_HOST` is configured.

**Templates**: Razor-based email templates (same content as Python Jinja2 templates translated to Razor). Rendered via `RazorLight` or `IEmailTemplateRenderer`.

**Alternatives Considered**:
- `System.Net.Mail.SmtpClient` — rejected; deprecated for async use. MailKit is the community standard.

---

## Resolved Clarifications

| # | Question | Answer |
|---|----------|--------|
| 1 | Identity preservation | All entity IDs (UUIDs, int PKs, recipe slugs) preserved verbatim in migration |
| 2 | Scheduler | Single-instance only; no distributed locking needed |
| 3 | Webhook delivery | Best-effort fire-and-forget; no retry on failure |
| 4 | Security hardening | Match existing Python behavior only; no new hardening in scope |
| 5 | Observability | Serilog structured logs only; no OpenTelemetry or Prometheus |
