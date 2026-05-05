# Quickstart: Implementing the Consolidation Plan

**Feature**: `001-csharp-backend-migration`  
**Goal**: Execute the scoped consolidation work in small, independently validated slices without changing public behavior.

---

## 1. Work from the scoped branch

```bash
cd /tmp/mealie-speckit-plan-1777774956/backend-dotnet
git branch --show-current
```

Expected branch: `001-csharp-backend-migration-plan`

---

## 2. Baseline before each workstream

From `backend-dotnet/`:

```bash
dotnet build Mealie.sln
dotnet test Mealie.sln
```

Use this baseline to confirm the refactor starts from a known-good state.

---

## 3. Implementation order

### Step 1 — Add characterization coverage

Before extracting shared code for a workstream:

- Capture current endpoint behavior and representative success/failure cases.
- Add or update tenant-isolation assertions.
- Add explicit parity coverage for slug, merge, random-selection, or route-alias behavior where relevant.

### Step 2 — Deliver high-priority workstreams

1. **Organizer CRUD consolidation**
   - Consolidate tags/categories/tools internals
   - Add create/update slug uniqueness tests
2. **Meal plan helper consolidation**
   - Split mapping from recipe-selection logic
   - Validate create/update/fill/random parity
3. **Shopping list helper consolidation**
   - Split mapping from item mutation logic
   - Validate merge/update/standalone/recipe-link parity

### Step 3 — Deliver medium-priority workstreams

4. **Food/unit CRUD consolidation**
   - Reuse patterns proven in organizer CRUD consolidation
5. **Parser provider consolidation**
   - Introduce client builder/factory for provider-specific `HttpClient` setup
6. **Tenant self-resource controller consolidation**
   - Extract shared self-resource flow while preserving route aliases

---

## 4. Validation checklist per workstream

### Organizer CRUD

- CRUD endpoints for tags/categories/tools still return the same DTOs
- `empty` and `recipes` endpoints still behave the same
- Slug collisions are handled consistently on create and update
- Cross-group access still returns 404

### Meal plans

- `CreateMealPlan`, `UpdateMealPlan`, `FillDay`, `FillWeek`, and meal plan queries still agree on response shape
- Random recipe selection preserves current fallback behavior
- Date-range ordering and fill behavior remain stable

### Shopping lists

- List and item DTOs remain unchanged
- Add/update/standalone item flows preserve current merge behavior
- Recipe-linked item creation still produces the same visible results
- Household scoping remains intact

### Foods and units

- Pagination/search behavior remains unchanged
- Alias replacement still works on update
- Merge continues to reassign ingredient references safely

### Parser providers

- Provider-specific base URLs, auth headers, and named-client configuration remain correct
- Request/response payloads from the shared OpenAI-compatible strategy remain unchanged
- Existing warnings around provider construction are reduced or documented

### Group/household self resources

- `self`, `preferences`, `members`, and invitation endpoints preserve current routes
- Shortcut aliases such as `members` and `self/members` still work where currently supported
- Not-found masking still returns 404 instead of leaking tenant state

---

## 5. Suggested PR slicing

Create separate implementation PRs (or equivalent task batches) for:

1. characterization tests + organizer CRUD
2. meal plan helpers
3. shopping list helpers
4. food/unit CRUD
5. parser providers
6. tenant self-resource controllers

This keeps risk localized and makes regressions easier to isolate.

---

## 6. Done criteria

The consolidation plan is complete when:

- All six scoped workstreams have an approved implementation sequence
- Each workstream has explicit validation points
- No low-priority items have been pulled into the main plan
- The plan still preserves API compatibility and tenant isolation as first-order constraints
