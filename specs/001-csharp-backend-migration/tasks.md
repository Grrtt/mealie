# Tasks: Mealie Backend Rewrite — C# .NET 10

**Feature branch**: `001-csharp-backend-migration` — never merge to `main` during task execution  
**Input**: Design documents from `specs/001-csharp-backend-migration/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-contract.md ✅, quickstart.md ✅

**Path constraint**: All C# code lives under `backend-dotnet/` at the repo root. The existing Python code in `mealie/` is **never touched or moved**.

## Format: `[ID] [P?] [Story?] Description — file path`

- **[P]**: Can run in parallel (different files, no incomplete-task dependencies)
- **[Story]**: Which user story this task belongs to ([US1]–[US8])
- Exact file paths are included in every description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the C# solution structure under `backend-dotnet/` on branch `001-csharp-backend-migration`.

- [ ] T001 Create `backend-dotnet/` directory at repo root and scaffold `Mealie.sln` solution file linking all projects
- [ ] T002 Create `Mealie.Api` ASP.NET Core web project in `backend-dotnet/src/Mealie.Api/` and add to `Mealie.sln`
- [ ] T003 [P] Create `Mealie.Application` class library in `backend-dotnet/src/Mealie.Application/` and add to `Mealie.sln`
- [ ] T004 [P] Create `Mealie.Domain` class library in `backend-dotnet/src/Mealie.Domain/` and add to `Mealie.sln`
- [ ] T005 [P] Create `Mealie.Infrastructure` class library in `backend-dotnet/src/Mealie.Infrastructure/` and add to `Mealie.sln`
- [ ] T006 [P] Create `Mealie.Shared` class library in `backend-dotnet/src/Mealie.Shared/` and add to `Mealie.sln`
- [ ] T007 [P] Create `Mealie.UnitTests` xUnit project in `backend-dotnet/tests/Mealie.UnitTests/` and add to `Mealie.sln`
- [ ] T008 [P] Create `Mealie.IntegrationTests` xUnit project in `backend-dotnet/tests/Mealie.IntegrationTests/` and add to `Mealie.sln`
- [ ] T009 [P] Create `Mealie.Migration` CLI project (`dotnet new console`) in `backend-dotnet/tools/Mealie.Migration/` and add to `Mealie.sln`
- [ ] T010 Add `Directory.Packages.props` and `Directory.Build.props` to `backend-dotnet/` declaring all NuGet packages with pinned versions (ASP.NET Core 10, EF Core 10 + Npgsql + SQLite, Mapperly, FluentValidation, Serilog, Swashbuckle.AspNetCore, MailKit, HtmlAgilityPack, AngleSharp, Dapper, NSubstitute, xUnit, WebApplicationFactory)
- [ ] T011 [P] Add `backend-dotnet/global.json` (pinning `dotnet-version` to `10.x`, `rollForward: latestFeature`), `.editorconfig` (C# coding style), and `.gitignore` additions for `bin/`, `obj/`, `*.user`
- [ ] T012 [P] Add a `C#-Build` stage to `azure-pipelines.yml` that runs `dotnet build backend-dotnet/Mealie.sln` and `dotnet test backend-dotnet/Mealie.sln` on branch `001-csharp-backend-migration` (no deployment step, no merge to main)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, EF Core, multi-tenant infrastructure, auth basics, middleware, and DI wiring. **No user-story work can begin until this phase is complete.**

### Domain Entities

- [ ] T013 Create core tenancy domain entities `Group`, `Household`, `User`, and `ApiKey` (matching data-model.md column specs exactly) in `backend-dotnet/src/Mealie.Domain/Entities/Core/`
- [ ] T014 [P] Create recipe domain entity cluster: `Recipe`, `RecipeIngredient`, `RecipeInstruction`, `RecipeNote`, `RecipeAsset`, `RecipeComment`, `RecipeTimelineEvent`, `RecipeShareToken`, `Nutrition`, `RecipeSettings` in `backend-dotnet/src/Mealie.Domain/Entities/Recipes/`
- [ ] T015 [P] Create organizer domain entities: `Tag`, `Category`, `Tool`, `Cookbook`, `MultiPurposeLabel`, `GroupInviteToken` in `backend-dotnet/src/Mealie.Domain/Entities/Organizers/`
- [ ] T016 [P] Create food and unit domain entities: `IngredientFood`, `IngredientFoodAlias`, `IngredientUnit` in `backend-dotnet/src/Mealie.Domain/Entities/Ingredients/`
- [ ] T017 [P] Create planning and shopping domain entities: `MealPlan`, `ShoppingList`, `ShoppingListItem`, `ShoppingListItemRecipeReference`, `ShoppingListRecipeReference` in `backend-dotnet/src/Mealie.Domain/Entities/Planning/`
- [ ] T018 [P] Create preferences and notification domain entities: `GroupPreferences`, `HouseholdPreferences`, `Webhook`, `EventNotifier`, `ServerTask` in `backend-dotnet/src/Mealie.Domain/Entities/Settings/`

### EF Core Data Layer

- [ ] T019 Create `ApplicationDbContext` with `UseSnakeCaseNamingConvention()`, dual provider selection (`DB_ENGINE=sqlite` → `UseSqlite`, `DB_ENGINE=postgres` → `UseNpgsql`), and `DbSet<>` registrations for all 30+ entities in `backend-dotnet/src/Mealie.Infrastructure/Data/ApplicationDbContext.cs`
- [ ] T020 Create `IEntityTypeConfiguration` implementations for `Group`, `Household`, `User`, `ApiKey` (explicit table names, constraints, unique indexes, FK relationships matching Python schema) in `backend-dotnet/src/Mealie.Infrastructure/Data/Configurations/`
- [ ] T021 [P] Create `IEntityTypeConfiguration` for `Recipe` (unique constraint `(slug, group_id)`, JSON column for `ingredient_references`, all FK relationships) and all recipe sub-entity configurations in `backend-dotnet/src/Mealie.Infrastructure/Data/Configurations/Recipes/`
- [ ] T022 [P] Create `IEntityTypeConfiguration` for all remaining entities: organizers, foods, units, planning, shopping, preferences, junction tables (`recipes_to_tags`, `recipes_to_categories`, `recipes_to_tools`, `households_to_ingredient_foods`, `user_to_recipe`) in `backend-dotnet/src/Mealie.Infrastructure/Data/Configurations/`
- [ ] T023 Implement `ITenantContext` interface and `TenantContextAccessor` scoped service (resolves `GroupId` and `HouseholdId` from current HTTP context JWT claims) in `backend-dotnet/src/Mealie.Infrastructure/Auth/TenantContextAccessor.cs`
- [ ] T024 Implement `TenantContextMiddleware` that reads authenticated user claims and populates `ITenantContext` on every authenticated request in `backend-dotnet/src/Mealie.Api/Middleware/TenantContextMiddleware.cs`
- [ ] T025 Apply EF Core global query filters in `ApplicationDbContext.OnModelCreating`: household-scoped filter on all tenant-owned entities; group-scoped filter on group-level entities; document `IgnoreQueryFilters()` usage pattern for admin routes in `backend-dotnet/src/Mealie.Infrastructure/Data/ApplicationDbContext.cs`
- [ ] T026 Generate initial EF Core migration using PostgreSQL as canonical provider (`dotnet ef migrations add InitialSchema`) capturing the full schema; verify SQLite compatibility in `backend-dotnet/src/Mealie.Infrastructure/Data/Migrations/`

### Authentication

- [ ] T027 Implement `JwtTokenService` (generate and validate JWT access tokens, issue refresh tokens, read signing key from `SECRET` env var) in `backend-dotnet/src/Mealie.Infrastructure/Auth/JwtTokenService.cs`
- [ ] T028 [P] Implement `ApiKeyAuthenticationHandler` (reads `Authorization: Bearer` header, queries `long_live_tokens` table, verifies bcrypt hash, resolves `HouseholdId`/`GroupId` from stored user) in `backend-dotnet/src/Mealie.Infrastructure/Auth/ApiKeyAuthenticationHandler.cs`

### Middleware & Cross-Cutting

- [ ] T029 Configure Serilog: Console JSON sink in production, human-readable in development; `UseSerilogRequestLogging` with enrichment properties `{Method}`, `{Path}`, `{StatusCode}`, `{Elapsed}`, `{UserId}`, `{HouseholdId}`, `{GroupId}`, `{RequestId}`; log level from `LOG_LEVEL` env var in `backend-dotnet/src/Mealie.Api/Program.cs`
- [ ] T030 [P] Configure FluentValidation: assembly scanning of `Mealie.Application`, automatic DI registration, disable default DataAnnotations validation in `backend-dotnet/src/Mealie.Api/Program.cs`
- [ ] T031 Implement `ValidationExceptionMiddleware` catching `FluentValidation.ValidationException` and returning HTTP 422 with Pydantic-compatible body `{"detail":[{"loc":["body","field"],"msg":"...","type":"..."}]}` in `backend-dotnet/src/Mealie.Api/Middleware/ValidationExceptionMiddleware.cs`
- [ ] T032 [P] Implement `GlobalExceptionHandlerMiddleware` catching all unhandled exceptions, logging via Serilog at Error level, and returning `{"detail":"Internal server error"}` HTTP 500 in `backend-dotnet/src/Mealie.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`
- [ ] T033 Implement `MealieControllerBase` (inherits `ControllerBase`): exposes `CurrentGroupId`, `CurrentHouseholdId`, `CurrentUserId` from `ITenantContext`; provides `NotFoundOrForbidden()` helper that returns HTTP 404 regardless of whether resource exists vs. belongs to different household in `backend-dotnet/src/Mealie.Api/Controllers/MealieControllerBase.cs`
- [ ] T034 [P] Implement `PaginatedResponse<T>` wrapper, `PaginationParams` (page, per_page), and `IQueryable<T>.ToPaginatedAsync()` extension method returning `{page, per_page, total, total_pages, items}` in `backend-dotnet/src/Mealie.Shared/Pagination/`
- [ ] T035 Implement `CorrelationIdMiddleware` (generate or pass-through `X-Correlation-Id` header; add to Serilog log context as `{RequestId}`) in `backend-dotnet/src/Mealie.Api/Middleware/CorrelationIdMiddleware.cs`

### DI Wiring & Startup

- [ ] T036 Wire up `Program.cs`: middleware pipeline order (CorrelationId → Serilog → HTTPS → Auth → TenantContext → Validation → GlobalException → Controllers), DI registrations for all services, configure Swashbuckle (camelCase JSON naming policy, `SchemaId` override, serve spec at `/api/openapi.json`, Swagger UI at `/api/docs` in development only) in `backend-dotnet/src/Mealie.Api/Program.cs`
- [ ] T037 [P] Implement health check endpoints `GET /healthz` and `GET /readyz` returning `{"status":"ok","version":"2.0.0","database":"connected"}` (HTTP 200) or `{"status":"degraded"}` (HTTP 503) in `backend-dotnet/src/Mealie.Api/Controllers/Utility/HealthController.cs`
- [ ] T038 [P] Configure static file serving from `DATA_DIR` with SPA fallback: `app.UseStaticFiles(DATA_DIR/frontend)` + `app.MapFallbackToFile("index.html")`; bind all environment variable configuration (`DATABASE_URL`, `SECRET`, `BASE_URL`, `DATA_DIR`, `LOG_LEVEL`, `LDAP_*`, `OIDC_*`, `SMTP_*`, `OPENAI_API_KEY`, `ALLOW_SIGNUP`, `API_PORT`) in `backend-dotnet/src/Mealie.Api/Configuration/AppSettings.cs`

**⚠️ Checkpoint**: Foundation complete — all user story phases can now begin in parallel.

---

## Phase 3: User Story 4 — Authentication Across All Methods (Priority: P1)

**Goal**: Every supported authentication method (local, API key, LDAP, OIDC) is fully operational. US1 depends on this phase.

**Independent Test**: For each auth method POST credentials or header to a protected endpoint and receive a valid session. Verify JWT 401 on expired/tampered tokens.

- [ ] T039 [US4] Implement `AuthController` with `POST /api/auth/token` (username/password → access token + refresh token) and `POST /api/auth/token/refresh` (validate refresh token → new access token) in `backend-dotnet/src/Mealie.Api/Controllers/Auth/AuthController.cs`
- [ ] T040 [P] [US4] Implement `IAuthService` and `AuthService` (bcrypt password verification, lockout check via `login_attempts`/`locked_at`, token issuance, bcrypt API key creation) in `backend-dotnet/src/Mealie.Application/Services/Auth/AuthService.cs`
- [ ] T041 [P] [US4] Implement user account lockout: increment `login_attempts` on failed auth, set `locked_at` when threshold exceeded, reset on successful login; expose unlock via admin endpoint in `backend-dotnet/src/Mealie.Application/Services/Auth/AuthService.cs`
- [ ] T042 [US4] Implement `LdapAuthenticationHandler` using `Novell.Directory.Ldap.NETStandard`: LDAP bind with user credentials, configurable timeout (`LDAP_QUERY_TIMEOUT`, default 5 s), auto-provision new user on first successful LDAP login, return `{"detail":"LDAP server unavailable"}` HTTP 401 on timeout in `backend-dotnet/src/Mealie.Infrastructure/Auth/LdapAuthenticationHandler.cs`
- [ ] T043 [US4] Implement OIDC authentication via `Microsoft.AspNetCore.Authentication.OpenIdConnect`: configure from `OIDC_*` env vars, add `GET /api/auth/oauth` (redirect to provider) and `GET /api/auth/oauth/callback` (exchange code, issue Mealie JWT) in `backend-dotnet/src/Mealie.Api/Controllers/Auth/AuthController.cs` and `backend-dotnet/src/Mealie.Infrastructure/Auth/OidcConfiguration.cs`
- [ ] T044 [P] [US4] Implement self-registration: `GET /api/users/registration` (returns `ALLOW_SIGNUP` policy), `POST /api/users/register` (create user in default group+household when allowed) in `backend-dotnet/src/Mealie.Api/Controllers/Users/UsersController.cs`
- [ ] T045 [P] [US4] Implement password reset flow: `POST /api/users/forgot-password` (generate signed reset token, send via `IEmailService`), `POST /api/users/reset-password` (validate token, update bcrypt hash) in `backend-dotnet/src/Mealie.Api/Controllers/Users/UsersController.cs` and `backend-dotnet/src/Mealie.Application/Services/Auth/PasswordResetService.cs`
- [ ] T046 [P] [US4] Implement FluentValidation validators for all auth DTOs: `LoginRequestValidator`, `RegisterRequestValidator`, `PasswordResetRequestValidator` in `backend-dotnet/src/Mealie.Application/Validators/Auth/`
- [ ] T047 [US4] Add xUnit integration tests for all four auth methods using `WebApplicationFactory<Program>` with in-memory SQLite: local login, API key header, LDAP mock (NSubstitute), OIDC mock in `backend-dotnet/tests/Mealie.IntegrationTests/Auth/AuthIntegrationTests.cs`

**Checkpoint**: Auth fully functional — US1 can now be implemented.

---

## Phase 4: User Story 1 — End Users Experience No Disruption (Priority: P1) 🎯 MVP

**Goal**: All ~150 API endpoints operational; frontend can run unmodified against new backend; multi-tenant isolation enforced.

**Independent Test**: Point unmodified Mealie frontend at new backend; complete login → browse recipes → scrape URL → add to meal plan → generate shopping list without errors.

### User & Profile Management

- [ ] T048 [P] [US1] Create User DTOs (`UserResponse`, `UserSummaryResponse`, `UpdateUserRequest`, `CreateApiKeyRequest`, `ApiKeyResponse`, `UserRatingResponse`) in `backend-dotnet/src/Mealie.Application/Dtos/Users/`
- [ ] T049 [P] [US1] Implement `UserMapper` (Mapperly `[Mapper]` partial class, `User → UserResponse`, `User → UserSummaryResponse`) in `backend-dotnet/src/Mealie.Application/Mappers/UserMapper.cs`
- [ ] T050 [P] [US1] Implement `IUserService` and `UserService` (profile CRUD, password change, API key create/list/delete with bcrypt hashing, ratings, favorites) in `backend-dotnet/src/Mealie.Application/Services/Users/UserService.cs`
- [ ] T051 [US1] Implement `UsersController` actions: `GET/PUT /api/users/self`, `PUT /api/users/self/password`, `GET/POST/DELETE /api/users/self/api-tokens`, `GET /api/users/{user_id}`, `PUT/DELETE /api/users/{user_id}/favorites/{slug}`, `GET/POST /api/users/{user_id}/ratings`, `POST /api/users/{user_id}/ratings/{slug}` in `backend-dotnet/src/Mealie.Api/Controllers/Users/UsersController.cs`

### Groups & Households

- [ ] T052 [P] [US1] Create Group and Household DTOs (`GroupResponse`, `HouseholdResponse`, `UpdateGroupRequest`, `CreateInviteTokenRequest`, `HouseholdStatisticsResponse`) and FluentValidation validators in `backend-dotnet/src/Mealie.Application/Dtos/Groups/` and `backend-dotnet/src/Mealie.Application/Validators/Groups/`
- [ ] T053 [P] [US1] Implement `IGroupService` and `GroupService` (group profile CRUD, member list, invite token create/list/delete, labels CRUD, categories CRUD, report list/get/delete, foods/units seeding) in `backend-dotnet/src/Mealie.Application/Services/Groups/GroupService.cs`
- [ ] T054 [P] [US1] Implement `IHouseholdService` and `HouseholdService` (household profile CRUD, member list, statistics aggregation, preferences update) in `backend-dotnet/src/Mealie.Application/Services/Households/HouseholdService.cs`
- [ ] T055 [US1] Implement `GroupsController` with all `/api/groups/*` endpoints: `GET/PUT /groups/self`, `GET /groups/self/households`, `GET /groups/self/members`, `GET/POST/DELETE /groups/self/invitations`, `GET/POST/PUT/DELETE /groups/categories`, `GET/POST/PUT/DELETE /groups/labels`, `GET/GET/DELETE /groups/reports`, `POST /groups/seed/foods`, `POST /groups/seed/units` in `backend-dotnet/src/Mealie.Api/Controllers/Groups/GroupsController.cs`
- [ ] T056 [US1] Implement `HouseholdsController` with `/api/households/*` endpoints: `GET/PUT /households/self`, `GET /households/self/members`, `GET /households/self/statistics` in `backend-dotnet/src/Mealie.Api/Controllers/Households/HouseholdsController.cs`

### Recipe CRUD (Core)

- [ ] T057 [P] [US1] Create Recipe DTOs: `RecipeDetailResponse`, `RecipeSummaryResponse`, `CreateRecipeRequest`, `UpdateRecipeRequest`, `PatchRecipeRequest` (with nested `RecipeIngredientDto`, `RecipeInstructionDto`, `RecipeNoteDto`, `RecipeAssetDto`, `NutritionDto`) in `backend-dotnet/src/Mealie.Application/Dtos/Recipes/`
- [ ] T058 [P] [US1] Implement `RecipeMapper` (Mapperly): `Recipe → RecipeDetailResponse`, `Recipe → RecipeSummaryResponse`, `CreateRecipeRequest → Recipe`; handle nested ingredient/instruction/tag/category/tool collections in `backend-dotnet/src/Mealie.Application/Mappers/RecipeMapper.cs`
- [ ] T059 [P] [US1] Implement FluentValidation validators: `CreateRecipeRequestValidator` (name required, max 255), `UpdateRecipeRequestValidator`, `PatchRecipeRequestValidator` in `backend-dotnet/src/Mealie.Application/Validators/Recipes/`
- [ ] T060 [US1] Implement `IRecipeService` and `RecipeService`: CRUD by slug, slug generation (unique within group), duplicate recipe, recipe filtering by tag/category/tool/food/cookbook (query DSL from API contract), full-text search, pagination, image file save/delete from `DATA_DIR` in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeService.cs`
- [ ] T061 [US1] Implement `RecipesController` core actions: `GET /api/recipes` (paginated, filterable), `POST /api/recipes`, `GET /api/recipes/summary`, `GET/PUT/PATCH/DELETE /api/recipes/{slug}`, `POST /api/recipes/{slug}/duplicate`, `PUT /api/recipes/{slug}/image` in `backend-dotnet/src/Mealie.Api/Controllers/Recipes/RecipesController.cs`

### Organizers (Tags, Categories, Tools, Cookbooks, Labels)

- [ ] T062 [P] [US1] Create Organizer DTOs (`TagResponse`, `CategoryResponse`, `ToolResponse`, `CookbookResponse`, `CookbookSummaryResponse`, `CreateOrganizerRequest`, `UpdateCookbookRequest`) and Mapperly mappers in `backend-dotnet/src/Mealie.Application/Dtos/Organizers/` and `backend-dotnet/src/Mealie.Application/Mappers/OrganizerMapper.cs`
- [ ] T063 [P] [US1] Implement `IOrganizerService` and `OrganizerService`: generic CRUD for tags, categories, and tools; slug-lookup; assignment to/from recipes in `backend-dotnet/src/Mealie.Application/Services/Organizers/OrganizerService.cs`
- [ ] T064 [US1] Implement `TagsController`, `CategoriesController`, `ToolsController`: full CRUD + `GET /organizers/{type}/slug/{slug}` in `backend-dotnet/src/Mealie.Api/Controllers/Organizers/`
- [ ] T065 [P] [US1] Implement `ICookbookService` and `CookbookService` (CRUD, position reordering, public/private toggle, filter by category/tag) in `backend-dotnet/src/Mealie.Application/Services/Cookbooks/CookbookService.cs`
- [ ] T066 [US1] Implement `CookbooksController`: `GET/POST/PUT /households/self/cookbooks`, `GET/PUT/DELETE /households/self/cookbooks/{item_id}` in `backend-dotnet/src/Mealie.Api/Controllers/Households/CookbooksController.cs`

### Foods & Units

- [ ] T067 [P] [US1] Create Food and Unit DTOs (`IngredientFoodResponse`, `IngredientUnitResponse`, `CreateFoodRequest`, `CreateUnitRequest`, `MergeFoodRequest`, `MergeUnitRequest`) and Mapperly mappers in `backend-dotnet/src/Mealie.Application/Dtos/Ingredients/` and `backend-dotnet/src/Mealie.Application/Mappers/IngredientMapper.cs`
- [ ] T068 [P] [US1] Implement `IFoodService` and `FoodService` (CRUD, fuzzy name normalization index, merge: reassign all ingredient references from source → target, delete source) in `backend-dotnet/src/Mealie.Application/Services/Foods/FoodService.cs`
- [ ] T069 [P] [US1] Implement `IUnitService` and `UnitService` (CRUD, name/abbreviation normalization, merge: reassign all ingredient references, delete source) in `backend-dotnet/src/Mealie.Application/Services/Units/UnitService.cs`
- [ ] T070 [US1] Implement `FoodsController` (`GET/POST /api/foods`, `GET/PUT/DELETE /api/foods/{food_id}`, `POST /api/foods/{food_id}/merge`) and `UnitsController` (same pattern for `/api/units`) in `backend-dotnet/src/Mealie.Api/Controllers/`

### Meal Planner

- [ ] T071 [P] [US1] Create MealPlan DTOs (`MealPlanResponse`, `CreateMealPlanRequest`, `UpdateMealPlanRequest`, `MealPlanRuleResponse`, `CreateMealPlanRuleRequest`) and validators in `backend-dotnet/src/Mealie.Application/Dtos/MealPlans/` and `backend-dotnet/src/Mealie.Application/Validators/MealPlans/`
- [ ] T072 [P] [US1] Implement `IMealPlanService` and `MealPlanService` (CRUD, date-range queries, today's plan, random recipe selection filtered by rules, plan rules CRUD) in `backend-dotnet/src/Mealie.Application/Services/MealPlans/MealPlanService.cs`
- [ ] T073 [US1] Implement `MealPlansController`: `GET/POST /households/self/meal-plans`, `GET /meal-plans/today`, `GET/PUT/DELETE /meal-plans/{item_id}`, `GET /meal-plans/random`, `GET/POST/PUT/DELETE /meal-plans/rules`, `GET/POST/PUT/DELETE /meal-plans/rules/{item_id}` in `backend-dotnet/src/Mealie.Api/Controllers/Households/MealPlansController.cs`

### Shopping Lists

- [ ] T074 [P] [US1] Create Shopping DTOs (`ShoppingListResponse`, `CreateShoppingListRequest`, `ShoppingListItemResponse`, `CreateShoppingListItemRequest`, `UpdateShoppingListItemRequest`, `BulkDeleteRequest`) and Mapperly mappers in `backend-dotnet/src/Mealie.Application/Dtos/Shopping/` and `backend-dotnet/src/Mealie.Application/Mappers/ShoppingMapper.cs`
- [ ] T075 [P] [US1] Implement `IShoppingListService` and `ShoppingListService`: list CRUD, item CRUD, `AddRecipeToList` (expand recipe ingredients → list items with quantity scaling), `RemoveRecipeFromList`, `BulkDeleteItems` in `backend-dotnet/src/Mealie.Application/Services/Shopping/ShoppingListService.cs`
- [ ] T076 [US1] Implement `ShoppingListsController` and `ShoppingItemsController` with all `/api/households/self/shopping/*` endpoints from the API contract (lists, items, bulk-delete, recipe add/remove) in `backend-dotnet/src/Mealie.Api/Controllers/Households/`

### Recipe Sub-Resources

- [ ] T077 [P] [US1] Implement recipe comments service (`IRecipeCommentService`, `RecipeCommentService`) and controller actions `GET/POST /recipes/{slug}/comments`, `PUT/DELETE /recipes/{slug}/comments/{comment_id}` in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeCommentService.cs` and `backend-dotnet/src/Mealie.Api/Controllers/Recipes/RecipesController.cs`
- [ ] T078 [P] [US1] Implement recipe timeline service (`IRecipeTimelineService`, `RecipeTimelineService`) and controller actions `GET/POST /recipes/{slug}/timeline`, `PUT/DELETE /recipes/{slug}/timeline/{event_id}` in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeTimelineService.cs`
- [ ] T079 [P] [US1] Implement recipe asset service (`IRecipeAssetService`): multipart form upload to `DATA_DIR/recipes/{recipe_id}/assets/`, delete, list; controller actions `GET/POST /recipes/{slug}/assets`, `DELETE /recipes/{slug}/assets/{file_name}` in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeAssetService.cs`
- [ ] T080 [P] [US1] Implement recipe share token service (`IRecipeShareService`, `RecipeShareService`) and controller actions `GET/POST /recipes/{slug}/share`, `DELETE /recipes/{slug}/share/{token_id}`, `GET /recipes/shared/{token_id}` (public, no auth) in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeShareService.cs`
- [ ] T081 [P] [US1] Implement recipe bulk actions: `POST /api/recipes/bulk-actions/delete`, `/tag`, `/categorize`, `/export`, `/undelete` (dispatches to `IRecipeService` and `IRecipeExportService`) in `backend-dotnet/src/Mealie.Api/Controllers/Recipes/RecipesController.cs`

### Recipe Scraper

- [ ] T082 [P] [US1] Implement `JsonLdScraperStrategy`: extract `<script type="application/ld+json">` blocks, parse `@type: Recipe` schema.org JSON, map all fields (name, description, recipeIngredient, recipeInstructions, image, recipeYield, totalTime, etc.) to `ScrapedRecipeDto` in `backend-dotnet/src/Mealie.Infrastructure/Scraper/JsonLdScraperStrategy.cs`
- [ ] T083 [US1] Implement `MicrodataScraperStrategy`: parse `itemprop` attributes for schema.org/Recipe properties using HtmlAgilityPack as fallback when JSON-LD is absent in `backend-dotnet/src/Mealie.Infrastructure/Scraper/MicrodataScraperStrategy.cs`
- [ ] T084 [US1] Implement `HeuristicScraperStrategy`: common CSS selector patterns for recipe title, ingredient `<ul>`, instruction `<ol>` using AngleSharp as last-resort fallback (covers sites without schema.org markup) in `backend-dotnet/src/Mealie.Infrastructure/Scraper/HeuristicScraperStrategy.cs`
- [ ] T085 [US1] Implement `RecipeScraperService` orchestrating JSON-LD → Microdata → Heuristic strategies; detect JS-rendering sites and return `{"scraping_not_supported": true}` with an HTTP 200 (not 500); register `HttpClient` with timeout in `backend-dotnet/src/Mealie.Infrastructure/Scraper/RecipeScraperService.cs`
- [ ] T086 [US1] Implement scraper controller actions: `POST /api/recipes/create-url` (single URL scrape → create recipe), `POST /api/recipes/create-url/bulk` (bulk URL list, async per-URL processing) in `backend-dotnet/src/Mealie.Api/Controllers/Recipes/RecipesController.cs`

### Recipe Export & ZIP Import

- [ ] T087 [P] [US1] Implement `IRecipeExportService` and `RecipeExportService` (serialize recipe to Mealie JSON format, compress to zip); controller actions `GET /api/recipes/exports` (list export types), `GET /api/recipes/{slug}/exports` in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeExportService.cs`
- [ ] T088 [US1] Implement ZIP recipe import handler (`POST /api/recipes/create-zip`: extract zip, detect format, delegate to `RecipeImportService`) and OCR endpoint stub (`POST /api/recipes/create-image-ocr`: returns HTTP 501 if OCR library absent) in `backend-dotnet/src/Mealie.Api/Controllers/Recipes/RecipesController.cs`

### Admin Endpoints

- [ ] T089 [P] [US1] Implement `IAdminUserService` (admin-level user CRUD bypassing household filter via `IgnoreQueryFilters()`) and `IAdminGroupService`, `IAdminHouseholdService` in `backend-dotnet/src/Mealie.Application/Services/Admin/`
- [ ] T090 [US1] Implement `AdminController`: `GET/POST/PUT/DELETE /api/admin/users`, `GET/POST/PUT/DELETE /api/admin/groups`, `GET/POST/PUT/DELETE /api/admin/households`, `GET /admin/about`, `GET /admin/statistics`, `GET /admin/email`, `POST /admin/email` (test email), `GET/GET/DELETE/POST /admin/debug/*` in `backend-dotnet/src/Mealie.Api/Controllers/Admin/AdminController.cs`
- [ ] T091 [P] [US1] Implement `BackupsController`: `GET/POST /api/admin/backups`, `GET/DELETE /api/admin/backups/{file_name}`, `POST /api/admin/backups/restore` wired to `IBackupService` in `backend-dotnet/src/Mealie.Api/Controllers/Admin/BackupsController.cs`

### Explore (Public) Endpoints

- [ ] T092 [P] [US1] Implement `ExploreController` with all public (no-auth) endpoints: `GET /api/explore/groups/{group_slug}`, `/recipes`, `/recipes/{recipe_slug}`, `/cookbooks`, `/cookbooks/{item_id}`, `/foods`, `/tags`, `/categories`, `/tools`; use `IgnoreQueryFilters()` + explicit `IsPublic = true` predicate; scoped to group by slug in `backend-dotnet/src/Mealie.Api/Controllers/Explore/ExploreController.cs`

### App Info & Media

- [ ] T093 [P] [US1] Implement `AppInfoController` (`GET /api/app/about`, `GET /api/app/about/oidc`) and `DebugController` (`GET /api/debug/version`) in `backend-dotnet/src/Mealie.Api/Controllers/Utility/`
- [ ] T094 [US1] Configure media file serving routes: `GET /api/media/recipes/{recipe_id}/images/{file_name}`, `GET /api/media/recipes/{recipe_id}/assets/{file_name}`, `GET /api/media/users/{user_id}/images/{file_name}`, `GET /api/media/groups/{group_id}/images/{file_name}` mapped to `DATA_DIR` via `PhysicalFileProvider` in `backend-dotnet/src/Mealie.Api/Program.cs`

### OpenAI Integration

- [ ] T095 [P] [US1] Implement `OpenAiController` (`POST /api/openai/parse-ingredient`, `POST /api/openai/parse-recipe`) returning HTTP 424 `{"detail":"OpenAI is not configured"}` when `OPENAI_API_KEY` is absent; implement `IOpenAiService` with GPT-4o structured output call when key is present in `backend-dotnet/src/Mealie.Api/Controllers/Parser/OpenAiController.cs` and `backend-dotnet/src/Mealie.Infrastructure/Parser/OpenAiParserService.cs`

**Checkpoint**: US1 fully functional — frontend can operate against the new backend end-to-end.

---

## Phase 5: User Story 3 — API Compatibility Is Verifiable (Priority: P1)

**Goal**: C# OpenAPI spec is provably identical to Python spec; TypeScript codegen compiles without errors.

**Independent Test**: `scripts/validate-openapi-compat.sh` exits 0 showing zero path or schema differences.

- [ ] T096 [US3] Configure Swashbuckle `SchemaGeneratorOptions.SchemaIdSelector` to produce component names matching Python Pydantic model names exactly (e.g., `RecipeResponse` not `RecipeDetailResponseDto`) in `backend-dotnet/src/Mealie.Api/Program.cs`
- [ ] T097 [P] [US3] Implement `PydanticValidationOperationFilter` (Swashbuckle `IOperationFilter`): add HTTP 422 `ValidationError` response schema to all `POST`/`PUT`/`PATCH` operations automatically in `backend-dotnet/src/Mealie.Api/Filters/PydanticValidationOperationFilter.cs`
- [ ] T098 [US3] Create `backend-dotnet/scripts/validate-openapi-compat.sh`: start both backends, fetch each `/api/openapi.json`, run `swagger-diff` (or `openapi-diff`), fail if any path missing, required field removed, or response type changed; document how to run locally in `backend-dotnet/scripts/validate-openapi-compat.sh`
- [ ] T099 [P] [US3] Add OpenAPI diff CI step to `azure-pipelines.yml` for branch `001-csharp-backend-migration`: run `validate-openapi-compat.sh` as a gating check (fails the pipeline on schema divergence)
- [ ] T100 [US3] Document TypeScript API client regeneration in `backend-dotnet/README.md`: how to re-run frontend codegen from `/api/openapi.json` after any schema change; add it as a required step in the PR checklist

**Checkpoint**: US3 verifiable — schema divergence is caught automatically in CI.

---

## Phase 6: User Story 2 — Administrators Migrate Existing Data Without Loss (Priority: P1)

**Goal**: Standalone migration CLI transfers all data from Python SQLite/PostgreSQL → C# schema with zero data loss, preserved IDs, and per-record error tolerance.

**Independent Test**: Run tool against a known-good Python SQLite dump; verify API returns identical record counts; migration is idempotent on second run.

- [ ] T101 [US2] Create `Mealie.Migration` CLI entry point: argument parsing (`--source`, `--target`, `--source-engine sqlite|postgres`, `--target-engine sqlite|postgres`), configuration validation, and exit codes (0 = success, 1 = already migrated or fatal error) in `backend-dotnet/tools/Mealie.Migration/Program.cs`
- [ ] T102 [US2] Implement idempotency guard: create `__mealie_migration_log` table on first run, write completion record; detect existing record on subsequent runs and exit with code 1 and message `"Database has already been migrated. Aborting to prevent data duplication."` in `backend-dotnet/tools/Mealie.Migration/MigrationRunner.cs`
- [ ] T103 [US2] Implement Dapper source readers for Tier 1 entities (read-only, no writes to source): `groups`, `households`, `users`, `multi_purpose_labels`, `group_preferences`, `household_preferences` in `backend-dotnet/tools/Mealie.Migration/SourceReaders/Tier1Reader.cs`
- [ ] T104 [P] [US2] Implement Dapper source readers for Tier 2 entities: `ingredient_units`, `ingredient_foods`, `ingredient_food_aliases`, `tags`, `categories`, `tools`, `cookbooks` in `backend-dotnet/tools/Mealie.Migration/SourceReaders/Tier2Reader.cs`
- [ ] T105 [P] [US2] Implement Dapper source readers for Tier 3 entities: `recipes`, `recipe_ingredients`, `recipe_instructions`, `recipe_notes`, `recipe_assets`, `recipe_comments`, `recipe_timeline_events`, `recipe_share_tokens` in `backend-dotnet/tools/Mealie.Migration/SourceReaders/Tier3Reader.cs`
- [ ] T106 [P] [US2] Implement Dapper source readers for Tier 4 entities: `group_meal_plans`, `shopping_lists`, `shopping_list_items`, `group_webhooks`, `group_event_notifiers`, `long_live_tokens`, and all junction tables (`recipes_to_tags`, `recipes_to_categories`, `recipes_to_tools`, `user_to_recipe`, `households_to_ingredient_foods`) in `backend-dotnet/tools/Mealie.Migration/SourceReaders/Tier4Reader.cs`
- [ ] T107 [US2] Implement EF Core target writers with identity insert enabled for all entity tiers: preserve all UUID and integer PKs verbatim, preserve `recipe.slug` verbatim (skip on unique constraint violation with log), disable EF Core ID generation for migration context in `backend-dotnet/tools/Mealie.Migration/TargetWriters/`
- [ ] T108 [US2] Implement per-record error handling in `MigrationRunner`: wrap each entity row insert in try/catch; log skipped records with full detail (`EntityType`, `SourceId`, `ErrorMessage`) to Serilog; continue processing remaining records after any single-record failure in `backend-dotnet/tools/Mealie.Migration/MigrationRunner.cs`
- [ ] T109 [P] [US2] Implement `ReportGenerator` printing a stdout migration summary table (`Entity | Migrated | Skipped | Errors`) and a Serilog-written JSON report file for post-migration audit in `backend-dotnet/tools/Mealie.Migration/ReportGenerator.cs`
- [ ] T110 [P] [US2] Implement source-to-target entity mappers (source schema `string` UUIDs → `Guid`, source `snake_case` column names → C# property names, nullable coercion) in `backend-dotnet/tools/Mealie.Migration/Mappers/`
- [ ] T111 [US2] Implement `MigrationRunner` orchestrator invoking tier readers in dependency-safe order (T1 → T2 → T3 → T4 → junctions → mark complete) with transaction per tier for atomicity in `backend-dotnet/tools/Mealie.Migration/MigrationRunner.cs`
- [ ] T112 [P] [US2] Write xUnit integration test: run `MigrationRunner` against a SQLite fixture database, assert row counts match expected, assert `__mealie_migration_log` exists, assert second run exits with code 1 in `backend-dotnet/tests/Mealie.IntegrationTests/Migration/MigrationRunnerTests.cs`

**Checkpoint**: US2 complete — migration tool produces a fully operational C# database from a Python source.

---

## Phase 7: User Story 5 — Background Services Continue to Operate (Priority: P2)

**Goal**: Scheduler, webhooks, backup, and email notifications fire on schedule without user action.

**Independent Test**: Configure a webhook; trigger a recipe create event; observe webhook delivery logged by Serilog.

- [ ] T113 [US5] Implement `IWebhookDeliveryService` and `WebhookDeliveryService`: fire-and-forget `HttpClient.PostAsJsonAsync`, log `Warning` with `{WebhookId}`, `{Url}`, `{StatusCode}`, `{Exception}` on any failure (no retry, no dead-letter queue per research.md §7) in `backend-dotnet/src/Mealie.Infrastructure/Webhooks/WebhookDeliveryService.cs`
- [ ] T114 [P] [US5] Implement in-process `IEventBus` and `EventBus` (in-memory publish/subscribe): publish domain events from service layer; subscribe `WebhookDeliveryService` and notification handlers; fire-and-forget dispatch in `backend-dotnet/src/Mealie.Infrastructure/Webhooks/EventBus.cs`
- [ ] T115 [US5] Implement `SchedulerHostedService` (`BackgroundService` subclass): 1-minute tick loop, reads enabled job schedules from DB/config at startup, computes next-run from "now" on restart (no job state migrated), dispatches due jobs in `backend-dotnet/src/Mealie.Infrastructure/Scheduler/SchedulerHostedService.cs`
- [ ] T116 [P] [US5] Implement `ScheduledBackupJob`: create timestamped zip archive of all data in `DATA_DIR/backups/`, log result; wire to admin on-demand `POST /api/admin/backups` as well as scheduler trigger in `backend-dotnet/src/Mealie.Infrastructure/Scheduler/Jobs/ScheduledBackupJob.cs`
- [ ] T117 [P] [US5] Implement `MealPlanNotificationJob`: query today's meal plans with notification settings, fire webhook or email notification for each configured action in `backend-dotnet/src/Mealie.Infrastructure/Scheduler/Jobs/MealPlanNotificationJob.cs`
- [ ] T118 [P] [US5] Implement `WebhooksController`: `GET/POST /api/households/self/webhooks`, `PUT/DELETE /api/households/self/webhooks/{item_id}`, `POST /api/households/self/webhooks/test` (synchronous test delivery) in `backend-dotnet/src/Mealie.Api/Controllers/Households/WebhooksController.cs`
- [ ] T119 [P] [US5] Implement `EventNotifiersController`: `GET/POST/PUT/DELETE /api/households/self/event-notifications`, `POST /api/households/self/event-notifications/{item_id}/test` in `backend-dotnet/src/Mealie.Api/Controllers/Households/EventNotifiersController.cs`
- [ ] T120 [US5] Implement `IBackupService` and `BackupService` (create zip archive with all recipe images + DB dump, restore from archive, list existing backups with metadata); wire to `BackupsController` and `ScheduledBackupJob` in `backend-dotnet/src/Mealie.Application/Services/Admin/BackupService.cs`
- [ ] T121 [P] [US5] Implement `IEmailService` and `EmailService` using MailKit: SMTP connection from `SMTP_*` env vars, disabled gracefully when `SMTP_HOST` is absent; Razor email templates for invitation and password reset emails in `backend-dotnet/src/Mealie.Infrastructure/Email/EmailService.cs` and `backend-dotnet/src/Mealie.Infrastructure/Email/Templates/`

**Checkpoint**: US5 complete — all scheduled and event-driven background services operational.

---

## Phase 8: User Story 8 — Ingredient Parsing at Equivalent Quality (Priority: P2)

**Goal**: Freeform ingredient strings are parsed to structured `{quantity, unit, food, note}` at equivalent quality to the Python brute-force parser.

**Independent Test**: Submit 50 representative ingredient strings from the Python test corpus; verify ≥95% match on quantity, unit, and food extraction.

- [ ] T122 [US8] Implement `QuantityTokenizer`: regex patterns for integers, decimals, fractions (`1/2`), Unicode vulgar fractions (`½ ¼ ¾`), and ranges (`1-2`); return `(decimal quantity, string remainingText)` in `backend-dotnet/src/Mealie.Infrastructure/Parser/QuantityTokenizer.cs`
- [ ] T123 [P] [US8] Implement `UnitMatcher`: normalize candidate unit string against `IngredientUnit` table (name, plural name, abbreviation, plural abbreviation, all normalized); return matched `IngredientUnit` or null in `backend-dotnet/src/Mealie.Infrastructure/Parser/UnitMatcher.cs`
- [ ] T124 [P] [US8] Implement `FoodMatcher`: extract food name from remaining text after quantity and unit removal; match against `IngredientFood` table by `name_normalized`; preserve unmatched text as `note` in `backend-dotnet/src/Mealie.Infrastructure/Parser/FoodMatcher.cs`
- [ ] T125 [US8] Implement `IngredientParserService` orchestrating `QuantityTokenizer → UnitMatcher → FoodMatcher`; when `OPENAI_API_KEY` is set and parser confidence is low, delegate to `IOpenAiService` for GPT-4o structured output; return `ParsedIngredientDto` with confidence level in `backend-dotnet/src/Mealie.Infrastructure/Parser/IngredientParserService.cs`
- [ ] T126 [US8] Implement `ParserController`: `POST /api/parser/ingredient` (single string), `POST /api/parser/ingredients` (batch list) returning `ParsedIngredientResponse` shape matching API contract in `backend-dotnet/src/Mealie.Api/Controllers/Parser/ParserController.cs`
- [ ] T127 [P] [US8] Write xUnit unit tests for `QuantityTokenizer`, `UnitMatcher`, and `FoodMatcher` using a fixed test corpus of 50 ingredient strings with known expected outputs in `backend-dotnet/tests/Mealie.UnitTests/Parser/`

**Checkpoint**: US8 complete — ingredient parser matches Python backend quality on standard test corpus.

---

## Phase 9: User Story 7 — Recipe Import from External Sources (Priority: P2)

**Goal**: Chowdown, Paprika, Nextcloud Cookbook, Tandoor, and Mealie own-format imports all produce correctly populated recipes.

**Independent Test**: Submit each supported format's sample file to `POST /api/groups/migrations`; verify recipe title, ingredients, and instructions are populated.

- [ ] T128 [US7] Define `IMigrationParser` interface (`bool CanParse(Stream input)`, `IEnumerable<ScrapedRecipeDto> Parse(Stream input)`) and `MigrationParserBase` abstract class with common field-mapping helpers in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/MigrationParserBase.cs`
- [ ] T129 [P] [US7] Implement `ChowdownMigrationParser`: extract from Chowdown zip (YAML front matter + markdown instructions per file), map to `ScrapedRecipeDto` in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/ChowdownMigrationParser.cs`
- [ ] T130 [P] [US7] Implement `PaprikaMigrationParser`: extract from `.paprikarecipes` zip (gzip-compressed JSON per recipe), map all supported Paprika fields to `ScrapedRecipeDto` in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/PaprikaMigrationParser.cs`
- [ ] T131 [P] [US7] Implement `NextcloudCookbookMigrationParser`: parse Nextcloud Cookbook JSON export (schema.org/Recipe format), map to `ScrapedRecipeDto` in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/NextcloudCookbookMigrationParser.cs`
- [ ] T132 [P] [US7] Implement `TandoorMigrationParser`: parse Tandoor JSON export format, map recipe fields including steps, ingredients, and keywords in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/TandoorMigrationParser.cs`
- [ ] T133 [P] [US7] Implement `MealieBackupImportParser`: restore from Mealie own JSON format (preserves original UUIDs and slugs for idempotent re-import) in `backend-dotnet/src/Mealie.Infrastructure/Scraper/Importers/MealieBackupImportParser.cs`
- [ ] T134 [US7] Implement `RecipeImportService`: auto-detect format via `IMigrationParser.CanParse()`, create recipes via `IRecipeService`, return import report (count created, skipped, errors); return HTTP 400 with actionable message for unsupported formats (not 500) in `backend-dotnet/src/Mealie.Application/Services/Recipes/RecipeImportService.cs`
- [ ] T135 [US7] Wire `RecipeImportService` to `POST /api/groups/migrations` in `GroupsController` (multipart form upload); handle malformed zip/JSON with structured error response in `backend-dotnet/src/Mealie.Api/Controllers/Groups/GroupsController.cs`

**Checkpoint**: US7 complete — all supported import formats produce well-formed recipe records.

---

## Phase 10: User Story 6 — Developers Can Extend and Maintain the Backend (Priority: P2)

**Goal**: Any developer with .NET 10 SDK installed can build, test, and extend the backend within 15 minutes by following the README alone.

**Independent Test**: A timed first-run with a developer unfamiliar with the codebase: build ✅, seed ✅, run ✅, test ✅ all within 15 minutes.

- [ ] T136 [US6] Create `backend-dotnet/README.md` with the full 15-minute onboarding guide from `quickstart.md`: prerequisites, clone+build, `.env` configuration, `dotnet ef database update`, `dotnet run -- seed`, API URL, `dotnet test`, hot reload, PostgreSQL variant, project structure overview, common issues table
- [ ] T137 [P] [US6] Create `backend-dotnet/.env.example` listing all supported environment variables (`DB_ENGINE`, `DATABASE_URL`, `SECRET`, `BASE_URL`, `DATA_DIR`, `LOG_LEVEL`, `API_PORT`, `ALLOW_SIGNUP`, `LDAP_*`, `OIDC_*`, `SMTP_*`, `OPENAI_API_KEY`) with inline comments and safe defaults
- [ ] T138 [US6] Implement `seed` startup verb (`dotnet run --project src/Mealie.Api -- seed`): creates default `"Home"` group, `"Family"` household, admin user `admin@example.com`/`admin`, and 20 sample recipes using `IRecipeService` in `backend-dotnet/src/Mealie.Api/Commands/SeedCommand.cs`
- [ ] T139 [P] [US6] Create `IDesignTimeDbContextFactory<ApplicationDbContext>` reading `DATABASE_URL` env var to support `dotnet ef migrations` commands without running the full application in `backend-dotnet/src/Mealie.Infrastructure/Data/ApplicationDbContextFactory.cs`
- [ ] T140 [P] [US6] Create `Dockerfile` using multi-stage build: `mcr.microsoft.com/dotnet/sdk:10.0` for build, `mcr.microsoft.com/dotnet/aspnet:10.0` for runtime; expose port 9000; use same image naming convention as Python backend in `backend-dotnet/docker/Dockerfile`
- [ ] T141 [P] [US6] Configure `launchSettings.json` with `Development` profile (hot reload via `dotnet watch`, environment variables, port 9000) and a `Docker` profile in `backend-dotnet/src/Mealie.Api/Properties/launchSettings.json`

**Checkpoint**: US6 complete — developer onboarding is verified end-to-end within 15-minute target.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening across all stories; validate the complete solution before declaring the branch ready for review.

- [ ] T142 [P] Audit all controllers for missing `[Authorize]` attributes or incorrect role checks; verify `ExploreController` and health/app-info endpoints are correctly unauthenticated; verify admin endpoints require admin role in `backend-dotnet/src/Mealie.Api/Controllers/`
- [ ] T143 [P] Verify `openapi-compat` diff produces zero differences by running `backend-dotnet/scripts/validate-openapi-compat.sh` locally; fix any remaining field name, type, or path mismatches surfaced by the diff
- [ ] T144 Run `dotnet test backend-dotnet/Mealie.sln` with `--logger trx` and confirm all unit and integration tests pass within the 10-minute CI time budget; fix any failures in `backend-dotnet/tests/`
- [ ] T145 Validate quickstart onboarding steps against the final project state (build, seed, run, test all succeed following `backend-dotnet/README.md` exactly); update any stale commands, ports, or paths
- [ ] T146 [P] Verify `mealie/` Python directory is untouched (`git diff origin/main -- mealie/` shows no changes); confirm all commits on `001-csharp-backend-migration` only touch `backend-dotnet/`, `specs/001-csharp-backend-migration/`, and CI config

---

## Dependencies & Execution Order

### Phase Dependencies

| Phase | Depends On | Blocks |
|-------|------------|--------|
| **Phase 1 — Setup** | Nothing | Phase 2 |
| **Phase 2 — Foundational** | Phase 1 complete | All user story phases |
| **Phase 3 — US4 (Auth)** | Phase 2 complete | Phase 4 (US1 needs auth to be testable) |
| **Phase 4 — US1 (Core API)** | Phase 3 complete | Phase 5 (US3 needs running API to diff) |
| **Phase 5 — US3 (OpenAPI compat)** | Phase 4 complete | Nothing |
| **Phase 6 — US2 (Migration tool)** | Phase 2 complete | Nothing (independent of US1–US3) |
| **Phase 7 — US5 (Background services)** | Phase 2 complete | Nothing (can run in parallel with US1–US3) |
| **Phase 8 — US8 (Parser)** | Phase 2 complete | Nothing (independent) |
| **Phase 9 — US7 (Imports)** | Phase 4 (needs `IRecipeService`) | Nothing |
| **Phase 10 — US6 (Dev experience)** | Phase 4 complete (needs working app to document) | Nothing |
| **Phase 11 — Polish** | All desired phases complete | — |

### User Story Dependencies

- **US4 (Auth)** [Phase 3]: Depends on Foundational (Phase 2) only.
- **US1 (Core API)** [Phase 4]: Depends on US4. The auth handlers must work before US1 is independently testable.
- **US3 (OpenAPI compat)** [Phase 5]: Depends on US1 (needs all ~150 endpoints serving their spec shapes).
- **US2 (Migration tool)** [Phase 6]: Depends on Foundational only (domain entities + EF Core). Can proceed in parallel with US4 and US1.
- **US5 (Background services)** [Phase 7]: Depends on Foundational and US1 (needs `IRecipeService`, `IBackupService`). Can start in parallel with later US1 tasks.
- **US8 (Parser)** [Phase 8]: Depends on Foundational only. `IngredientFood` and `IngredientUnit` entities must exist.
- **US7 (Imports)** [Phase 9]: Depends on US1 (needs `IRecipeService` to create imported recipes).
- **US6 (Dev experience)** [Phase 10]: Depends on US1 being working so the documented commands actually succeed.

### Parallel Opportunities Within Each Phase

**Phase 1** — T002–T009 can all run in parallel after T001 (sln creation).  
**Phase 2** — T013–T018 (entity cluster) can run in parallel; T019 (DbContext) needs T013–T018; T020–T022 (configs) can run in parallel after T019; T027–T028 (auth) can run in parallel after T019; T029–T035 (middleware) can run in parallel.  
**Phase 3** — T040–T046 can run in parallel; T039 (controller) and T047 (integration tests) depend on T040.  
**Phase 4** — Within each sub-group, DTO+Mapper+Validator tasks ([P]-marked) can run in parallel; Service depends on DTOs; Controller depends on Service.  
**Phase 6** — T103–T106 (tier readers) can all run in parallel after T102.

---

## Parallel Execution Example: Phase 4, US1 Recipe Group

```
# In parallel (different files, no shared dependencies):
Task T057: Create Recipe DTOs in Mealie.Application/Dtos/Recipes/
Task T058: Create RecipeMapper in Mealie.Application/Mappers/RecipeMapper.cs
Task T059: Create recipe FluentValidation validators in Mealie.Application/Validators/Recipes/

# After T057 + T058 complete:
Task T060: Implement RecipeService in Mealie.Application/Services/Recipes/RecipeService.cs

# After T060 completes:
Task T061: Implement RecipesController in Mealie.Api/Controllers/Recipes/RecipesController.cs
```

---

## Implementation Strategy

### MVP First (US4 + US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks everything)
3. Complete Phase 3: US4 — Auth working
4. Complete Phase 4: US1 — Core API working
5. **STOP and VALIDATE**: Point the Mealie frontend at the new backend; run end-to-end session
6. Demo if ready; gather feedback before proceeding to P2 stories

### Incremental Delivery (After MVP)

- Add **Phase 5 (US3)** → automated OpenAPI compat CI gate — prevents regressions
- Add **Phase 6 (US2)** → migration tool → enables real-world production migration
- Add remaining P2 phases in any order (they are independent):
  - Phase 7 (US5): Background jobs
  - Phase 8 (US8): Ingredient parser quality
  - Phase 9 (US7): External format imports
  - Phase 10 (US6): Polish developer experience

### Parallel Team Strategy

With multiple developers after Phase 2 completes:

| Developer | Phase |
|-----------|-------|
| A (lead) | Phase 3 (US4 Auth) → Phase 4 (US1 Core API) |
| B | Phase 6 (US2 Migration tool) — independent |
| C | Phase 8 (US8 Parser) → Phase 7 (US5 Background) |

---

## Notes

- `backend-dotnet/` is the root of all C# work. **No file under `mealie/` is ever modified.**
- Branch `001-csharp-backend-migration` is active for all tasks. **Never merge to `main`** as part of task execution.
- `[P]` tasks = different files, no dependency on incomplete tasks in the same phase — safe to parallelize.
- `[Story]` label maps each task to a specific user story for independent testing and delivery traceability.
- Commit after each logical group of tasks; include task ID(s) in commit message (e.g., `feat: T057-T061 Recipe CRUD`).
- Stop at each **Checkpoint** to validate the story independently before proceeding.
