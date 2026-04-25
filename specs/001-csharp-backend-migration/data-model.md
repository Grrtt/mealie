# Data Model: Mealie C# Backend Migration

**Phase**: Phase 1 — Design  
**Feature**: `001-csharp-backend-migration`  
**Source**: SQLAlchemy models in `mealie/db/models/`

---

## Overview

The C# EF Core schema maps 1:1 to the Python SQLAlchemy schema with the following structural rules:

- All table names are preserved exactly (EF Core `.ToTable("table_name")` explicit mapping).
- All column names are preserved exactly (snake_case via `UseSnakeCaseNamingConvention()`).
- All primary key values (UUIDs, integer PKs) are preserved verbatim by the migration tool.
- UUID columns: `Guid` in C# → `TEXT` in SQLite / `uuid` in PostgreSQL.
- Soft-delete is NOT used (Python backend uses hard deletes).
- Timestamps: `created_at`, `updated_at` on all entities via `BaseMixins` equivalent.

---

## Core Domain Entities

### Group
**Table**: `groups`  
**Tenancy**: Top-level boundary.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | UUID, preserved verbatim |
| `name` | `string` | Not null, unique |
| `slug` | `string?` | Unique index |
| `created_at` | `DateTime` | UTC |
| `update_at` | `DateTime` | UTC |

**Relationships**:
- Has many `Household`
- Has many `User`
- Has many `Recipe` (all recipes in group)
- Has many `Tag`, `Category`, `Tool` (via junction tables)
- Has many `IngredientUnit`, `IngredientFood`
- Has one `GroupPreferences`
- Has many `GroupInviteToken`, `GroupWebhook`, `Cookbook`, `MealPlan`, `ShoppingList`

**Validation Rules**:
- `name`: required, max 255 chars, globally unique
- `slug`: auto-generated from name if not provided

---

### Household
**Table**: `households`  
**Tenancy**: Second-level boundary. Owns most content.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | UUID |
| `name` | `string` | Not null |
| `slug` | `string?` | |
| `group_id` | `Guid` FK → `groups.id` | Not null, indexed |
| `created_at` | `DateTime` | UTC |
| `update_at` | `DateTime` | UTC |

**Relationships**:
- Belongs to `Group`
- Has many `User`
- Has many owned recipes via `HouseholdToRecipe` junction
- Has many `Cookbook`, `MealPlan`, `ShoppingList`, `Webhook`, `EventNotifier`
- Has one `HouseholdPreferences`

**EF Core Query Filter**: none at household level (filter applied at resource level by `household_id`).

---

### User
**Table**: `users`  
**Auth Methods**: `Mealie`, `LDAP`, `OIDC` (enum).

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | UUID |
| `full_name` | `string?` | Indexed |
| `username` | `string?` | Unique, indexed |
| `email` | `string?` | Unique, indexed |
| `password` | `string?` | bcrypt hash |
| `auth_method` | `AuthMethod` enum | |
| `admin` | `bool` | default false |
| `advanced` | `bool` | default false |
| `group_id` | `Guid` FK → `groups.id` | Not null, indexed |
| `household_id` | `Guid?` FK → `households.id` | Nullable, indexed |
| `cache_key` | `string?` | Used to invalidate sessions |
| `login_attempts` | `int` | default 0 |
| `locked_at` | `DateTime?` | Account lockout |
| `can_manage_household` | `bool` | Permission flag |
| `can_manage` | `bool` | Permission flag |
| `can_invite` | `bool` | Permission flag |
| `can_organize` | `bool` | Permission flag |
| `show_announcements` | `bool` | default true |
| `last_read_announcement` | `string?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Validation Rules**:
- `email`: required for local auth, valid email format
- `username`: required, unique across the system
- `password`: bcrypt-hashed on write, minimum length not re-validated on read

---

### ApiKey (LongLiveToken)
**Table**: `long_live_tokens`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | UUID |
| `name` | `string` | Human label |
| `token` | `string` | Indexed; stored as bcrypt hash |
| `user_id` | `Guid?` FK → `users.id` | Indexed |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Association proxies** (computed at query time, not stored): `group_id`, `household_id` (via User).

---

### Recipe
**Table**: `recipes`  
**Unique Constraint**: `(slug, group_id)`.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | UUID |
| `slug` | `string?` | Indexed |
| `group_id` | `Guid` FK → `groups.id` | Not null, indexed |
| `user_id` | `Guid?` FK → `users.id` | Creator, indexed |
| `name` | `string` | Not null |
| `description` | `string?` | |
| `image` | `string?` | Filename of stored image |
| `total_time` | `string?` | ISO 8601 duration or freeform |
| `prep_time` | `string?` | |
| `perform_time` | `string?` | |
| `cook_time` | `string?` | |
| `recipe_yield` | `string?` | Freeform text |
| `recipe_yield_quantity` | `double` | Indexed, default 0 |
| `recipe_servings` | `double` | Indexed, default 0 |
| `rating` | `double?` | Indexed, nullable |
| `date_added` | `DateOnly?` | |
| `date_updated` | `DateTime?` | |
| `last_made` | `DateTime?` | |
| `public` | `bool` | Whether visible outside household |
| `disable_amounts` | `bool` | |
| `disable_comments` | `bool` | |
| `is_ocr_recipe` | `bool` | |
| `org_url` | `string?` | Source URL if scraped |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**EF Core Query Filter**: `r => r.GroupId == _tenantContext.GroupId` (household filter applied via `HouseholdToRecipe` join for non-admin, non-public queries).

**Relationships**:
- Has one `RecipeSettings`
- Has one `Nutrition`
- Has many `RecipeIngredient` (ordered by `position`)
- Has many `RecipeInstruction` (ordered by `position`)
- Has many `RecipeNote`
- Has many `RecipeAsset`
- Has many `RecipeComment`
- Has many `RecipeTimelineEvent`
- Has many `Tag` (via `recipes_to_tags`)
- Has many `Category` (via `recipes_to_categories`)
- Has many `Tool` (via `recipes_to_tools`)
- Has many `User` (rated_by, favorited_by via `user_to_recipe`)
- Has many `HouseholdToRecipe` (household ownership)
- Belongs to `Group`, `User` (creator)

---

### RecipeIngredient
**Table**: `recipes_ingredients`  
**Ordering**: `position` column, 0-based.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `recipe_id` | `Guid` FK → `recipes.id` | Indexed |
| `referenced_recipe_id` | `Guid?` FK → `recipes.id` | For sub-recipe references |
| `unit_id` | `Guid?` FK → `ingredient_units.id` | |
| `food_id` | `Guid?` FK → `ingredient_foods.id` | |
| `quantity` | `double?` | |
| `note` | `string?` | |
| `original_text` | `string?` | Raw input string |
| `title` | `string?` | Section header if set |
| `position` | `int` | Ordering within recipe |
| `disable_amount` | `bool` | |
| `display` | `string?` | Override display string |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### RecipeInstruction
**Table**: `recipe_instructions`  
**Ordering**: `position` column.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `recipe_id` | `Guid` FK | |
| `title` | `string?` | Section header |
| `text` | `string` | Instruction body, not null |
| `ingredient_references` | JSON | List of `{id}` references |
| `position` | `int` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### IngredientFood
**Table**: `ingredient_foods`  
**Scope**: Shared within a Group.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `group_id` | `Guid` FK → `groups.id` | Not null, indexed |
| `name` | `string?` | |
| `plural_name` | `string?` | |
| `description` | `string?` | |
| `label_id` | `Guid?` FK → `multi_purpose_labels.id` | |
| `aliases` | nav | via `IngredientFoodAlias` |
| `on_hand` | `bool` | |
| `name_normalized` | `string?` | Auto-indexed for fuzzy matching |
| `plural_name_normalized` | `string?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Junction table** `households_to_ingredient_foods`: links Household → Food for "on hand" tracking.

---

### IngredientUnit
**Table**: `ingredient_units`  
**Scope**: Shared within a Group.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `group_id` | `Guid` FK | |
| `name` | `string?` | |
| `plural_name` | `string?` | |
| `description` | `string?` | |
| `abbreviation` | `string?` | |
| `plural_abbreviation` | `string?` | |
| `use_abbreviation` | `bool` | |
| `fraction` | `bool` | |
| `standard_quantity` | `double?` | For unit conversion |
| `standard_unit` | `string?` | |
| `name_normalized` | `string?` | Auto-indexed |
| `plural_name_normalized` | `string?` | |
| `abbreviation_normalized` | `string?` | |
| `plural_abbreviation_normalized` | `string?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### Tag / Category / Tool
**Tables**: `tags`, `categories`, `tools`  
**Scope**: Scoped to a Group.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `group_id` | `Guid` FK | |
| `name` | `string` | Not null |
| `slug` | `string?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Junction tables** (many-to-many with recipes):
- `recipes_to_tags(recipe_id, tag_id)`
- `recipes_to_categories(recipe_id, category_id)`
- `recipes_to_tools(recipe_id, tool_id)`

---

### Cookbook
**Table**: `cookbooks`  
**Scope**: Belongs to Household.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `name` | `string` | |
| `slug` | `string?` | |
| `description` | `string?` | |
| `position` | `int` | Display ordering |
| `public` | `bool` | Visible to other households in group |
| `group_id` | `Guid` FK | |
| `household_id` | `Guid` FK | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Relationships**: Has many `Category` (filter categories), has many `Tag` (filter tags) via junction tables.

---

### MealPlan (GroupMealPlan)
**Table**: `group_meal_plans`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `date` | `DateOnly` | Indexed |
| `entry_type` | `string` | `breakfast`, `lunch`, `dinner`, `side` |
| `title` | `string` | |
| `text` | `string?` | Free notes |
| `recipe_id` | `Guid?` FK → `recipes.id` | Nullable (can be free-text entry) |
| `group_id` | `Guid` FK | |
| `household_id` | `Guid` FK | |
| `user_id` | `Guid?` FK | Creator |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### ShoppingList
**Table**: `shopping_lists`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `name` | `string` | |
| `group_id` | `Guid` FK | |
| `household_id` | `Guid` FK | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Relationships**: Has many `ShoppingListItem`, has many `ShoppingListRecipeReference`.

### ShoppingListItem
**Table**: `shopping_list_items`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `shopping_list_id` | `Guid` FK | Indexed |
| `checked` | `bool` | |
| `is_food` | `bool` | |
| `disable_amount` | `bool` | |
| `position` | `int` | |
| `food_id` | `Guid?` FK | |
| `label_id` | `Guid?` FK | |
| `unit_id` | `Guid?` FK | |
| `quantity` | `double` | |
| `note` | `string?` | |
| `extras` | JSON | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

**Junction**: `ShoppingListItemRecipeReference` — links item to recipe ingredient for quantity tracking.

---

### Webhook (GroupWebhooksModel)
**Table**: `group_webhooks`  
**Scope**: Scoped to a Group.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `enabled` | `bool` | |
| `name` | `string` | |
| `url` | `string` | Target URL |
| `webhook_type` | `string` | Event type filter |
| `scheduled_time` | `TimeOnly?` | For time-based webhooks |
| `group_id` | `Guid` FK | |
| `household_id` | `Guid?` FK | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### Backup
**Table**: `server_tasks` (used to record backup jobs)  
**Storage**: Files on disk in `DATA_DIR/backups/`.

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `task_type` | `string` | `backup`, `restore`, etc. |
| `status` | `string` | `running`, `completed`, `failed` |
| `log` | `string?` | |
| `group_id` | `Guid` FK | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### GroupPreferences
**Table**: `group_preferences`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `group_id` | `Guid` FK | One-to-one |
| `private_group` | `bool` | |
| `first_day_of_week` | `int` | 0=Sunday |
| `recipe_public` | `bool` | |
| `recipe_show_nutrition` | `bool` | |
| `recipe_show_assets` | `bool` | |
| `recipe_landscape_images` | `bool` | |
| `recipe_disable_comments` | `bool` | |
| `recipe_disable_amount` | `bool` | |

---

### HouseholdPreferences
**Table**: `household_preferences`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `household_id` | `Guid` FK | One-to-one |
| `private_household` | `bool` | |
| `first_day_of_week` | `int` | |
| `recipe_public` | `bool` | |

---

### MultiPurposeLabel
**Table**: `multi_purpose_labels`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `name` | `string` | |
| `color` | `string?` | Hex color |
| `group_id` | `Guid` FK | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### RecipeTimelineEvent
**Table**: `recipe_timeline_events`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `recipe_id` | `Guid` FK | |
| `user_id` | `Guid?` FK | |
| `subject` | `string` | |
| `event_type` | `string` | `system`, `info`, `warning` |
| `timestamp` | `DateTime` | |
| `image` | `string?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### GroupInviteToken
**Table**: `group_invite_tokens`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `token` | `string` | |
| `group_id` | `Guid` FK | |
| `uses_left` | `int?` | Null = unlimited |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### RecipeComment
**Table**: `recipe_comments`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `recipe_id` | `Guid` FK | Indexed |
| `user_id` | `Guid` FK | |
| `text` | `string` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

### RecipeShareToken
**Table**: `recipe_share_tokens`

| Column | C# Type | Notes |
|--------|---------|-------|
| `id` | `Guid` PK | |
| `recipe_id` | `Guid` FK | |
| `group_id` | `Guid` FK | |
| `expires_at` | `DateTime?` | |
| `created_at` | `DateTime` | |
| `update_at` | `DateTime` | |

---

## EF Core Configuration Highlights

### Naming Convention
```csharp
optionsBuilder.UseSnakeCaseNamingConvention();
```
Maps `RecipeId` → `recipe_id`, `GroupId` → `group_id` etc., preserving the Python schema column names.

### Global Query Filters
```csharp
// Applied in OnModelCreating via extension method:
builder.ApplyTenantFilters(_tenantContextAccessor);
```
Entities with `HouseholdId`: filtered by current household.  
Entities with only `GroupId`: filtered by current group.  
Admin controllers call `.IgnoreQueryFilters()`.

### Concurrency
Last-write-wins on Recipe updates (matching Python behavior). No optimistic concurrency tokens in EF Core (`[ConcurrencyCheck]`) — consistent with Python `Base.update()` semantics.

### State Transitions

**Recipe Publication Flow**:
```
draft → public (when public=true set by user)
public → household-private (when public=false)
```

**User Account States**:
```
active → locked (when login_attempts >= threshold and locked_at set)
locked → active (when admin resets or lockout expires)
```

**Backup States**:
```
running → completed
running → failed
```

---

## Migration Tool Entity Order (dependency-safe)

1. `groups`
2. `households`
3. `users`
4. `multi_purpose_labels`
5. `ingredient_units`
6. `ingredient_foods`
7. `tags`, `categories`, `tools`
8. `cookbooks`
9. `recipes`
10. `recipe_ingredients`, `recipe_instructions`, `recipe_notes`, `recipe_assets`
11. `recipe_timeline_events`, `recipe_comments`, `recipe_share_tokens`
12. `group_meal_plans`, `shopping_lists`, `shopping_list_items`
13. `group_webhooks`, `group_event_notifiers`
14. `long_live_tokens`
15. Junction tables (all `*_to_*` tables)
16. `__mealie_migration_log` (written last to mark completion)
