# Data Model: Consolidation Refactor Design

**Phase**: Phase 1 — Design  
**Feature**: `001-csharp-backend-migration`

---

## Overview

This refactor plan does **not** change the persistent database schema or public API contracts. The "data model" for this effort is the set of internal abstractions that will own duplicated behavior after consolidation.

### Global invariants

- Existing route paths, DTO types, and status codes remain unchanged.
- Existing group/household tenant boundaries remain unchanged.
- Existing database entities remain the source of truth.
- Shared abstractions may centralize behavior, but they must not hide required tenant IDs or 404-masking decisions.

---

## Refactor entities

### 1. OrganizerCrudModule

**Purpose**: Shared CRUD execution path for tags, categories, and tools.

| Field / Responsibility | Description |
|---|---|
| `OrganizerKind` | Identifies Tag, Category, or Tool behavior |
| `QueryShape` | List/get/get-by-slug/get-recipes/get-empty flow for one organizer kind |
| `CommandShape` | Create/update/delete flow for one organizer kind |
| `RouteMetadata` | Keeps existing controller route prefix and action naming stable |
| `ResponseMapper` | Preserves the current DTO per organizer type |
| `SlugPolicy` | Shared create/update slug generation and uniqueness enforcement |

**Relationships**
- Uses `OrganizerSlugPolicy`
- Invoked by organizer controllers and organizer commands/queries

**Validation rules**
- Every execution path requires `GroupId`
- Update must use the same uniqueness rules as create
- Shared internals cannot change entity-specific DTO types

---

### 2. OrganizerSlugPolicy

**Purpose**: Centralize slug normalization and uniqueness checks for organizer entities.

| Input | Output |
|---|---|
| `GroupId` | Tenant-safe slug scope |
| `Name` | Generated base slug |
| `ExistingEntityId?` | Allows update flow to exclude current record when checking uniqueness |
| `EntitySet` | Tag/category/tool repository source |

**Validation rules**
- Slug uniqueness is enforced within group scope
- Update and create must follow identical collision rules

---

### 3. MealPlanMappingHelper

**Purpose**: Shared loading/mapping path for meal plan responses.

| Field / Responsibility | Description |
|---|---|
| `MealPlanEntity` | Source meal plan record(s) |
| `RecipeLoadContext` | Recipe/nav data needed for response hydration |
| `ResponseProjection` | Shared DTO construction logic |
| `DateScope` | Day/week/query date boundaries |

**Relationships**
- Consumed by meal plan commands and queries
- Independent from recipe-selection decisions

**Validation rules**
- Must not choose recipes
- Must preserve current response shape and ordering

---

### 4. MealPlanRecipeSelectionService

**Purpose**: Shared rule and recipe-selection logic for random/fill flows.

| Field / Responsibility | Description |
|---|---|
| `GroupId` / `HouseholdId` | Tenant scope for candidate selection |
| `DateScope` | Fill target date/day/week |
| `RuleInputs` | Existing rule and planner inputs |
| `SelectionResult` | Recipe IDs or selected meal plan candidates |

**Relationships**
- Produces selections consumed by meal plan commands
- Feeds `MealPlanMappingHelper` after persistence

**Validation rules**
- Preserve current fallback order
- Keep tenant-scoped recipe selection explicit

---

### 5. ShoppingListMappingHelper

**Purpose**: Shared DTO mapping for shopping lists and shopping list items.

| Field / Responsibility | Description |
|---|---|
| `ShoppingListEntity` | Source list entity |
| `ItemEntities` | Source item entities |
| `RelationLoadContext` | Food/unit/list relationships used by response DTOs |
| `ResponseProjection` | Shared list/item response construction |

**Validation rules**
- Must be read-only
- Must preserve current list/item JSON shape

---

### 6. ShoppingListItemMutationService

**Purpose**: Shared mutation logic for item create/update/merge/standalone flows.

| Field / Responsibility | Description |
|---|---|
| `ListId` | Owning shopping list |
| `MutationMode` | Add, update, standalone create, standalone update, bulk update |
| `MergeInputs` | Existing item matching/merge data |
| `PersistenceResult` | Saved item(s) with timestamps/state ready for mapping |

**Relationships**
- Uses `ShoppingListMappingHelper` for output
- May collaborate with `ShoppingListRecipeLinkService`

**Validation rules**
- Preserve existing merge semantics
- Preserve household scoping
- Keep recipe-linked and standalone flows distinguishable

---

### 7. ShoppingListRecipeLinkService

**Purpose**: Encapsulate recipe-to-shopping-list item creation/linking behavior.

| Field / Responsibility | Description |
|---|---|
| `RecipeId` | Source recipe |
| `IngredientInputs` | Recipe ingredient rows used to create items |
| `TargetListId` | Shopping list receiving items |
| `LinkResult` | Added/linked items and references |

**Validation rules**
- Preserve existing recipe-link semantics
- Must remain household-safe

---

### 8. IngredientCrudCore

**Purpose**: Shared CRUD core for foods and units.

| Field / Responsibility | Description |
|---|---|
| `IngredientKind` | Food or Unit |
| `ListQuery` | Paginated search behavior |
| `AliasMutation` | Replace aliases during create/update |
| `MergeBehavior` | Reassign ingredient references before delete |
| `ResponseMapper` | Food/unit-specific response mapping |
| `Hooks` | Optional behavior such as label loading or event publishing |

**Validation rules**
- Requires explicit `GroupId`
- Must preserve current entity-specific fields
- Merge must not weaken existing tenant checks

---

### 9. ParserProviderDescriptor

**Purpose**: Provider-specific configuration for OpenAI-compatible parser clients.

| Field | Description |
|---|---|
| `ProviderKind` | OpenAI, Azure OpenAI, Ollama, Custom |
| `BaseUrlStrategy` | How base URL is derived |
| `AuthStrategy` | Bearer, `api-key`, optional auth, or named client config |
| `NamedClient?` | Optional `IHttpClientFactory` client name |
| `HeaderSet` | Extra provider-specific headers |

**Relationships**
- Consumed by `ParserClientFactory`
- Leaves request/response behavior in `OpenAiCompatibleParserStrategy`

---

### 10. ParserClientFactory

**Purpose**: Centralized builder for provider-specific `HttpClient` instances.

| Input | Output |
|---|---|
| `AiParserConfig` | Provider runtime config |
| `ParserProviderDescriptor` | Provider construction rules |
| `IHttpClientFactory` | Underlying client creation |
| `HttpClient` | Fully configured provider client |

**Validation rules**
- Must preserve provider-specific auth/header semantics
- Must not change parser request/response payloads

---

### 11. SelfResourceControllerDescriptor

**Purpose**: Shared description of group/household self-resource controller behavior.

| Field / Responsibility | Description |
|---|---|
| `TenantKind` | Group or Household |
| `SelfGet` | `GET self` handler contract |
| `SelfUpdate` | `PUT self` handler contract |
| `PreferencesHandlers` | Preferences read/update behavior |
| `MembersHandlers` | Members list/get behavior |
| `InvitationHandlers?` | Invitation-related behavior where applicable |
| `AliasRoutes` | Legacy shortcut paths that must remain supported |

**Relationships**
- Used by a shared base/helper for `GroupsController` and `HouseholdsController`

**Validation rules**
- Preserve route aliases
- Preserve `NotFoundOrForbidden()` behavior
- Keep non-shared endpoints controller-specific

---

## Relationship summary

```text
Organizer controllers ──> OrganizerCrudModule ──> OrganizerSlugPolicy

Meal plan commands/queries ──> MealPlanRecipeSelectionService
Meal plan commands/queries ──> MealPlanMappingHelper

Shopping list commands/queries ──> ShoppingListItemMutationService
Shopping list commands/queries ──> ShoppingListRecipeLinkService
Shopping list commands/queries ──> ShoppingListMappingHelper

Foods/Units controllers/services ──> IngredientCrudCore

Parser strategies ──> ParserClientFactory ──> ParserProviderDescriptor

Groups/Households controllers ──> SelfResourceControllerDescriptor/shared helper
```

---

## State transitions

### Organizer slug lifecycle
`Name change` → `OrganizerSlugPolicy.Generate` → `Uniqueness check in group scope` → `Persist` → `Return unchanged DTO shape`

### Meal plan bulk flow
`Command input` → `Recipe selection` → `Persist meal plan rows` → `Shared mapping/load` → `Return response`

### Shopping list mutation flow
`Command input` → `Mutation/merge decision` → `Persist list/item rows` → `Optional recipe link update` → `Shared mapping` → `Return response`

### Parser provider flow
`AiParserConfig` → `Provider descriptor` → `Client factory` → `OpenAiCompatibleParserStrategy request` → `Parsed ingredient DTOs`

### Self-resource controller flow
`Tenant context` → `Shared self-resource helper` → `Entity-specific query/command` → `NotFoundOrForbidden()` or mapped response`
