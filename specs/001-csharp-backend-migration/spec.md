# Feature Specification: Mealie Backend Rewrite — C# .NET 10

**Feature Branch**: `001-csharp-backend-migration`  
**Created**: 2025-07-14  
**Status**: Draft  
**Input**: Migrate the Mealie backend from Python/FastAPI to C# .NET 10 with full API compatibility and feature parity.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — End Users Experience No Disruption (Priority: P1)

A home-cook user logs into Mealie, browses their household recipe collection, creates a new recipe by scraping a URL, manages shopping lists, and schedules meals on the meal planner — all exactly as they did before the migration. Nothing about the product has changed from their perspective: the URLs work, saved data is intact, all features behave identically.

**Why this priority**: The entire value proposition of this migration is that it is invisible to end users. Any regression or data loss makes the migration a failure regardless of technical quality.

**Independent Test**: A tester can point an unmodified Mealie frontend at the new backend and complete an end-to-end user session — login → browse recipes → scrape URL → add to meal plan → generate shopping list — without errors or missing data.

**Acceptance Scenarios**:

1. **Given** a user's existing account and data in the migrated database, **When** they log in with their existing credentials, **Then** their full recipe collection, household, group membership, and preferences are accessible and correct.
2. **Given** a logged-in user, **When** they submit a recipe URL to the scraper endpoint, **Then** the system returns a parsed recipe with title, ingredients, steps, and image — matching the expected response shape used by the frontend.
3. **Given** a user in a multi-household group, **When** they create or modify a recipe, **Then** that change is only visible to members of their household and is invisible to members of other households.
4. **Given** a user with previously saved API keys or session tokens, **When** they make authenticated requests to the new backend, **Then** existing JWT tokens are rejected gracefully and users are prompted to re-authenticate (fresh tokens are issued by the new backend).
5. **Given** any existing Mealie page in the frontend SPA, **When** it makes any API call, **Then** it receives a response that conforms exactly to the same JSON shape, status codes, and HTTP headers as the Python backend produced.

---

### User Story 2 — Administrators Migrate Existing Data Without Loss (Priority: P1)

A self-hosted administrator has an existing Mealie SQLite or PostgreSQL database. They stop the Python container, run a provided migration utility, start the new C# container, and all their data — users, groups, households, recipes, organizers, media files, settings — is intact and accessible.

**Why this priority**: Self-hosted data portability is a core promise of the Mealie project. Any migration that risks data loss or requires manual intervention per-record is unacceptable.

**Independent Test**: A tester can take a known-good Mealie Python database dump (SQLite and PostgreSQL), run the migration tool, start the new backend, and verify record counts and spot-check data integrity across all major entities via the API.

**Acceptance Scenarios**:

1. **Given** a valid Mealie SQLite database from the Python backend, **When** the migration tool is executed, **Then** all records are present in the new schema with no data loss, and the new backend starts and passes health checks.
2. **Given** a valid Mealie PostgreSQL database from the Python backend, **When** the migration tool is executed, **Then** the same outcome as SQLite: full data integrity, new backend operational.
3. **Given** a database that contains media files (recipe images, user avatars), **When** migration completes, **Then** all media file references resolve correctly and images are served by the new backend.
4. **Given** a migration that encounters a malformed or corrupt record, **When** the tool processes it, **Then** it logs a clear, actionable error describing the problematic record and continues processing remaining records rather than aborting entirely.
5. **Given** an administrator who needs to roll back, **When** they restore the original database and restart the Python backend, **Then** the original backend functions normally (migration is non-destructive to the source database).

---

### User Story 3 — API Compatibility Is Verifiable Against the OpenAPI Specification (Priority: P1)

A developer runs an automated compatibility check that compares the OpenAPI spec produced by the new C# backend against the spec produced by the Python backend. All endpoints, request shapes, response schemas, and status code contracts are present and equivalent.

**Why this priority**: The Nuxt 4 frontend generates its TypeScript API client directly from the OpenAPI spec. Any divergence silently breaks the frontend without a compilation error. This must be continuously verifiable.

**Independent Test**: Running a diff of both OpenAPI JSON documents (Python vs. C#) shows zero structural differences in paths, methods, schema definitions, required fields, and response codes.

**Acceptance Scenarios**:

1. **Given** the new backend is running, **When** the OpenAPI specification endpoint is queried, **Then** every API path present in the Python backend's spec is present in the C# spec with the same HTTP methods.
2. **Given** any request/response schema in the Python OpenAPI spec, **When** the corresponding C# schema is compared, **Then** all required fields, data types, and optional fields match — no field is missing, renamed, or has a changed type.
3. **Given** the TypeScript codegen tool is run against the C# OpenAPI spec, **When** the generated client is used to compile the frontend, **Then** there are zero TypeScript compilation errors.
4. **Given** an API endpoint that previously returned HTTP 422 for validation errors, **When** the C# backend receives the same invalid input, **Then** it returns HTTP 422 with a body that matches the expected Pydantic-style error schema shape.

---

### User Story 4 — Authentication Works Across All Supported Methods (Priority: P1)

Users who authenticate via username/password, API keys, LDAP, or OIDC continue to access Mealie without any change to their login flow. Administrators who have configured LDAP or OIDC providers do not need to reconfigure anything.

**Why this priority**: Authentication is a hard blocker — no user can do anything if login fails. Multi-org deployments commonly use LDAP or OIDC, and any regression here prevents entire user populations from accessing the system.

**Independent Test**: For each auth method (local, API key, LDAP, OIDC), a tester can authenticate successfully and receive a valid session that allows access to protected endpoints.

**Acceptance Scenarios**:

1. **Given** a user with a local Mealie account, **When** they POST credentials to the token endpoint, **Then** they receive a JWT access token and refresh token with the same field structure as before.
2. **Given** a user with a valid API key, **When** they include it in the `Authorization: Bearer` header, **Then** they can access protected resources scoped to their user and household.
3. **Given** an LDAP-configured instance, **When** an LDAP user authenticates with their directory credentials, **Then** they receive a valid Mealie session and their account is provisioned if it does not yet exist.
4. **Given** an OIDC-configured instance, **When** a user completes the OIDC authorization flow, **Then** they are redirected back to the frontend with a valid session established.
5. **Given** an expired or tampered JWT, **When** it is submitted to a protected endpoint, **Then** the backend returns HTTP 401 with a clear error message.

---

### User Story 5 — Background Services Continue to Operate (Priority: P2)

Scheduled tasks — nightly backups, webhook deliveries, the meal planner scheduler, and other time-triggered operations — continue to fire on their configured schedules without any user action required.

**Why this priority**: Background services are critical to data integrity (backups) and user-configured automations (webhooks, reminders). Silent failure would erode trust without obvious symptoms.

**Independent Test**: A tester can configure a webhook, advance the system clock or wait for the schedule trigger, and observe the webhook fired and was logged correctly.

**Acceptance Scenarios**:

1. **Given** a configured backup schedule, **When** the scheduled time elapses, **Then** a backup archive is created in the configured location and logged.
2. **Given** a configured webhook, **When** a recipe is created or updated, **Then** the webhook payload is delivered to the target URL within a reasonable time window.
3. **Given** a meal plan with notification settings, **When** the notification time arrives, **Then** any configured notification action (email, webhook) fires correctly.
4. **Given** the background scheduler is running, **When** the backend restarts, **Then** scheduled jobs resume from their next scheduled time without duplication or skipping.

---

### User Story 6 — Developers Can Extend and Maintain the Backend (Priority: P2)

A developer contributing to Mealie can clone the C# backend repository, build it, run all tests, and make a change to an API endpoint or domain service by following clear conventions — without needing to understand the Python codebase.

**Why this priority**: Long-term maintainability is one of the primary stated motivations for the migration. If the new codebase is difficult to contribute to, the migration fails its own goal.

**Independent Test**: A developer following only the README can get a local development environment running, execute the full test suite, and make a passing addition of a new field to an existing API endpoint.

**Acceptance Scenarios**:

1. **Given** a developer with the .NET SDK installed, **When** they follow the project README, **Then** the backend builds, seeds test data, and is accessible at a local URL within 15 minutes.
2. **Given** a full test suite run, **When** no code changes have been made, **Then** all unit and integration tests pass and coverage is reported.
3. **Given** a developer who adds a new optional field to an existing recipe response, **When** they follow the established mapping/DTO pattern, **Then** the change is reflected in the OpenAPI spec without manual edits to the spec document.
4. **Given** an integration test, **When** it exercises a multi-tenant operation (e.g., creating a recipe in Household A), **Then** the test infrastructure automatically ensures Household B cannot see the record.

---

### User Story 7 — Recipe Import from External Sources Works (Priority: P2)

A user who previously imported recipes from Chowdown, Paprika, Nextcloud Cookbook, or Tandoor via the Python backend can perform the same imports using the new backend. The same zip/JSON file they used before produces the same set of recipes.

**Why this priority**: Recipe import from other apps is a major acquisition feature — it is how users move from a competing product to Mealie. Regression here breaks onboarding for new users.

**Independent Test**: A tester can use each supported import format's sample file and verify that recipes are created with title, ingredients, and instructions populated correctly.

**Acceptance Scenarios**:

1. **Given** a valid Chowdown export zip, **When** it is submitted to the import endpoint, **Then** recipes are created in the user's household with title, ingredients, and instructions parsed correctly.
2. **Given** a valid Paprika export file, **When** it is submitted, **Then** recipes are created with all supported fields populated.
3. **Given** a valid Nextcloud Cookbook export, **When** it is submitted, **Then** recipes are imported correctly.
4. **Given** a valid Tandoor export, **When** it is submitted, **Then** recipes are imported correctly.
5. **Given** a malformed or unsupported file, **When** it is submitted to an import endpoint, **Then** the backend returns a clear error identifying the problem rather than a 500.

---

### User Story 8 — Ingredient Parsing Works at Equivalent Quality (Priority: P2)

A user types a freeform ingredient string like "2 cups all-purpose flour, sifted" and the system parses it into structured quantity, unit, and food components — at quality equivalent to the Python backend.

**Why this priority**: Ingredient parsing is used throughout the recipe creation and editing flows. Quality regression would impact the core recipe management experience.

**Independent Test**: A tester can submit a representative sample of 50 ingredient strings and verify that the new parser matches or exceeds the Python parser's accuracy on the same inputs.

**Acceptance Scenarios**:

1. **Given** a simple ingredient string (e.g., "2 cups flour"), **When** submitted to the parser endpoint, **Then** the response contains quantity=2, unit="cups", food="flour".
2. **Given** a complex ingredient string with notes (e.g., "1½ lbs chicken breast, boneless and skinless"), **When** submitted, **Then** quantity, unit, and food are extracted correctly and notes are preserved.
3. **Given** an ingredient string in a non-English language (if supported by the existing parser), **When** submitted with the appropriate locale, **Then** parsing behavior is equivalent to the Python backend.

---

### Edge Cases

- What happens when a user's LDAP server is unreachable at login time? The system must return a clear error and not hang indefinitely.
- What happens when the recipe scraper encounters a site that requires JavaScript rendering? The system must return a partial result or a clear "scraping not supported for this site" error rather than a 500.
- What happens when the database runs out of disk space during a backup? The backup operation must fail cleanly, log the error, and not corrupt the existing backup.
- What happens when a webhook delivery fails (target URL unreachable)? The system must retry with backoff and log delivery failures.
- What happens when two concurrent requests modify the same recipe? The system must apply appropriate optimistic concurrency or last-write-wins semantics consistent with the Python backend's behavior.
- What happens when a migration is run on an already-migrated database? The tool must detect this and refuse to re-migrate rather than duplicating data.
- What happens when a user attempts to access a recipe belonging to a different household? The system must return HTTP 404 (not 403) to avoid leaking the existence of resources across household boundaries.
- What happens when the OpenAI API key is not configured but a user triggers an AI-assisted feature? The system must return a clear configuration error, not a 500.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Core API & Compatibility

- **FR-001**: The system MUST expose every HTTP endpoint, request schema, and response schema that the Python backend currently exposes, such that the frontend can operate against the new backend without any frontend code changes.
- **FR-002**: The system MUST produce an OpenAPI 3.x specification document at a well-known endpoint that is structurally equivalent to the Python backend's OpenAPI spec for all user-facing paths.
- **FR-003**: The system MUST return the same HTTP status codes and error body shapes as the Python backend for all validation errors (HTTP 422), authentication failures (HTTP 401/403), and not-found responses (HTTP 404).
- **FR-004**: The system MUST support the same query parameters, pagination conventions, and filtering capabilities as the Python backend for all list endpoints.
- **FR-005**: The system MUST serve the Nuxt 4 frontend SPA at the root path, with all non-API routes falling through to the SPA's `index.html` (same behavior as the Python backend's static file serving).

#### Authentication & Authorization

- **FR-006**: The system MUST authenticate users via username/password and issue JWT access tokens and refresh tokens.
- **FR-007**: The system MUST authenticate users via long-lived API keys issued through the user profile management interface.
- **FR-008**: The system MUST support optional LDAP authentication, enabled via administrator configuration, with automatic user provisioning for first-time LDAP logins.
- **FR-009**: The system MUST support optional OIDC authentication, enabled via administrator configuration, completing the standard authorization code flow.
- **FR-010**: The system MUST enforce role-based access at the group, household, and admin levels — users can only read and modify resources they are authorized to access.
- **FR-011**: The system MUST ensure that every data access operation is scoped to the requesting user's household, such that no query can return resources belonging to a different household without explicit cross-household sharing being configured.

#### Multi-Tenancy

- **FR-012**: The system MUST enforce the three-tier tenancy hierarchy: Group → Household → User, with all recipe and organizer data owned at the Household level.
- **FR-013**: The system MUST prevent any data leakage between households, including via shared identifiers — a recipe ID from Household A must not be accessible by a member of Household B.
- **FR-014**: The system MUST support the existing group-level shared cookbook and recipe exploration features, where recipes can be shared across households within the same group.

#### Domains & Feature Coverage

- **FR-015**: The system MUST implement all Recipe management operations: create, read, update, delete, duplicate, scrape-from-URL, bulk import/export, image management, and timeline/history.
- **FR-016**: The system MUST implement all Organizer operations: tags, categories, tools, and cookbooks — with full CRUD and assignment to recipes.
- **FR-017**: The system MUST implement Meal Planner operations: creating, updating, and deleting meal plan entries; retrieving plans by date range.
- **FR-018**: The system MUST implement Shopping List operations: creating lists, adding/removing items, linking recipe ingredients to list items.
- **FR-019**: The system MUST implement Units and Foods management: CRUD for unit and food records, merging duplicates, and linking to ingredients.
- **FR-020**: The system MUST implement the Ingredient Parser endpoint, accepting freeform ingredient strings and returning structured quantity, unit, and food data.
- **FR-021**: The system MUST implement the Recipe Scraper endpoint, accepting a URL and returning a parsed recipe object.
- **FR-022**: The system MUST implement User management: profile CRUD, password change, API key management, and household assignment.
- **FR-023**: The system MUST implement Group and Household management: creation, configuration, member management, and invitation flows.
- **FR-024**: The system MUST implement Admin operations: user management, system settings, application configuration, and instance statistics.
- **FR-025**: The system MUST implement the Explore (public) endpoints, which surface recipes and cookbooks to unauthenticated users when the instance is configured for public access.
- **FR-026**: The system MUST implement Email (SMTP) notifications — triggered by invitations, password resets, and other system events.
- **FR-027**: The system MUST implement the Event Bus and Webhook system: publishing events when resources change and delivering payloads to configured webhook URLs.
- **FR-028**: The system MUST implement Backup and Restore: creating full-instance backup archives on schedule or on demand, and restoring from those archives.
- **FR-029**: The system MUST implement a Data Seeder for development and fresh-install scenarios, creating sample recipes, users, and groups.
- **FR-030**: The system MUST implement recipe imports from the following external formats: Mealie's own backup format, Chowdown, Paprika, Nextcloud Cookbook, and Tandoor.
- **FR-031**: The system MUST implement the Recipe Scraper with support for the same breadth of sites as the Python backend, using a strategy that covers structured data (schema.org/Recipe JSON-LD) and common site-specific patterns.
- **FR-032**: The system MUST implement an optional OpenAI integration endpoint that, when configured with an API key, accepts a natural-language ingredient description and returns structured data.
- **FR-033**: The system MUST implement a Background Scheduler that executes periodic tasks (backup, notifications, housekeeping) on configurable schedules without requiring external orchestration.

#### Data Migration

- **FR-034**: The system MUST provide a one-time migration utility that converts an existing Mealie Python SQLite or PostgreSQL database into the new C# schema with zero data loss for all supported entities.
- **FR-035**: The migration utility MUST be idempotent-safe: if run against an already-migrated database, it MUST detect this condition and exit without modifying data.
- **FR-036**: The migration utility MUST preserve all media files (recipe images, user avatars) and update file references in the new schema correctly.
- **FR-037**: The migration utility MUST produce a migration report listing counts of migrated records per entity type and any records that were skipped or required fallback handling.

#### Operational

- **FR-038**: The system MUST expose a health check endpoint that returns the instance version, database connectivity status, and readiness state.
- **FR-039**: The system MUST emit structured logs with configurable log levels, including request/response logging for API calls.
- **FR-040**: The system MUST support configuration via environment variables using the same variable names and semantics as the Python backend where possible, to allow existing Docker Compose files to work without changes.
- **FR-041**: The system MUST support both SQLite (for single-node self-hosted deployments) and PostgreSQL (for higher-scale or containerized deployments) as database backends, selectable via configuration.

### Key Entities

- **Group**: Top-level tenancy boundary. Contains one or more Households. Has shared settings (public access, registration policy, webhooks).
- **Household**: Second-level tenancy. Owns recipes, organizers, meal plans, and shopping lists. Multiple Households can coexist within a Group.
- **User**: Belongs to a Group and a Household. Has a role (admin, standard). Owns API keys and personal preferences.
- **Recipe**: Core content entity. Belongs to a Household. Has ingredients, instructions, notes, tags, categories, tools, nutritional info, and media. Tracks version history.
- **Ingredient**: A line item within a Recipe. Links to a Food entity and a Unit entity; has a freeform quantity and optional note.
- **Food**: A canonical food item (e.g., "all-purpose flour"). Shared within a Group. Has optional nutritional data.
- **Unit**: A canonical measurement unit (e.g., "cup"). Shared within a Group.
- **Tag / Category / Tool**: Organizer entities that classify Recipes. Scoped to a Group.
- **Cookbook**: A curated collection of Recipes. Belongs to a Household. Can be made public within the Group.
- **MealPlan**: An assignment of a Recipe to a date and meal slot (breakfast, lunch, dinner, side). Belongs to a Household.
- **ShoppingList**: A list of items to be purchased. Belongs to a Household. Items may be linked to Recipe Ingredients.
- **ApiKey**: A long-lived credential tied to a User. Used for automation and integration access.
- **Webhook**: A registered URL that receives event payloads when configured resource events occur. Scoped to a Household.
- **Backup**: A timestamped archive of all instance data. Created by the scheduler or on demand. Stored in a configured directory.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every existing Mealie frontend page and workflow completes without errors when the frontend is pointed at the new backend — verified by end-to-end test suite with zero failures.
- **SC-002**: The OpenAPI specification produced by the new backend differs from the Python backend's spec in zero paths, zero required request fields, and zero response field types — verified by automated schema diff.
- **SC-003**: A full migration of a representative production database (10,000+ recipes, multiple groups and households) completes in under 30 minutes with zero data-loss errors.
- **SC-004**: Common API operations (recipe list, recipe detail, search) respond in under 200ms at the 95th percentile under a load of 50 concurrent users on commodity hardware — representing a measurable improvement over the Python baseline.
- **SC-005**: The new backend sustains 200 concurrent users performing typical read/write operations without returning 5xx errors, compared to the Python backend's observed limits.
- **SC-006**: The full automated test suite (unit + integration) executes in under 10 minutes in a standard CI environment, enabling rapid feedback loops for contributors.
- **SC-007**: All four authentication methods (local, API key, LDAP, OIDC) pass their full acceptance scenario suite with no failures.
- **SC-008**: A developer unfamiliar with the C# codebase can build, run, and execute tests locally within 15 minutes by following the README, verified by a timed first-run test with a new contributor.
- **SC-009**: Zero regressions are reported in community self-hosted deployments during the 90-day post-release stabilization period, measured by GitHub issue volume compared to the pre-migration baseline.
- **SC-010**: The recipe scraper returns a valid, populated recipe object for at least 80% of URLs that the Python backend successfully scraped in the same test corpus.

---

## Assumptions

- **Scope**: This migration replaces the Python/FastAPI backend entirely. No Python code runs in production after the migration is complete. The strangler fig pattern is explicitly out of scope.
- **Frontend**: The Nuxt 4 frontend is completely unchanged. All changes are backend-only. The frontend's TypeScript API client is regenerated from the new C# OpenAPI spec and must compile without errors.
- **Tech stack**: The implementation uses ASP.NET Core 10 MVC Controllers, EF Core 10 with Npgsql and SQLite providers, Mapperly for object mapping, FluentValidation, Serilog for logging, Swashbuckle for OpenAPI, MailKit for SMTP, and HtmlAgilityPack/AngleSharp for HTML scraping. These are team decisions already made and are not up for re-evaluation in this specification.
- **Project structure**: The codebase is organized into `Mealie.Api`, `Mealie.Application`, `Mealie.Domain`, `Mealie.Infrastructure`, and `Mealie.Shared` projects, with `Mealie.UnitTests` and `Mealie.IntegrationTests` test projects. This structure is already decided.
- **Recipe scraper strategy**: The Python `recipe-scrapers` library (1,000+ site-specific scrapers) has no direct C# equivalent. The strategy is to implement schema.org/Recipe JSON-LD parsing (covering the majority of modern recipe sites) plus fallback heuristics, and to accept a reduced but substantial site coverage initially. Site-specific scrapers can be added incrementally. This is a known risk that has been accepted.
- **Database compatibility**: The new EF Core schema may differ from the SQLAlchemy schema. A one-time migration utility bridges the gap. There is no ongoing Alembic-style migration history to maintain from the Python side.
- **JWT tokens**: Existing JWT tokens issued by the Python backend are not valid in the new backend (different signing keys and possibly different claims). Users will need to log in again after migration. This is acceptable and expected.
- **LDAP/OIDC configuration**: Existing environment variables for LDAP and OIDC configuration will be honored by the new backend. No reconfiguration is needed by administrators.
- **Media files**: Recipe images, user avatars, and other media are stored on disk (not in the database) and are unchanged by the migration. File paths are preserved.
- **Background jobs**: APScheduler (Python) is replaced by `IHostedService` implementations in C#. Job schedules are re-read from configuration at startup; no job state is migrated.
- **Analytics**: The internal analytics/telemetry subsystem is included in scope as a low-priority item. It will behave identically to the Python implementation if configured.
- **OpenAI integration**: The optional OpenAI integration is in scope and uses the same environment variable configuration (`OPENAI_API_KEY`) as the Python backend.
- **Docker**: The new backend is distributed as a Docker image using the same image naming convention. The existing `docker-compose.yml` examples in the documentation require no changes beyond pointing to the new image tag.
- **Migration is one-way**: The migration utility does not provide a path to migrate back from C# to Python. Rollback is achieved by restoring the original database from backup and running the Python image.
- **CI/CD**: The existing Azure Pipelines configuration will be updated to build and test the C# solution. This is infrastructure work accompanying the migration, not a separate feature.
