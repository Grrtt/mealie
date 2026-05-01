# Gap Analysis: Units & Foods

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/unit_and_foods/`

### Units
- `GET /units` — paginated list with search
- `POST /units` — create unit
- `GET /units/{id}` — get unit
- `PUT /units/{id}` — update unit
- `DELETE /units/{id}` — delete unit
- `POST /units/merge` — merge two units (reassign all ingredient refs, delete source)

### Foods
- `GET /foods` — paginated list with search
- `POST /foods` — create food
- `GET /foods/{id}` — get food
- `PUT /foods/{id}` — update food
- `DELETE /foods/{id}` — delete food
- `POST /foods/merge` — merge two foods (reassign all ingredient refs, delete source)

### Data Model
**Unit:**
- `name`, `abbreviation`, `plural_name`, `plural_abbreviation`
- `use_abbreviation` flag
- `fraction` flag (display as fraction vs decimal)
- `aliases` — list of alternative names for matching

**Food:**
- `name`, `plural_name`
- `description`
- `extras` (JSON)
- `label` (many-to-many to shopping labels)
- `aliases` — list of alternative names for parser matching
- `on_hand` — flag for pantry tracking

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Ingredients/`

### Implemented
- Units: CRUD
- Foods: CRUD

### Missing
- **Merge unit** — `POST /units/merge` not implemented
- **Merge food** — `POST /foods/merge` not implemented
- **Aliases** — `ingredient_foods_aliases` table exists in DB config (noted in memories); unclear if aliases are exposed in CRUD endpoints
- **Food `on_hand` flag** — unclear if stored/returned
- **Food labels** — many-to-many food ↔ shopping label unclear if implemented
- **Pagination + search** on list endpoints — unclear

---

## Enhancement Opportunities (C#-Specific)

- Merge endpoint: use EF Core `ExecuteUpdateAsync` to bulk-reassign all `RecipeIngredient.UnitId` / `FoodId` references in one SQL statement, then delete the source entity — efficient and atomic
- `IFoodAliasMatchingService` — used by the ingredient parser; stores aliases in `ingredient_foods_aliases` and matches against them during parsing; cache per group
- `on_hand` flag for foods enables a "pantry" feature — filter recipes by ingredients you have on hand; worth exposing as a recipe filter parameter
- Unit fractions: store `fraction` flag and use it in the frontend to display `1/2 cup` instead of `0.5 cup`
