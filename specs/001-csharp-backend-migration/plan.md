# Implementation Plan: C# Backend Consolidation Refactors

**Branch**: `001-csharp-backend-migration-plan` | **Date**: 2026-05-03 | **Spec**: [`specs/001-csharp-backend-migration/spec.md`](spec.md)  
**Input**: Existing migration spec plus duplication review for targeted C# backend consolidation work

## Summary

Plan an incremental, behavior-safe refactor pass across the C# backend for the HIGH and MEDIUM duplication areas only. The goal is to reduce near-clone controllers, queries, commands, and helper classes while preserving existing API routes, response DTOs, tenant isolation, and current migration-scope behavior. The recommended sequencing is characterization-first, then six independently shippable workstreams: organizer CRUD, meal plan helpers, shopping list helpers, food/unit CRUD, parser provider wiring, and tenant self-resource controllers.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core MVC, MediatR-style command/query execution, EF Core, `IHttpClientFactory`, `ITenantContext`  
**Storage**: SQLite/PostgreSQL through `ApplicationDbContext`  
**Testing**: xUnit unit/integration suites under `backend-dotnet/tests/`, API regression tests, tenant-isolation checks, targeted characterization tests  
**Target Platform**: Linux-hosted ASP.NET Core backend in the existing Mealie container/runtime model  
**Project Type**: Multi-project web service (`Mealie.Api`, `Mealie.Application`, `Mealie.Domain`, `Mealie.Infrastructure`)  
**Performance Goals**: No regression on CRUD latency; reduce repeated query/mapping work in meal plan and shopping list flows; keep bulk operations independently verifiable  
**Constraints**: Preserve route/DTO/status-code compatibility, preserve group/household isolation, avoid schema changes, keep each refactor incremental and independently testable, exclude low-priority cleanup  
**Scale/Scope**: 6 workstreams touching organizer, meal plan, shopping list, ingredient, parser, and tenant controller layers in `backend-dotnet/src/`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still a placeholder template, so there are no project-specific constitutional rules to enforce beyond the active migration spec. Derived gates for this plan:

1. **API compatibility gate** — pass only if refactors keep current routes, request/response DTOs, and not-found behavior compatible with the migration spec.
2. **Tenant isolation gate** — pass only if group/household scoping remains explicit in every extracted abstraction and 404 masking behavior is preserved.
3. **Incremental delivery gate** — pass only if each workstream can be validated and merged independently without requiring a single large rewrite.
4. **Behavior safety gate** — pass only if characterization tests are added or updated before consolidating duplicated logic.

**Initial assessment**: PASS for planning. No unavoidable gate violations identified.  
**Post-design assessment**: PASS. The proposed workstreams preserve compatibility by extracting shared internals rather than changing public contracts.

## Project Structure

### Documentation (this feature)

```text
specs/001-csharp-backend-migration/
├── plan.md              # Consolidation/refactor implementation plan
├── research.md          # Decisions and rationale for scoped refactors
├── data-model.md        # Refactor design model and abstraction boundaries
├── quickstart.md        # Suggested implementation/validation workflow
├── contracts/           # Unchanged; no external contract changes planned for this refactor
└── tasks.md             # Existing implementation backlog for the broader migration
```

### Source Code (repository root)

```text
backend-dotnet/
├── src/
│   ├── Mealie.Api/
│   │   └── Controllers/
│   │       ├── Organizers/
│   │       ├── Ingredients/
│   │       ├── Groups/
│   │       └── Households/
│   ├── Mealie.Application/
│   │   ├── Commands/
│   │   │   ├── Organizers/
│   │   │   ├── MealPlans/
│   │   │   └── ShoppingLists/
│   │   ├── Queries/
│   │   │   ├── Organizers/
│   │   │   ├── MealPlans/
│   │   │   └── ShoppingLists/
│   │   └── Services/
│   │       ├── Ingredients/
│   │       └── Parser/
│   └── Mealie.Infrastructure/
└── tests/
    ├── Mealie.UnitTests/
    └── Mealie.IntegrationTests/
```

**Structure Decision**: Keep the current layered backend structure and confine refactor work to shared internal abstractions inside `Mealie.Application` and reusable controller patterns in `Mealie.Api`. No new project is needed; the main design choice is where to place shared behavior so the public API surface remains unchanged.

## Scope Guardrails

### In Scope

- **High priority**
  - Organizer CRUD consolidation for tags/categories/tools
  - Meal plan helper consolidation with mapping split from recipe-selection logic
  - Shopping list helper consolidation with mapping split from item creation/merge/update logic
- **Medium priority**
  - Food/unit CRUD consolidation
  - Parser provider consolidation around OpenAI-compatible client construction
  - Group/household tenant self-resource controller consolidation

### Out of Scope

- Any low-priority duplication cleanup
- Broad API redesigns or route renames
- Database schema changes
- Frontend changes
- Cross-cutting rewrites outside the named files and their directly supporting tests

## Workstreams

### WS1 — Organizer CRUD Consolidation (HIGH)

**Problem**  
`TagsController`, `CategoriesController`, and `ToolsController` are near-clones, and their related organizer queries/commands repeat the same list/get/create/update/delete behavior. Create commands enforce unique slugs per group, while update commands currently regenerate slugs directly without the same uniqueness handling.

**Approach**

- Introduce a shared organizer CRUD abstraction for the tag/category/tool family.
- Centralize slug generation and uniqueness enforcement into one policy/helper used by both create and update paths.
- Keep entity-specific DTOs and route prefixes intact; share only controller flow and command/query internals.
- Preserve `CreatedAtAction`, 404 masking, and current per-group scoping semantics.

**Expected outputs**

- Shared organizer slug policy and organizer CRUD execution path
- Reduced duplicate controllers/commands/queries
- Explicit update-path uniqueness handling
- Characterization coverage for list/get/create/update/delete/recipes/empty endpoints

**Key risks**

- Accidentally normalizing away small route differences (`empty`, tool-specific request DTOs)
- Changing slug-collision behavior during updates
- Leaking cross-group data if genericization hides the group filter

### WS2 — Meal Plan Helper Consolidation (HIGH)

**Problem**  
`MealPlanHelpers` is duplicated across create/update/fill/query handlers. Mapping logic and random recipe-selection logic are currently intertwined, which makes reuse risky and obscures performance-sensitive behavior.

**Approach**

- Split the duplicated helper into:
  - a **meal plan mapping/loading helper** for response shaping and navigation loading
  - a **recipe selection service** for random/fill logic and rule evaluation
- Keep command/query entry points thin and explicit.
- Preserve ordering, date-window behavior, and current tenant-scoped selection rules.
- Validate bulk operations independently from single-entry CRUD.

**Expected outputs**

- One shared mapping path for meal plan DTO hydration
- One shared recipe-selection path for random/fill flows
- Cleaner command/query handlers with fewer inline helper copies

**Key risks**

- Reordering meal-plan generation behavior
- Performance regressions in fill-day/fill-week flows
- Subtle changes in rule fallback behavior

### WS3 — Shopping List Helper Consolidation (HIGH)

**Problem**  
`ShoppingListMappings` is duplicated across list creation, item add/update flows, standalone item flows, and queries. Mapping concerns are mixed with item creation, merge, and update behavior.

**Approach**

- Split duplicated shopping-list logic into:
  - **list/item mapping helpers**
  - **item mutation helpers** for create/merge/update behavior
  - **recipe-link helpers** where item creation is driven by recipe ingredients
- Preserve current merge semantics, household scoping, and list/item response shapes.
- Stage bulk-item behavior behind explicit characterization coverage before refactoring.

**Expected outputs**

- Shared mapping path for shopping list and item DTOs
- Shared mutation path for add/update/standalone flows
- Clearer separation between query projection and mutation behavior

**Key risks**

- Changing merge precedence or standalone-item semantics
- Breaking recipe-linked item update behavior
- Accidentally changing list-level timestamps or update ordering

### WS4 — Food/Unit CRUD Consolidation (MEDIUM)

**Problem**  
`FoodService` and `UnitService`, plus `FoodsController` and `UnitsController`, are highly parallel. Shared concerns include paginated list handling, alias mutation, merge behavior, and response mapping.

**Approach**

- Reuse the organizer consolidation pattern to extract a shared ingredient-entity CRUD core.
- Keep entity-specific hooks where behavior diverges (for example food label mapping or current event publication differences).
- Consolidate alias replacement, merge flow, and response mapping behind a shared internal service pattern rather than forcing a public generic API.

**Expected outputs**

- Shared CRUD core for foods and units
- Shared alias mutation and merge workflow
- Explicit decision on whether event publication asymmetry stays intentional or is standardized

**Key risks**

- Over-generalizing away food-specific label handling
- Changing merge side effects
- Missing tenant filter application during shared list/query code

### WS5 — Parser Provider Consolidation (MEDIUM)

**Problem**  
`OpenAiParserStrategy`, `AzureOpenAiParserStrategy`, `OllamaParserStrategy`, and `CustomAiParserStrategy` mostly differ in how they construct/configure `HttpClient`, while `OpenAiCompatibleParserStrategy` already owns the core behavior. This duplication also creates an opportunity to clean up constructor-capture warnings around provider-specific setup.

**Approach**

- Keep `OpenAiCompatibleParserStrategy` as the core request/response behavior.
- Introduce a config-driven provider client builder/factory that owns base URL, auth/header, and named-client configuration.
- Convert provider strategies into thin provider descriptors/adapters around the shared builder.
- Treat constructor-capture warning cleanup as a secondary benefit, not the primary goal.

**Expected outputs**

- Shared provider client factory/builder
- Smaller provider-specific strategy classes
- Cleaner DI story for provider-specific client setup

**Key risks**

- Provider-specific header/auth regressions
- Breaking non-OpenAI providers while standardizing the builder
- Hiding provider behavior that still deserves explicit tests

### WS6 — Tenant Self-Resource Controller Consolidation (MEDIUM)

**Problem**  
`GroupsController` and `HouseholdsController` repeat self/get/update/preferences/members/invitations patterns, including repeated not-found handling and route alias behavior.

**Approach**

- Extract a shared self-resource controller pattern or helper layer for:
  - `GET self`
  - `PUT self`
  - preferences read/update
  - members/invitations list flows
- Preserve current route aliases (`members`, `self/members`, etc.) and 404 masking behavior.
- Keep non-shared actions (migration/report endpoints, email flows, permissions) entity-specific.

**Expected outputs**

- Reduced duplicate self-resource controller flow
- Shared not-found and tenant-context handling
- Explicit preservation of current route aliases

**Key risks**

- Accidentally collapsing distinct group vs household behavior
- Breaking mixed route alias coverage
- Introducing a base controller abstraction that is harder to test than the current actions

## Sequencing and Dependencies

### Phase A — Refactor Guardrails

1. Add/update characterization tests for the six scoped areas.
2. Capture route, DTO, and tenant-isolation invariants before moving shared code.
3. Identify one preferred pattern for shared controller logic vs shared application-layer logic.

### Phase B — High-Priority Delivery

1. **WS1 Organizer CRUD**  
   Lowest-risk high-priority extraction and best template for later CRUD consolidation.
2. **WS2 Meal Plan Helpers**  
   Independent from organizer CRUD, but should reuse the same characterization-first discipline.
3. **WS3 Shopping List Helpers**  
   Follows after meal-plan helper extraction patterns are established for mapping vs mutation splits.

### Phase C — Medium-Priority Delivery

1. **WS4 Food/Unit CRUD**  
   Reuse patterns proven in organizer CRUD consolidation.
2. **WS5 Parser Provider Consolidation**  
   Can run in parallel with WS4 once the client-builder design is agreed.
3. **WS6 Tenant Self-Resource Controllers**  
   Best done after controller-sharing lessons from organizer CRUD are available.

## Validation Strategy

### Required validation for every workstream

- Build and targeted test pass for affected backend projects
- Integration coverage for route/response compatibility
- Explicit tenant-isolation checks (wrong group/household still yields 404)
- No OpenAPI or JSON-shape drift for touched endpoints

### Additional validation by workstream

- **Organizer CRUD**: slug collision tests on both create and update
- **Meal plan helpers**: random/fill behavior snapshots, date-range parity, bulk operation coverage
- **Shopping list helpers**: merge/update parity, standalone item parity, recipe-linked item coverage
- **Food/unit CRUD**: alias replacement, merge reassignment, label/response parity
- **Parser providers**: provider matrix tests for headers/base URLs/named clients
- **Tenant self-resource**: route alias coverage for `self/*` and legacy shortcut paths

## Notable Risks and Mitigations

| Risk | Affected Workstreams | Mitigation |
|------|----------------------|------------|
| Shared abstraction changes visible API behavior | WS1, WS4, WS6 | Characterization tests first; keep public controllers/routes stable |
| Slug uniqueness remains inconsistent after consolidation | WS1 | Centralize slug policy and require create/update callers to use it |
| Refactor mixes mapping with domain behavior again | WS2, WS3 | Separate mapping helpers from selection/mutation helpers in the design model |
| Generic CRUD base becomes too rigid | WS1, WS4 | Prefer internal shared components with entity-specific hooks over a single rigid generic surface |
| Provider factory hides auth/header differences | WS5 | Keep provider-specific config tests and explicit provider descriptors |
| Tenant aliases or 404 masking regress | WS1, WS4, WS6 | Preserve current controller action signatures and assert not-found behavior in integration tests |

## Complexity Tracking

No constitution violations requiring exception tracking at plan time.
