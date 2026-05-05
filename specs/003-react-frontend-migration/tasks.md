# Tasks: React Frontend Migration Parity

**Feature**: `003-react-frontend-migration`  
**Input**: Design documents from `/home/grrtt/dev/mealie/specs/003-react-frontend-migration/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/route-parity.yaml`, `contracts/release-gates.yaml`, `contracts/runtime-invariants.md`  
**Tech stack**: TypeScript 5.x, React, Vite, TanStack Router, TanStack Query, React Hook Form, Zod, react-i18next, Material UI, Playwright, Vitest  
**Tests**: Included — parity validation, route smoke coverage, locale/RTL checks, coexistence checks, and rollback drills are explicitly required by the feature specification.  
**Organization**: Tasks are grouped by user story so each migration slice can be implemented, validated, and promoted independently.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel with other `[P]` tasks in the same phase (different files, no dependency on unfinished work)
- **[Story]**: Maps to a user story from `spec.md` (`[US1]` ... `[US5]`)
- Every task includes exact file paths and is written for direct execution by the implementation workflow

---

## Phase 1: Setup (Inventory, Baseline, and Workspace Bootstrap)

**Purpose**: Freeze the legacy inventory, capture release gates, and create the parallel React workspace before shared runtime work begins.

- [X] T001 Create the parallel React workspace manifest and bootstrap entry files in `frontend-react/package.json`, `frontend-react/tsconfig.json`, `frontend-react/vite.config.ts`, `frontend-react/index.html`, and `frontend-react/src/main.tsx`
- [X] T002 Add React migration development/build tasks and dual-frontend operator entry points in `Taskfile.yml`
- [X] T003 [P] Update the route/workflow inventory, surface ownership, and migration outcomes in `specs/003-react-frontend-migration/contracts/route-parity.yaml`
- [X] T004 [P] Update phase gates, required evidence, and runtime guardrails for implementation in `specs/003-react-frontend-migration/contracts/release-gates.yaml` and `specs/003-react-frontend-migration/contracts/runtime-invariants.md`

**Checkpoint**: The React workspace exists, operator entry points are defined, and the inventory/gate contracts are current enough to drive implementation.

---

## Phase 2: Foundational (Blocking Runtime Prerequisites)

**Purpose**: Build the shared runtime, deployment plumbing, and validation harness that every migrated surface depends on.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

- [X] T005 Create the typed React app entry, root route, and route-tree bootstrap in `frontend-react/src/main.tsx`, `frontend-react/src/router.tsx`, and `frontend-react/src/routes/__root.tsx`
- [X] T006 [P] Implement the shared backend API client, query client, and contract adapters in `frontend-react/src/lib/api/client.ts`, `frontend-react/src/lib/api/contracts.ts`, and `frontend-react/src/lib/query/queryClient.ts`
- [X] T007 [P] Implement shared auth/session hydration, logout, 401 recovery, and intended-destination persistence in `frontend-react/src/features/auth/session.ts`, `frontend-react/src/features/auth/routeGuards.ts`, and `frontend-react/src/features/auth/redirectStore.ts`
- [X] T008 [P] Implement locale loading, persisted locale preference, date/time formatting, and `Accept-Language` propagation in `frontend-react/src/lib/i18n/i18n.ts`, `frontend-react/src/lib/i18n/locales.ts`, `frontend-react/src/lib/i18n/dateTime.ts`, and `frontend-react/src/lib/i18n/persistedLocale.ts`
- [X] T009 [P] Implement Material UI theming, RTL cache support, and shared shell/navigation primitives in `frontend-react/src/theme/index.ts`, `frontend-react/src/theme/rtlCache.ts`, `frontend-react/src/components/layout/AppShell.tsx`, and `frontend-react/src/components/navigation/MainNav.tsx`
- [X] T010 [P] Implement shared form, validation, and route error-boundary primitives in `frontend-react/src/lib/forms/useZodForm.ts`, `frontend-react/src/lib/validation/index.ts`, `frontend-react/src/components/forms/`, and `frontend-react/src/components/errors/RouteErrorBoundary.tsx`
- [X] T011 [P] Configure the React Vitest and Playwright parity harnesses in `frontend-react/vitest.config.ts`, `frontend-react/src/test/setup.ts`, `tests/e2e/playwright.config.ts`, and `tests/e2e/react-migration/fixtures.ts`
- [X] T012 Implement static SPA serving, nginx `try_files`, and `SUB_PATH` build plumbing for the React workspace in `frontend-react/Dockerfile`, `frontend-react/nginx.conf`, `frontend-react/vite.config.ts`, and `docker-compose.dotnet.yml`

**Checkpoint**: The replacement SPA can boot under the existing deployment assumptions, shares the backend contract safely, and has the common infrastructure needed for all user stories.

---

## Phase 3: User Story 1 - Secure Access and Navigation Parity (Priority: P1) 🎯 MVP

**Goal**: Users can authenticate, recover access, keep session continuity, and navigate to the same destinations they use today without broken redirects or unexpected sign-outs.

**Independent Test**: Sign in through every supported auth path, open protected and bookmarked deep links, refresh the browser, expire the session once, and confirm users always land on the intended working destination.

### Tests for User Story 1 ⚠️

> Write these tests first and confirm they fail before implementing the React access/navigation slice.

- [X] T013 [P] [US1] Add auth entry-point and session continuity coverage in `tests/e2e/react-migration/auth-parity.spec.ts`
- [X] T014 [P] [US1] Add root redirect, protected deep-link, and post-login return coverage in `tests/e2e/react-migration/navigation-parity.spec.ts`

### Implementation for User Story 1

- [X] T015 [US1] Implement the login, register, forgot-password, and reset-password routes in `frontend-react/src/routes/login.tsx`, `frontend-react/src/routes/register/index.tsx`, `frontend-react/src/routes/forgot-password.tsx`, and `frontend-react/src/routes/reset-password.tsx`
- [X] T016 [US1] Implement auth-aware `/` redirect behavior, public/protected/admin route guards, and intended-destination restoration in `frontend-react/src/routes/index.tsx`, `frontend-react/src/features/auth/routeGuards.ts`, and `frontend-react/src/features/auth/defaultLanding.ts`
- [X] T017 [P] [US1] Implement the authenticated navigation shell and group landing route parity in `frontend-react/src/components/layout/AppShell.tsx`, `frontend-react/src/components/navigation/MainNav.tsx`, and `frontend-react/src/routes/g/$groupSlug/index.tsx`
- [X] T018 [US1] Implement session hydration, logout, session refresh, and cross-surface cookie continuity in `frontend-react/src/features/auth/session.ts`, `frontend-react/src/features/auth/useCurrentUser.ts`, and `frontend-react/src/components/auth/LogoutButton.tsx`
- [X] T019 [US1] Implement route-level titles and meaningful metadata for auth and landing routes in `frontend-react/src/routes/login.tsx`, `frontend-react/src/routes/forgot-password.tsx`, `frontend-react/src/routes/register/index.tsx`, `frontend-react/src/routes/reset-password.tsx`, and `frontend-react/src/routes/index.tsx`

**Checkpoint**: Access and navigation parity is independently testable and provides the MVP slice for the migration.

---

## Phase 4: User Story 2 - Recipe Workflows Reach Functional Parity (Priority: P1)

**Goal**: Users can browse, create, import, edit, share, and interact with recipes in the React frontend without losing public access or recipe-management depth.

**Independent Test**: Browse recipes, open deep links, create and import recipes from supported sources, edit recipe content and assets, add comments/ratings/favorites, and open a public shared recipe link from a cold browser start.

### Tests for User Story 2 ⚠️

> Write these tests first and confirm they fail before implementing the React recipe slice.

- [X] T020 [P] [US2] Add recipe browse, detail, edit, and import journey coverage in `tests/e2e/react-migration/recipe-workflows.spec.ts`
- [X] T021 [P] [US2] Add public/shared recipe and metadata parity coverage in `tests/e2e/react-migration/recipe-public.spec.ts`

### Implementation for User Story 2

- [X] T022 [US2] Implement recipe detail, timeline, and finder routes with TanStack Query data loading in `frontend-react/src/routes/g/$groupSlug/r/$slug/index.tsx`, `frontend-react/src/routes/g/$groupSlug/recipes/timeline.tsx`, `frontend-react/src/routes/g/$groupSlug/recipes/finder/index.tsx`, and `frontend-react/src/features/recipes/api.ts`
- [X] T023 [P] [US2] Implement recipe create/edit forms plus image, asset, comment, rating, and favorite interactions in `frontend-react/src/routes/g/$groupSlug/r/create.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/new.tsx`, `frontend-react/src/components/recipes/RecipeEditor.tsx`, and `frontend-react/src/components/recipes/RecipeInteractions.tsx`
- [X] T024 [P] [US2] Implement recipe import entry points and share-target redirect handling in `frontend-react/src/routes/g/$groupSlug/r/create/url.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/zip.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/html.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/image.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/bulk.tsx`, `frontend-react/src/routes/g/$groupSlug/r/create/debug.tsx`, and `frontend-react/src/routes/r/create/url.tsx`
- [X] T025 [P] [US2] Implement recipe organization and cookbook routes in `frontend-react/src/routes/g/$groupSlug/recipes/categories/index.tsx`, `frontend-react/src/routes/g/$groupSlug/recipes/tags/index.tsx`, `frontend-react/src/routes/g/$groupSlug/recipes/tools/index.tsx`, `frontend-react/src/routes/g/$groupSlug/cookbooks/index.tsx`, and `frontend-react/src/routes/g/$groupSlug/cookbooks/$slug.tsx`
- [X] T026 [US2] Implement the public/shared recipe route and meaningful share metadata parity in `frontend-react/src/routes/g/$groupSlug/shared/r/$id.tsx`, `frontend-react/src/lib/seo/recipeMeta.ts`, and `frontend-react/src/components/recipes/PublicRecipePage.tsx`

**Checkpoint**: Core recipe creation, discovery, interaction, import, and public sharing are independently functional in React.

---

## Phase 5: User Story 3 - Household Planning and Shopping Continue Uninterrupted (Priority: P1)

**Goal**: Household users can manage meal plans, planning rules, and shopping lists in React without losing persistence or cross-workflow continuity.

**Independent Test**: Create and edit meal plans, run planning-rule actions, create and update shopping lists from recipes, check off items, and verify state persists across refreshes and linked screens.

### Tests for User Story 3 ⚠️

> Write these tests first and confirm they fail before implementing the React planning/shopping slice.

- [X] T027 [P] [US3] Add meal planner parity coverage in `tests/e2e/react-migration/meal-planner.spec.ts`
- [X] T028 [P] [US3] Add shopping list parity coverage in `tests/e2e/react-migration/shopping-lists.spec.ts`

### Implementation for User Story 3

- [X] T029 [US3] Implement meal planner view/edit routes and planner interactions in `frontend-react/src/routes/household/mealplan/planner.tsx`, `frontend-react/src/routes/household/mealplan/planner/view.tsx`, `frontend-react/src/routes/household/mealplan/planner/edit.tsx`, and `frontend-react/src/components/mealplan/MealPlanner.tsx`
- [X] T030 [P] [US3] Implement meal planning rules, settings, and assisted planning actions in `frontend-react/src/routes/household/mealplan/settings.tsx`, `frontend-react/src/components/mealplan/PlanningRulesForm.tsx`, and `frontend-react/src/features/mealplan/actions.ts`
- [X] T031 [US3] Implement shopping list index/detail/item mutation flows in `frontend-react/src/routes/shopping-lists/index.tsx`, `frontend-react/src/routes/shopping-lists/$id.tsx`, and `frontend-react/src/components/shopping/ShoppingListEditor.tsx`
- [X] T032 [US3] Implement cross-links between recipe, meal-plan, and shopping-list workflows in `frontend-react/src/features/shopping/fromRecipe.ts`, `frontend-react/src/features/mealplan/toShoppingList.ts`, and `frontend-react/src/components/navigation/WorkflowLinks.tsx`

**Checkpoint**: Meal planning and shopping list workflows are independently functional and stable under refresh and collaboration scenarios.

---

## Phase 6: User Story 4 - Administration, Settings, and Localization Remain Complete (Priority: P2)

**Goal**: Profile, household management, group-data, admin, locale, and RTL-sensitive surfaces all remain usable with the same permission boundaries as the current frontend.

**Independent Test**: Edit profile settings, manage API tokens and favorites, exercise household/group/admin management routes, switch locales, and verify representative RTL navigation and layouts remain usable.

### Tests for User Story 4 ⚠️

> Write these tests first and confirm they fail before implementing the React profile/admin/localization slice.

- [X] T033 [P] [US4] Add profile, household-management, and group-data parity coverage in `tests/e2e/react-migration/profile-household-group.spec.ts`
- [X] T034 [P] [US4] Add admin authorization, locale-switching, and RTL smoke coverage in `tests/e2e/react-migration/admin-localization.spec.ts`

### Implementation for User Story 4

- [X] T035 [US4] Implement profile, favorites, API token, and personal-preference routes in `frontend-react/src/routes/user/$id/favorites.tsx`, `frontend-react/src/routes/user/profile/index.tsx`, `frontend-react/src/routes/user/profile/edit.tsx`, and `frontend-react/src/routes/user/profile/api-tokens.tsx`
- [X] T036 [P] [US4] Implement household member, notifier, and webhook management surfaces in `frontend-react/src/routes/household/index.tsx`, `frontend-react/src/routes/household/members.tsx`, `frontend-react/src/routes/household/notifiers.tsx`, and `frontend-react/src/routes/household/webhooks.tsx`
- [X] T037 [P] [US4] Implement group-data, organizer, reports, and migration-helper surfaces in `frontend-react/src/routes/group/index.tsx`, `frontend-react/src/routes/group/data.tsx`, `frontend-react/src/routes/group/data/recipes.tsx`, `frontend-react/src/routes/group/data/categories.tsx`, `frontend-react/src/routes/group/data/tags.tsx`, `frontend-react/src/routes/group/data/tools.tsx`, `frontend-react/src/routes/group/data/foods.tsx`, `frontend-react/src/routes/group/data/units.tsx`, `frontend-react/src/routes/group/data/labels.tsx`, `frontend-react/src/routes/group/data/recipe-actions.tsx`, `frontend-react/src/routes/group/reports/$id.tsx`, and `frontend-react/src/routes/group/migrations.tsx`
- [X] T038 [US4] Implement admin setup, site settings, backups, AI configuration, debug, and maintenance surfaces in `frontend-react/src/routes/admin/setup.tsx`, `frontend-react/src/routes/admin/site-settings.tsx`, `frontend-react/src/routes/admin/backups.tsx`, `frontend-react/src/routes/admin/ai-configurations.tsx`, `frontend-react/src/routes/admin/debug/indexes.tsx`, `frontend-react/src/routes/admin/debug/openai.tsx`, `frontend-react/src/routes/admin/debug/parser.tsx`, and `frontend-react/src/routes/admin/maintenance/index.tsx`
- [X] T039 [US4] Implement admin manage users/groups/households/ingredient-aliases routes with admin-only guard parity in `frontend-react/src/routes/admin/manage/users/index.tsx`, `frontend-react/src/routes/admin/manage/users/create.tsx`, `frontend-react/src/routes/admin/manage/users/$id.tsx`, `frontend-react/src/routes/admin/manage/groups/index.tsx`, `frontend-react/src/routes/admin/manage/groups/$id.tsx`, `frontend-react/src/routes/admin/manage/households/index.tsx`, `frontend-react/src/routes/admin/manage/households/$id.tsx`, and `frontend-react/src/routes/admin/manage/ingredient-aliases.tsx`
- [X] T040 [US4] Implement locale selection, persisted preference handling, and RTL-safe layout adjustments across `frontend-react/src/components/settings/LocaleSelector.tsx`, `frontend-react/src/lib/i18n/persistedLocale.ts`, `frontend-react/src/theme/index.ts`, and `frontend-react/src/components/layout/AppShell.tsx`

**Checkpoint**: Power-user, household-management, profile, admin, and localization surfaces are independently functional with preserved permission boundaries.

---

## Phase 7: User Story 5 - Rollout, Coexistence, and Rollback Are Safe (Priority: P2)

**Goal**: Operators can run React and legacy side by side, cut over approved surfaces safely, validate parity evidence, and roll back quickly without backend or account changes.

**Independent Test**: Enable React for an approved route slice, verify unsupported routes still land on a working legacy experience, execute a rollback, and confirm users continue working without data repair or re-registration.

### Tests for User Story 5 ⚠️

> Write these tests first and confirm they fail before implementing coexistence and cutover controls.

- [X] T041 [P] [US5] Add coexistence routing and legacy-fallback smoke coverage in `tests/e2e/react-migration/coexistence-routing.spec.ts`
- [X] T042 [P] [US5] Add cutover and rollback drill coverage in `tests/e2e/react-migration/cutover-rollback.spec.ts`

### Implementation for User Story 5

- [X] T043 [US5] Implement release-variant routing config, approved route-slice flags, and fallback mapping in `frontend-react/src/config/releaseVariant.ts`, `frontend-react/src/router/fallbackRoutes.ts`, and `specs/003-react-frontend-migration/contracts/route-parity.yaml`
- [X] T044 [US5] Wire nginx and compose cutover controls so React serves approved subtrees and legacy remains the fallback in `frontend-react/nginx.conf`, `frontend/nginx.conf`, `docker-compose.dotnet.yml`, and `Taskfile.yml`
- [X] T045 [US5] Implement parity-gap reporting and phase evidence output in `specs/003-react-frontend-migration/contracts/release-gates.yaml`, `specs/003-react-frontend-migration/checklists/phase-parity.md`, and `tests/e2e/react-migration/reporters/parityReport.ts`
- [X] T046 [US5] Document the operator rollout, rollback, and phased-release workflow in `specs/003-react-frontend-migration/quickstart.md` and `docs/frontend/react-migration-rollout.md`

**Checkpoint**: Coexistence, cutover, and rollback are independently testable and controlled by documented release gates.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Close the final parity gaps, validate the full migration, cut over permanently, and retire the legacy frontend safely.

- [X] T047 [P] Run the full route-coverage, locale-matrix, and public-metadata validation suite in `tests/e2e/react-migration/route-coverage.spec.ts`, `tests/e2e/react-migration/locale-matrix.spec.ts`, `tests/e2e/react-migration/public-metadata.spec.ts`, and `specs/003-react-frontend-migration/contracts/release-gates.yaml`
- [X] T048 [P] Add accessibility and top-workflow performance regression checks in `tests/e2e/react-migration/performance.spec.ts`, `frontend-react/src/test/accessibility/navigation.a11y.test.tsx`, and `frontend-react/src/test/accessibility/forms.a11y.test.tsx`
- [X] T049 Switch the default shipped SPA to the React workspace while preserving explicit rollback controls in `docker-compose.dotnet.yml`, `Taskfile.yml`, `frontend-react/Dockerfile`, and `frontend/nginx.conf`
- [ ] T050 Retire the legacy Nuxt frontend and consolidate the final single-SPA deployment shape in `frontend/`, `frontend-react/`, `docs/frontend/react-migration-rollout.md`, and `specs/003-react-frontend-migration/contracts/route-parity.yaml`
- Note: T047 and T049 are now validated against the live Docker stack. T050 remains open because the `legacy-only` rollback contract is still active and depends on the Nuxt gateway/frontend assets in `frontend/`; removing them in this pass would break the documented rollback expectation before the retirement window is formally closed.

**Final Checkpoint**: All release gates pass, React is the default and only supported SPA, rollback windows are closed, and the legacy frontend is retired.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** — starts immediately
- **Phase 2: Foundational** — depends on T001-T004 and blocks all story work
- **Phase 3: US1** — depends on T005-T012
- **Phase 4: US2** — depends on T005-T012 and should follow US1 once auth/navigation parity is proven
- **Phase 5: US3** — depends on T005-T012 and should follow US1 because planning/shopping routes rely on the authenticated shell
- **Phase 6: US4** — depends on T005-T012 and should follow US1-US3 so profile/household/admin surfaces reuse proven shell, auth, and locale primitives
- **Phase 7: US5** — depends on T005-T012 and should follow US1-US4 because coexistence gates need actual migrated surfaces to promote
- **Phase 8: Polish** — depends on all selected user stories being complete

### User Story Dependencies

- **US1 (P1)**: Starts immediately after the foundational runtime is ready; no other story dependencies
- **US2 (P1)**: Depends on US1 for auth/session, group shell, and route guard parity
- **US3 (P1)**: Depends on US1 for authenticated navigation and shared session continuity; otherwise independent of US2
- **US4 (P2)**: Depends on US1 for guards/shell and should follow US2-US3 so locale, admin, and management surfaces build on proven route patterns
- **US5 (P2)**: Depends on US1-US4 because rollout, cutover, and rollback controls need real migrated route slices to govern

### Within Each User Story

- Write the listed tests first and confirm they fail before implementation
- Route files and page metadata come after the shared runtime pieces they depend on
- Data/query adapters should be in place before page-level forms and interactions
- Do not promote a surface until its parity evidence and fallback path both exist

### Parallel Opportunities

- **Setup**: T003 and T004 can run in parallel after T001-T002
- **Foundational**: T006-T011 can run in parallel once T005 is in place
- **US1**: T013 and T014 can run in parallel; T017 can run in parallel with T015-T016 once shared auth primitives exist
- **US2**: T020 and T021 can run in parallel; T023-T025 can run in parallel after T022 starts the recipe route structure
- **US3**: T027 and T028 can run in parallel; T030 can run in parallel with T031 after T029 establishes planner routes
- **US4**: T033 and T034 can run in parallel; T036 and T037 can run in parallel after T035 establishes shared management patterns
- **US5**: T041 and T042 can run in parallel; T043 and T045 can run in parallel before T044 integrates the runtime controls

---

## Parallel Example: User Story 1

```bash
Task: "T013 Add auth entry-point and session continuity coverage in tests/e2e/react-migration/auth-parity.spec.ts"
Task: "T014 Add root redirect, protected deep-link, and post-login return coverage in tests/e2e/react-migration/navigation-parity.spec.ts"
Task: "T017 Implement the authenticated navigation shell and group landing route parity in frontend-react/src/components/layout/AppShell.tsx, frontend-react/src/components/navigation/MainNav.tsx, and frontend-react/src/routes/g/$groupSlug/index.tsx"
```

## Parallel Example: User Story 2

```bash
Task: "T020 Add recipe browse, detail, edit, and import journey coverage in tests/e2e/react-migration/recipe-workflows.spec.ts"
Task: "T021 Add public/shared recipe and metadata parity coverage in tests/e2e/react-migration/recipe-public.spec.ts"
Task: "T024 Implement recipe import entry points and share-target redirect handling in frontend-react/src/routes/g/$groupSlug/r/create/url.tsx, frontend-react/src/routes/g/$groupSlug/r/create/zip.tsx, frontend-react/src/routes/g/$groupSlug/r/create/html.tsx, frontend-react/src/routes/g/$groupSlug/r/create/image.tsx, frontend-react/src/routes/g/$groupSlug/r/create/bulk.tsx, frontend-react/src/routes/g/$groupSlug/r/create/debug.tsx, and frontend-react/src/routes/r/create/url.tsx"
Task: "T025 Implement recipe organization and cookbook routes in frontend-react/src/routes/g/$groupSlug/recipes/categories/index.tsx, frontend-react/src/routes/g/$groupSlug/recipes/tags/index.tsx, frontend-react/src/routes/g/$groupSlug/recipes/tools/index.tsx, frontend-react/src/routes/g/$groupSlug/cookbooks/index.tsx, and frontend-react/src/routes/g/$groupSlug/cookbooks/$slug.tsx"
```

## Parallel Example: User Story 3

```bash
Task: "T027 Add meal planner parity coverage in tests/e2e/react-migration/meal-planner.spec.ts"
Task: "T028 Add shopping list parity coverage in tests/e2e/react-migration/shopping-lists.spec.ts"
Task: "T030 Implement meal planning rules, settings, and assisted planning actions in frontend-react/src/routes/household/mealplan/settings.tsx, frontend-react/src/components/mealplan/PlanningRulesForm.tsx, and frontend-react/src/features/mealplan/actions.ts"
Task: "T031 Implement shopping list index/detail/item mutation flows in frontend-react/src/routes/shopping-lists/index.tsx, frontend-react/src/routes/shopping-lists/$id.tsx, and frontend-react/src/components/shopping/ShoppingListEditor.tsx"
```

## Parallel Example: User Story 4

```bash
Task: "T033 Add profile, household-management, and group-data parity coverage in tests/e2e/react-migration/profile-household-group.spec.ts"
Task: "T034 Add admin authorization, locale-switching, and RTL smoke coverage in tests/e2e/react-migration/admin-localization.spec.ts"
Task: "T036 Implement household member, notifier, and webhook management surfaces in frontend-react/src/routes/household/index.tsx, frontend-react/src/routes/household/members.tsx, frontend-react/src/routes/household/notifiers.tsx, and frontend-react/src/routes/household/webhooks.tsx"
Task: "T037 Implement group-data, organizer, reports, and migration-helper surfaces in frontend-react/src/routes/group/index.tsx, frontend-react/src/routes/group/data.tsx, frontend-react/src/routes/group/data/recipes.tsx, frontend-react/src/routes/group/data/categories.tsx, frontend-react/src/routes/group/data/tags.tsx, frontend-react/src/routes/group/data/tools.tsx, frontend-react/src/routes/group/data/foods.tsx, frontend-react/src/routes/group/data/units.tsx, frontend-react/src/routes/group/data/labels.tsx, frontend-react/src/routes/group/data/recipe-actions.tsx, frontend-react/src/routes/group/reports/$id.tsx, and frontend-react/src/routes/group/migrations.tsx"
```

## Parallel Example: User Story 5

```bash
Task: "T041 Add coexistence routing and legacy-fallback smoke coverage in tests/e2e/react-migration/coexistence-routing.spec.ts"
Task: "T042 Add cutover and rollback drill coverage in tests/e2e/react-migration/cutover-rollback.spec.ts"
Task: "T043 Implement release-variant routing config, approved route-slice flags, and fallback mapping in frontend-react/src/config/releaseVariant.ts, frontend-react/src/router/fallbackRoutes.ts, and specs/003-react-frontend-migration/contracts/route-parity.yaml"
Task: "T045 Implement parity-gap reporting and phase evidence output in specs/003-react-frontend-migration/contracts/release-gates.yaml, specs/003-react-frontend-migration/checklists/phase-parity.md, and tests/e2e/react-migration/reporters/parityReport.ts"
```

---

## Implementation Strategy

### MVP First

1. Complete **Phase 1: Setup**
2. Complete **Phase 2: Foundational**
3. Complete **Phase 3: US1 Secure Access and Navigation Parity**
4. Validate auth, deep-link, and session continuity before expanding migration scope

### Incremental Delivery

1. Foundation + US1 establishes the safe hybrid shell
2. Add **US2** for core recipe workflows
3. Add **US3** for meal planning and shopping
4. Add **US4** for profile, household, group-data, admin, and localization completeness
5. Add **US5** for controlled coexistence, cutover, and rollback
6. Finish with full validation, final cutover, and legacy retirement

### Suggested MVP Scope

- **Recommended MVP**: T001-T019 (Setup + Foundational + US1)
- This delivers the smallest valuable slice that proves the React runtime, auth/session parity, route guards, and navigation shell before migrating the broader product surface

---

## Task Summary

| Phase | Task Range | Count |
|---|---|---:|
| Setup | T001-T004 | 4 |
| Foundational | T005-T012 | 8 |
| US1 | T013-T019 | 7 |
| US2 | T020-T026 | 7 |
| US3 | T027-T032 | 6 |
| US4 | T033-T040 | 8 |
| US5 | T041-T046 | 6 |
| Polish | T047-T050 | 4 |
| **Total** | **T001-T050** | **50** |

---

## Notes

- Route/workflow inventory remains anchored in `specs/003-react-frontend-migration/contracts/route-parity.yaml`
- Release approval remains anchored in `specs/003-react-frontend-migration/contracts/release-gates.yaml`
- Preserve existing backend contracts, auth semantics, public-route behavior, locale scope, and static deployment assumptions unless an explicit migration exception is approved
- Do not retire the legacy frontend until route coverage, parity-gap closure, and rollback drills are complete
