# Gap Analysis: Shopping Lists

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/households/controller_shopping_lists.py`, `controller_shopping_items.py`
**Service:** `mealie/services/household/shopping_list_service.py`

### List Endpoints
- `GET /households/shopping/lists` — paginated list
- `POST /households/shopping/lists` — create list
- `GET /households/shopping/lists/{id}` — get with items
- `PUT /households/shopping/lists/{id}` — update list
- `DELETE /households/shopping/lists/{id}` — delete list
- `POST /households/shopping/lists/{id}/recipe` — add recipe ingredients
- `DELETE /households/shopping/lists/{id}/recipe/{recipe_slug}` — remove recipe ingredients
- `POST /households/shopping/lists/{id}/label-settings` — update label settings

### Item Endpoints
- `GET /households/shopping/items` — flat item list (paginated, filterable by checked)
- `POST /households/shopping/items` — create single item
- `POST /households/shopping/items/bulk` — **bulk create** (array of items)
- `PUT /households/shopping/items/{id}` — update item
- `POST /households/shopping/items/bulk-update` — **bulk update** (array)
- `DELETE /households/shopping/items/{id}` — delete item
- `DELETE /households/shopping/items` — **bulk delete** (array of IDs in body)

### Core Business Logic (Service Layer)

**Item merging / deduplication:**
- When an item is added, if a matching item exists (same food, same unit, same list), quantities are summed and refs merged
- Controlled by `ShoppingListItem.disable_amounts`

**Recipe linking:**
- `add_recipe_ingredients_to_list` — adds each ingredient to list, creating `ShoppingListItemRecipeReference` rows
- `remove_recipe_ingredients_from_list` — decrements quantities; removes item if qty reaches 0
- Tracks `recipe_id`, `recipe_quantity`, `recipe_note` per reference

**Quantity & unit normalization:**
- Unit conversion before merging (e.g., 2 cups + 1 pint → normalized)
- `disable_amounts` flag bypasses numeric logic

**Event publishing:**
- `ShoppingListUpdated` event fired on create, update, delete, recipe add/remove

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Households/ShoppingListsController.cs`, `ShoppingItemsController.cs`
**Service:** `Mealie.Application/Services/ShoppingLists/ShoppingListService.cs`

### Implemented
- CRUD for lists
- CRUD for single items
- Flat item query (`GET /items` with checked filter)
- Add recipe ingredients to list
- Remove recipe ingredients from list
- Label settings update
- `ShoppingListItemRecipeReference` tracked correctly

### Missing
- **Bulk create items** — `POST /items/bulk` not present
- **Bulk update items** — `POST /items/bulk-update` not present
- **Bulk delete items** — `DELETE /items` with ID array not present
- **Item merging/deduplication** — no merge logic in service; duplicate items accumulate
- **Quantity normalization / unit conversion** — not implemented
- **Domain events** — `ShoppingListUpdated` not published on mutations

---

## Enhancement Opportunities (C#-Specific)

- `IShoppingListMergeService` — extract merge/dedup logic into a dedicated service, testable independently
- Publish `ShoppingListItemsChanged` domain event carrying the diff (added/removed item IDs); webhook handler dispatches to configured URLs
- Use `IUnitConversionService` interface so conversion strategies (simple ratio, Pint library equivalent) can be swapped
- Bulk endpoints can accept `IReadOnlyList<T>` and use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` in EF Core 7+ for efficiency
