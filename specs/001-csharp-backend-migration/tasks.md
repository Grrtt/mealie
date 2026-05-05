# Tasks: C# Backend Consolidation Refactors

**Input**: Design documents from `/specs/001-csharp-backend-migration/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/api-contract.md`  
**Tests**: Characterization-first validation is required for every scoped workstream in this backlog.  
**Contracts**: `specs/001-csharp-backend-migration/contracts/api-contract.md` remains unchanged for this scope; no external contract change tasks are planned.  
**Organization**: Tasks are grouped by the six scoped consolidation workstreams so each can land as a separate PR slice where practical.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Maps to one of the six scoped workstreams (`[US1]` ... `[US6]`)
- Every task includes an exact file path and preserves API compatibility, tenant isolation, behavior-safe incremental delivery, and characterization-first validation

## Phase 1: Setup (Scoped Refactor Prerequisites)

**Purpose**: Establish only the shared validation harness needed for the six scoped refactors.

- [ ] T001 Create a shared scoped-refactor test host in `backend-dotnet/tests/Mealie.IntegrationTests/ScopedRefactors/ScopedRefactorTestFactory.cs`
- [ ] T002 Create shared parity and tenant-isolation assertions in `backend-dotnet/tests/Mealie.IntegrationTests/ScopedRefactors/ScopedRefactorAssertions.cs`

---

## Phase 2: Foundational (Blocking Guardrails)

**Purpose**: Add the cross-workstream safety net that blocks all six refactors until it is in place.

**⚠️ CRITICAL**: No workstream implementation should begin until this phase is complete.

- [ ] T003 [P] Add unchanged-contract regression coverage for the scoped endpoints in `backend-dotnet/tests/Mealie.IntegrationTests/ScopedRefactors/ScopedOpenApiParityTests.cs`
- [ ] T004 [P] Add reusable tenant-safe seed/build helpers for organizer, meal plan, shopping list, ingredient, parser, group, and household scenarios in `backend-dotnet/tests/Mealie.IntegrationTests/ScopedRefactors/ScopedRefactorDataBuilder.cs`

**Checkpoint**: Scoped characterization harness is ready; all six workstreams can now proceed with behavior-safe slices.

---

## Phase 3: User Story 1 - Organizer CRUD Consolidation (Priority: P1) 🎯 MVP

**Goal**: Consolidate tag/category/tool internals while preserving routes, DTOs, `CreatedAtAction`, slug behavior, and group-scoped 404 masking.

**Independent Test**: Run organizer characterization coverage and confirm tags, categories, and tools still preserve CRUD, `recipes`, `empty`, slug lookup, and cross-group 404 behavior with no response-shape drift.

### Tests for User Story 1 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [ ] T005 [US1] Add characterization coverage for organizer CRUD parity, slug collisions, `recipes`, `empty`, and 404 masking in `backend-dotnet/tests/Mealie.IntegrationTests/Organizers/OrganizerCrudIntegrationTests.cs`

### Implementation for User Story 1

- [X] T006 [P] [US1] Create shared organizer slug normalization and uniqueness enforcement in `backend-dotnet/src/Mealie.Application/Services/Organizers/OrganizerSlugPolicy.cs`
- [X] T007 [P] [US1] Create shared organizer CRUD execution internals in `backend-dotnet/src/Mealie.Application/Services/Organizers/OrganizerCrudModule.cs`
- [X] T008 [US1] Refactor organizer create/update/delete commands to use the shared slug policy and CRUD module in `backend-dotnet/src/Mealie.Application/Commands/Organizers/CreateTagCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/CreateCategoryCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/CreateToolCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/UpdateTagCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/UpdateCategoryCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/UpdateToolCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/DeleteTagCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Organizers/DeleteCategoryCommand.cs`, and `backend-dotnet/src/Mealie.Application/Commands/Organizers/DeleteToolCommand.cs`
- [ ] T009 [US1] Refactor organizer queries and controllers to use shared organizer internals without changing routes or DTOs in `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetTagsQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetCategoriesQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetToolsQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetTagBySlugQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetCategoryBySlugQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetToolBySlugQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetRecipesByTagQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetRecipesByCategoryQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetRecipesByToolQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetEmptyTagsQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Organizers/GetEmptyCategoriesQuery.cs`, `backend-dotnet/src/Mealie.Api/Controllers/Organizers/TagsController.cs`, `backend-dotnet/src/Mealie.Api/Controllers/Organizers/CategoriesController.cs`, and `backend-dotnet/src/Mealie.Api/Controllers/Organizers/ToolsController.cs`

**Checkpoint**: Organizer CRUD consolidation is shippable as the first PR slice and validates the shared CRUD pattern for later work.

---

## Phase 4: User Story 2 - Meal Plan Helper Consolidation (Priority: P1)

**Goal**: Split meal plan mapping/loading from recipe-selection logic while preserving date-range behavior, ordering, random/fill behavior, and tenant scoping.

**Independent Test**: Run meal plan characterization coverage and confirm create, update, random, fill-day, fill-week, get-by-id, and today flows still match existing response shapes and 404 behavior.

### Tests for User Story 2 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [ ] T010 [US2] Expand characterization coverage for meal plan create/update/query/random/fill parity in `backend-dotnet/tests/Mealie.IntegrationTests/Households/MealPlanIntegrationTests.cs`

### Implementation for User Story 2

- [X] T011 [P] [US2] Create shared meal plan response mapping and navigation loading logic in `backend-dotnet/src/Mealie.Application/Services/MealPlans/MealPlanMappingHelper.cs`
- [X] T012 [P] [US2] Create shared meal plan recipe-selection logic for random and fill flows in `backend-dotnet/src/Mealie.Application/Services/MealPlans/MealPlanRecipeSelectionService.cs`
- [X] T013 [US2] Refactor meal plan commands to consume the split helper services in `backend-dotnet/src/Mealie.Application/Commands/MealPlans/CreateMealPlanCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/MealPlans/UpdateMealPlanCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/MealPlans/CreateRandomMealPlanCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/MealPlans/FillDayCommand.cs`, and `backend-dotnet/src/Mealie.Application/Commands/MealPlans/FillWeekCommand.cs`
- [ ] T014 [US2] Refactor meal plan queries and controller flow to use shared mapping without contract drift in `backend-dotnet/src/Mealie.Application/Queries/MealPlans/GetMealPlansQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/MealPlans/GetMealPlanByIdQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/MealPlans/GetTodayMealPlansQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/MealPlans/GetRandomRecipeIdQuery.cs`, and `backend-dotnet/src/Mealie.Api/Controllers/Households/MealPlansController.cs`

**Checkpoint**: Meal plan helper consolidation is independently releasable with characterization coverage protecting mapping and recipe-selection parity.

---

## Phase 5: User Story 3 - Shopping List Helper Consolidation (Priority: P1)

**Goal**: Split shopping list mapping, item mutation/merge behavior, and recipe-link behavior while preserving household scoping and response parity.

**Independent Test**: Run shopping list characterization coverage and confirm list, item, bulk, standalone, and recipe-linked flows still preserve current merge semantics, timestamps, and 404 masking.

### Tests for User Story 3 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [ ] T015 [US3] Add characterization coverage for shopping list list/item/bulk/recipe-link parity in `backend-dotnet/tests/Mealie.IntegrationTests/Households/ShoppingListIntegrationTests.cs`

### Implementation for User Story 3

- [X] T016 [P] [US3] Create shared shopping list and item DTO projection helpers in `backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListMappingHelper.cs`
- [X] T017 [P] [US3] Create shared shopping list item mutation and merge behavior in `backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListItemMutationService.cs`
- [X] T018 [P] [US3] Create shared recipe-to-shopping-list linking behavior in `backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListRecipeLinkService.cs`
- [X] T019 [US3] Refactor shopping list commands and queries to use the shared mapping, mutation, and recipe-link helpers in `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/CreateShoppingListCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/CreateShoppingListWithRecipeCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/AddRecipeToShoppingListCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/AddShoppingListItemCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/UpdateShoppingListItemCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/CreateStandaloneItemCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/UpdateStandaloneItemCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/CreateBulkShoppingListItemsCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/UpdateBulkShoppingListItemsCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/UpdateShoppingListCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/ShoppingLists/UpdateShoppingListLabelSettingsCommand.cs`, `backend-dotnet/src/Mealie.Application/Queries/ShoppingLists/GetShoppingListsQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/ShoppingLists/GetShoppingListByIdQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/ShoppingLists/GetShoppingListItemsQuery.cs`, and `backend-dotnet/src/Mealie.Application/Queries/ShoppingLists/GetShoppingListItemByIdQuery.cs`
- [ ] T020 [US3] Refactor shopping list controllers to preserve current routes and response shapes in `backend-dotnet/src/Mealie.Api/Controllers/Households/ShoppingListsController.cs` and `backend-dotnet/src/Mealie.Api/Controllers/Households/ShoppingItemsController.cs`

**Checkpoint**: Shopping list helper consolidation is independently releasable with merge-safe and recipe-link characterization coverage.

---

## Phase 6: User Story 4 - Food/Unit CRUD Consolidation (Priority: P2)

**Goal**: Reuse the organizer-style consolidation pattern for foods and units while preserving paginated list behavior, alias replacement, merge semantics, and group isolation.

**Independent Test**: Run food/unit characterization coverage and confirm paginated CRUD, patch parity, merge behavior, alias updates, and cross-group 404 masking still match the existing implementation.

### Tests for User Story 4 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [ ] T021 [US4] Add characterization coverage for food/unit CRUD, alias replacement, merge behavior, and group-scoped 404 masking in `backend-dotnet/tests/Mealie.IntegrationTests/Ingredients/FoodUnitCrudIntegrationTests.cs`

### Implementation for User Story 4

- [X] T022 [P] [US4] Create a shared ingredient CRUD core with explicit entity hooks in `backend-dotnet/src/Mealie.Application/Services/Ingredients/IngredientCrudCore.cs`
- [X] T023 [US4] Refactor ingredient services to use the shared CRUD core in `backend-dotnet/src/Mealie.Application/Services/Ingredients/FoodService.cs` and `backend-dotnet/src/Mealie.Application/Services/Ingredients/UnitService.cs`
- [X] T024 [US4] Refactor food/unit commands and queries to use the shared CRUD core in `backend-dotnet/src/Mealie.Application/Commands/Ingredients/CreateFoodCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/UpdateFoodCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/DeleteFoodCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/MergeFoodCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/CreateUnitCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/UpdateUnitCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/DeleteUnitCommand.cs`, `backend-dotnet/src/Mealie.Application/Commands/Ingredients/MergeUnitCommand.cs`, `backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetFoodsQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetFoodByIdQuery.cs`, `backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetUnitsQuery.cs`, and `backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetUnitByIdQuery.cs`
- [ ] T025 [US4] Refactor ingredient controllers to preserve paginated and merge responses in `backend-dotnet/src/Mealie.Api/Controllers/Ingredients/FoodsController.cs` and `backend-dotnet/src/Mealie.Api/Controllers/Ingredients/UnitsController.cs`

**Checkpoint**: Food/unit CRUD consolidation is independently releasable and reuses the safest organizer-style shared CRUD pattern.

---

## Phase 7: User Story 5 - Parser Provider Consolidation (Priority: P2)

**Goal**: Centralize provider-specific `HttpClient` construction for OpenAI-compatible parser strategies without changing parser request or response contracts.

**Independent Test**: Run parser provider matrix coverage and confirm each provider still applies the correct base URL, auth/header behavior, named client configuration, and request payload shape.

### Tests for User Story 5 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [X] T026 [US5] Add provider matrix characterization coverage for parser client configuration in `backend-dotnet/tests/Mealie.UnitTests/Parser/ParserClientFactoryTests.cs`

### Implementation for User Story 5

- [X] T027 [P] [US5] Create provider-specific parser configuration descriptors in `backend-dotnet/src/Mealie.Application/Services/Parser/ParserProviderDescriptor.cs`
- [X] T028 [P] [US5] Create a shared provider-aware parser client builder in `backend-dotnet/src/Mealie.Application/Services/Parser/ParserClientFactory.cs`
- [X] T029 [US5] Refactor OpenAI-compatible provider strategies to use the shared client builder in `backend-dotnet/src/Mealie.Application/Services/Parser/OpenAiCompatibleParserStrategy.cs`, `backend-dotnet/src/Mealie.Application/Services/Parser/OpenAiParserStrategy.cs`, `backend-dotnet/src/Mealie.Application/Services/Parser/AzureOpenAiParserStrategy.cs`, `backend-dotnet/src/Mealie.Application/Services/Parser/OllamaParserStrategy.cs`, and `backend-dotnet/src/Mealie.Application/Services/Parser/CustomAiParserStrategy.cs`
- [X] T030 [US5] Refactor parser strategy resolution to construct provider descriptors without external contract changes in `backend-dotnet/src/Mealie.Application/Services/Parser/ParserStrategyResolver.cs`

**Checkpoint**: Parser provider consolidation is independently releasable and can be reviewed as a narrow infrastructure-focused PR slice.

---

## Phase 8: User Story 6 - Tenant Self-Resource Controller Consolidation (Priority: P2)

**Goal**: Consolidate group and household self-resource controller flow while preserving route aliases, 404 masking, and controller-specific non-shared endpoints.

**Independent Test**: Run tenant self-resource characterization coverage and confirm self, preferences, members, invitations, and alias routes still behave the same for both groups and households.

### Tests for User Story 6 ⚠️

> Write these tests first and confirm they fail before refactoring shared internals.

- [ ] T031 [US6] Add characterization coverage for group/household self-resource aliases and 404 masking in `backend-dotnet/tests/Mealie.IntegrationTests/Groups/TenantSelfResourceIntegrationTests.cs`

### Implementation for User Story 6

- [X] T032 [US6] Create a shared tenant self-resource controller helper in `backend-dotnet/src/Mealie.Api/Controllers/Shared/SelfResourceControllerHelper.cs`
- [X] T033 [US6] Refactor shared group self-resource actions to use the helper while keeping migration/report endpoints local in `backend-dotnet/src/Mealie.Api/Controllers/Groups/GroupsController.cs`
- [X] T034 [US6] Refactor shared household self-resource actions to use the helper while keeping statistics, recipe, email, and permissions endpoints local in `backend-dotnet/src/Mealie.Api/Controllers/Households/HouseholdsController.cs`

**Checkpoint**: Tenant self-resource consolidation is independently releasable and preserves the current controller surface without broad controller rewrites.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across the six scoped PR slices only.

- [ ] T035 Verify the scoped no-contract-change guardrail against the targeted endpoints in `backend-dotnet/tests/Mealie.IntegrationTests/ScopedRefactors/ScopedOpenApiParityTests.cs`
- [ ] T036 Run the full scoped build and regression gate from `backend-dotnet/Mealie.sln` before merging each workstream PR slice

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Start immediately.
- **Foundational (Phase 2)**: Depends on T001-T002 and blocks every workstream until complete.
- **User Stories (Phases 3-8)**: Depend on T001-T004.
- **Polish (Phase 9)**: Depends on the workstreams selected for the current delivery train.

### User Story Dependencies

- **US1 — Organizer CRUD**: Starts immediately after T001-T004; preferred first PR because it proves the shared CRUD pattern.
- **US2 — Meal plan helpers**: Starts after T001-T004; independent of US1 for delivery, but follows the same characterization-first discipline.
- **US3 — Shopping list helpers**: Starts after T001-T004; preferred after US2 because both rely on a mapping-vs-behavior split pattern.
- **US4 — Food/unit CRUD**: Starts after T001-T004; preferred after US1 because it reuses the shared CRUD lessons from organizer consolidation.
- **US5 — Parser providers**: Starts after T001-T004; can proceed in parallel with US4 because contracts stay unchanged and the refactor is isolated to parser services.
- **US6 — Tenant self-resource controllers**: Starts after T001-T004; preferred after US1 because controller-sharing lessons should be proven first.

### Within Each User Story

- Characterization tests MUST be written and failing before implementation starts.
- Shared helper/descriptor/core files come before command/query/controller rewiring.
- Controller changes must preserve route paths, alias behavior, DTOs, status codes, and tenant-safe 404 masking.
- Each workstream should stop at its checkpoint and be validated before starting the next preferred dependency.

### Parallel Opportunities

- T003 and T004 can run in parallel after T001-T002.
- US1: T006 and T007 can run in parallel after T005.
- US2: T011 and T012 can run in parallel after T010.
- US3: T016, T017, and T018 can run in parallel after T015.
- US4: T023 and T024 can run in parallel after T021 and T022 are complete.
- US5: T027 and T028 can run in parallel after T026.
- Cross-story: US2 and US5 can run in parallel after T001-T004; US4 and US5 can also overlap once the foundational guardrails are green.

---

## Parallel Example: User Story 1

```bash
Task: "T006 Create shared organizer slug normalization and uniqueness enforcement in backend-dotnet/src/Mealie.Application/Services/Organizers/OrganizerSlugPolicy.cs"
Task: "T007 Create shared organizer CRUD execution internals in backend-dotnet/src/Mealie.Application/Services/Organizers/OrganizerCrudModule.cs"
```

## Parallel Example: User Story 2

```bash
Task: "T011 Create shared meal plan response mapping and navigation loading logic in backend-dotnet/src/Mealie.Application/Services/MealPlans/MealPlanMappingHelper.cs"
Task: "T012 Create shared meal plan recipe-selection logic for random and fill flows in backend-dotnet/src/Mealie.Application/Services/MealPlans/MealPlanRecipeSelectionService.cs"
```

## Parallel Example: User Story 3

```bash
Task: "T016 Create shared shopping list and item DTO projection helpers in backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListMappingHelper.cs"
Task: "T017 Create shared shopping list item mutation and merge behavior in backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListItemMutationService.cs"
Task: "T018 Create shared recipe-to-shopping-list linking behavior in backend-dotnet/src/Mealie.Application/Services/ShoppingLists/ShoppingListRecipeLinkService.cs"
```

## Parallel Example: User Story 4

```bash
# After T021 and T022 complete:
Task: "T023 Refactor ingredient services to use the shared CRUD core in backend-dotnet/src/Mealie.Application/Services/Ingredients/FoodService.cs and backend-dotnet/src/Mealie.Application/Services/Ingredients/UnitService.cs"
Task: "T024 Refactor food/unit commands and queries to use the shared CRUD core in backend-dotnet/src/Mealie.Application/Commands/Ingredients/CreateFoodCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/UpdateFoodCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/DeleteFoodCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/MergeFoodCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/CreateUnitCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/UpdateUnitCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/DeleteUnitCommand.cs, backend-dotnet/src/Mealie.Application/Commands/Ingredients/MergeUnitCommand.cs, backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetFoodsQuery.cs, backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetFoodByIdQuery.cs, backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetUnitsQuery.cs, and backend-dotnet/src/Mealie.Application/Queries/Ingredients/GetUnitByIdQuery.cs"
```

## Parallel Example: User Story 5

```bash
Task: "T027 Create provider-specific parser configuration descriptors in backend-dotnet/src/Mealie.Application/Services/Parser/ParserProviderDescriptor.cs"
Task: "T028 Create a shared provider-aware parser client builder in backend-dotnet/src/Mealie.Application/Services/Parser/ParserClientFactory.cs"
```

## Parallel Example: User Story 6

```bash
# No safe parallel implementation tasks are planned for US6 until T031 characterization coverage and T032 helper extraction are complete.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001-T004.
2. Deliver US1 (T005-T009) as the first, lowest-risk PR slice.
3. Run T035-T036 for the organizer slice before merge.
4. Stop and validate that organizer routes, DTOs, slug behavior, and 404 masking remain unchanged.

### Incremental Delivery

1. **PR Slice 1**: Setup + Foundational + US1.
2. **PR Slice 2**: US2 meal plan helper consolidation.
3. **PR Slice 3**: US3 shopping list helper consolidation.
4. **PR Slice 4**: US4 food/unit CRUD consolidation.
5. **PR Slice 5**: US5 parser provider consolidation.
6. **PR Slice 6**: US6 tenant self-resource controller consolidation.
7. Run T035-T036 after every slice to keep compatibility and tenant isolation continuously verified.

### Parallel Team Strategy

With multiple engineers after T001-T004:

1. Engineer A: US1, then US4.
2. Engineer B: US2, then US3.
3. Engineer C: US5 in parallel with US4, then US6 after the controller-sharing pattern is proven.

This keeps changes small, behavior-safe, and reviewable without reopening the broader migration backlog.

---

## Notes

- Contracts stay unchanged for this scope; do not add OpenAPI or external API redesign tasks.
- Exclude low-priority cleanup and unrelated migration backlog items from these slices.
- Prefer separate PRs per workstream unless two adjacent tasks are required to keep characterization coverage passing.
- Preserve explicit tenant IDs and 404 masking in every extracted abstraction.
