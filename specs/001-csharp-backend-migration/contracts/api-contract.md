# API Contract: Mealie C# Backend

**Phase**: Phase 1 — Design  
**Feature**: `001-csharp-backend-migration`  
**Type**: HTTP REST / OpenAPI 3.x

---

## Contract Principles

1. **All existing API paths are preserved exactly.** The frontend must operate against the new backend without any code changes.
2. **JSON field names use `camelCase`** (matching FastAPI/Pydantic defaults).
3. **OpenAPI spec is available at `/api/openapi.json`** — same as Python backend.
4. **HTTP 422 validation errors** match the Pydantic `ValidationError` shape (see §Shared Schemas).
5. **Pagination** uses the same `page` / `per_page` / `total` / `items` envelope for all list endpoints.

---

## Base URL & Versioning

- API prefix: `/api/` (all endpoints)
- No version prefix (matching Python backend — no `/v1/`)
- OpenAPI document served at: `GET /api/openapi.json`
- Swagger UI (development only): `GET /api/docs`

---

## Authentication Schemes

| Scheme | Header | Notes |
|--------|--------|-------|
| JWT Bearer | `Authorization: Bearer <token>` | Short-lived access token |
| API Key | `Authorization: Bearer <key>` | Long-lived, stored as bcrypt hash |
| OIDC | Cookie / redirect flow | Via `/api/auth/oauth` endpoints |

---

## Shared Schemas

### PaginatedResponse\<T\>
```json
{
  "page": 1,
  "per_page": 50,
  "total": 1234,
  "total_pages": 25,
  "items": [...]
}
```

### ValidationError (HTTP 422)
Matches Pydantic output shape exactly:
```json
{
  "detail": [
    {
      "loc": ["body", "name"],
      "msg": "field required",
      "type": "value_error.missing"
    }
  ]
}
```

### ErrorDetail (HTTP 401/403/404)
```json
{
  "detail": "Not authenticated"
}
```

---

## Endpoint Groups

### Authentication — `/api/auth`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/token` | Username/password login → JWT tokens |
| POST | `/api/auth/token/refresh` | Refresh access token |
| GET | `/api/auth/oauth` | OIDC redirect initiation |
| GET | `/api/auth/oauth/callback` | OIDC callback |

**POST /api/auth/token** request:
```json
{ "username": "string", "password": "string" }
```
Response (HTTP 200):
```json
{
  "access_token": "string",
  "token_type": "bearer",
  "refresh_token": "string"
}
```

---

### Users — `/api/users`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/users/self` | JWT/APIKey | Get current user profile |
| PUT | `/api/users/self` | JWT | Update own profile |
| PUT | `/api/users/self/password` | JWT | Change own password |
| GET | `/api/users/self/api-tokens` | JWT | List API keys |
| POST | `/api/users/self/api-tokens` | JWT | Create API key |
| DELETE | `/api/users/self/api-tokens/{token_id}` | JWT | Delete API key |
| GET | `/api/users/{user_id}` | JWT | Get user by ID (admin or self) |
| PUT | `/api/users/{user_id}/favorites/{slug}` | JWT | Favorite a recipe |
| DELETE | `/api/users/{user_id}/favorites/{slug}` | JWT | Unfavorite a recipe |
| GET | `/api/users/{user_id}/ratings` | JWT | Get user recipe ratings |
| POST | `/api/users/{user_id}/ratings/{slug}` | JWT | Rate a recipe |
| GET | `/api/users/registration` | — | Get registration policy |
| POST | `/api/users/register` | — | Self-registration |
| POST | `/api/users/forgot-password` | — | Password reset request |
| POST | `/api/users/reset-password` | — | Password reset confirm |

---

### Groups — `/api/groups`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/groups/self` | JWT | Get current group |
| PUT | `/api/groups/self` | JWT | Update group (admin) |
| GET | `/api/groups/self/households` | JWT | List households in group |
| GET | `/api/groups/self/members` | JWT | List group members |
| POST | `/api/groups/self/invitations` | JWT | Create invite token |
| DELETE | `/api/groups/self/invitations/{token}` | JWT | Delete invite token |
| GET | `/api/groups/self/invitations` | JWT | List invite tokens |
| GET | `/api/groups/categories` | JWT | List group categories |
| POST | `/api/groups/categories` | JWT | Create category |
| PUT | `/api/groups/categories` | JWT | Update categories (bulk) |
| DELETE | `/api/groups/categories/{item_id}` | JWT | Delete category |
| GET | `/api/groups/labels` | JWT | List labels |
| POST | `/api/groups/labels` | JWT | Create label |
| PUT | `/api/groups/labels/{item_id}` | JWT | Update label |
| DELETE | `/api/groups/labels/{item_id}` | JWT | Delete label |
| GET | `/api/groups/reports` | JWT | List migration/import reports |
| GET | `/api/groups/reports/{report_id}` | JWT | Get report detail |
| DELETE | `/api/groups/reports/{report_id}` | JWT | Delete report |
| POST | `/api/groups/seed/foods` | JWT | Seed foods |
| POST | `/api/groups/seed/units` | JWT | Seed units |
| POST | `/api/groups/migrations` | JWT | Import from external format |

---

### Households — `/api/households`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/households/self` | JWT | Get current household |
| PUT | `/api/households/self` | JWT | Update household |
| GET | `/api/households/self/members` | JWT | List household members |
| GET | `/api/households/self/statistics` | JWT | Household stats |
| GET | `/api/households/self/cookbooks` | JWT | List cookbooks |
| POST | `/api/households/self/cookbooks` | JWT | Create cookbook |
| PUT | `/api/households/self/cookbooks` | JWT | Bulk update cookbooks |
| GET | `/api/households/self/cookbooks/{item_id}` | JWT | Get cookbook |
| PUT | `/api/households/self/cookbooks/{item_id}` | JWT | Update cookbook |
| DELETE | `/api/households/self/cookbooks/{item_id}` | JWT | Delete cookbook |
| GET | `/api/households/self/webhooks` | JWT | List webhooks |
| POST | `/api/households/self/webhooks` | JWT | Create webhook |
| PUT | `/api/households/self/webhooks/{item_id}` | JWT | Update webhook |
| DELETE | `/api/households/self/webhooks/{item_id}` | JWT | Delete webhook |
| POST | `/api/households/self/webhooks/test` | JWT | Test webhook delivery |
| GET | `/api/households/self/shopping/lists` | JWT | List shopping lists |
| POST | `/api/households/self/shopping/lists` | JWT | Create shopping list |
| GET | `/api/households/self/shopping/lists/{item_id}` | JWT | Get shopping list |
| PUT | `/api/households/self/shopping/lists/{item_id}` | JWT | Update shopping list |
| DELETE | `/api/households/self/shopping/lists/{item_id}` | JWT | Delete shopping list |
| POST | `/api/households/self/shopping/lists/{item_id}/recipe/{recipe_id}` | JWT | Add recipe to list |
| DELETE | `/api/households/self/shopping/lists/{item_id}/recipe/{recipe_id}` | JWT | Remove recipe from list |
| GET | `/api/households/self/shopping/items` | JWT | List shopping items |
| POST | `/api/households/self/shopping/items` | JWT | Create shopping item |
| PUT | `/api/households/self/shopping/items/{item_id}` | JWT | Update shopping item |
| DELETE | `/api/households/self/shopping/items/{item_id}` | JWT | Delete shopping item |
| POST | `/api/households/self/shopping/items/bulk-delete` | JWT | Bulk delete items |
| GET | `/api/households/self/meal-plans` | JWT | List meal plans |
| POST | `/api/households/self/meal-plans` | JWT | Create meal plan entry |
| GET | `/api/households/self/meal-plans/today` | JWT | Today's meal plan |
| GET | `/api/households/self/meal-plans/{item_id}` | JWT | Get meal plan entry |
| PUT | `/api/households/self/meal-plans/{item_id}` | JWT | Update meal plan entry |
| DELETE | `/api/households/self/meal-plans/{item_id}` | JWT | Delete meal plan entry |
| GET | `/api/households/self/meal-plans/random` | JWT | Random recipe for slot |
| GET | `/api/households/self/meal-plans/rules` | JWT | List plan rules |
| POST | `/api/households/self/meal-plans/rules` | JWT | Create plan rule |
| PUT | `/api/households/self/meal-plans/rules/{item_id}` | JWT | Update plan rule |
| DELETE | `/api/households/self/meal-plans/rules/{item_id}` | JWT | Delete plan rule |
| GET | `/api/households/self/event-notifications` | JWT | List event notifiers |
| POST | `/api/households/self/event-notifications` | JWT | Create event notifier |
| PUT | `/api/households/self/event-notifications/{item_id}` | JWT | Update event notifier |
| DELETE | `/api/households/self/event-notifications/{item_id}` | JWT | Delete event notifier |
| POST | `/api/households/self/event-notifications/{item_id}/test` | JWT | Test notifier |

---

### Recipes — `/api/recipes`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/recipes` | JWT | List recipes (paginated, filterable) |
| POST | `/api/recipes` | JWT | Create recipe |
| GET | `/api/recipes/summary` | JWT | List recipe summaries |
| GET | `/api/recipes/{slug}` | JWT | Get recipe by slug |
| PUT | `/api/recipes/{slug}` | JWT | Update recipe |
| PATCH | `/api/recipes/{slug}` | JWT | Partial update recipe |
| DELETE | `/api/recipes/{slug}` | JWT | Delete recipe |
| POST | `/api/recipes/{slug}/duplicate` | JWT | Duplicate recipe |
| PUT | `/api/recipes/{slug}/image` | JWT | Update recipe image |
| GET | `/api/recipes/{slug}/timeline` | JWT | Get timeline events |
| POST | `/api/recipes/{slug}/timeline` | JWT | Add timeline event |
| PUT | `/api/recipes/{slug}/timeline/{event_id}` | JWT | Update timeline event |
| DELETE | `/api/recipes/{slug}/timeline/{event_id}` | JWT | Delete timeline event |
| GET | `/api/recipes/{slug}/comments` | JWT | List comments |
| POST | `/api/recipes/{slug}/comments` | JWT | Create comment |
| PUT | `/api/recipes/{slug}/comments/{comment_id}` | JWT | Update comment |
| DELETE | `/api/recipes/{slug}/comments/{comment_id}` | JWT | Delete comment |
| GET | `/api/recipes/{slug}/assets` | JWT | List assets |
| POST | `/api/recipes/{slug}/assets` | JWT | Upload asset |
| DELETE | `/api/recipes/{slug}/assets/{file_name}` | JWT | Delete asset |
| GET | `/api/recipes/{slug}/share` | JWT | List share tokens |
| POST | `/api/recipes/{slug}/share` | JWT | Create share token |
| DELETE | `/api/recipes/{slug}/share/{token_id}` | JWT | Delete share token |
| GET | `/api/recipes/exports` | JWT | List export types |
| GET | `/api/recipes/{slug}/exports` | JWT | Export single recipe |
| POST | `/api/recipes/create-url` | JWT | Scrape recipe from URL |
| POST | `/api/recipes/create-url/bulk` | JWT | Bulk scrape from URLs |
| POST | `/api/recipes/create-zip` | JWT | Import from zip |
| POST | `/api/recipes/create-image-ocr` | JWT | Create from OCR image |
| POST | `/api/recipes/bulk-actions/delete` | JWT | Bulk delete |
| POST | `/api/recipes/bulk-actions/tag` | JWT | Bulk tag |
| POST | `/api/recipes/bulk-actions/categorize` | JWT | Bulk categorize |
| POST | `/api/recipes/bulk-actions/export` | JWT | Bulk export |
| POST | `/api/recipes/bulk-actions/undelete` | JWT | Bulk restore from export |
| GET | `/api/recipes/shared/{token_id}` | — | Access shared recipe |

**GET /api/recipes — Query Parameters**:
```
page          int     default=1
perPage       int     default=50
orderBy       string  e.g. "name", "date_added", "rating"
orderDirection string  "asc" | "desc"
filterString  string  Mealie query filter DSL
groupId       uuid    
householdId   uuid
requireAllCategories bool
requireAllTags bool
cookbook      uuid    filter by cookbook
categories    uuid[]  
tags          uuid[]
tools         uuid[]
foods         uuid[]
search        string  full-text search
```

---

### Organizers

#### Tags — `/api/organizers/tags`
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/organizers/tags` | List all tags |
| POST | `/api/organizers/tags` | Create tag |
| GET | `/api/organizers/tags/{item_id}` | Get tag |
| PUT | `/api/organizers/tags/{item_id}` | Update tag |
| DELETE | `/api/organizers/tags/{item_id}` | Delete tag |
| GET | `/api/organizers/tags/slug/{tag_slug}` | Get tag by slug |

#### Categories — `/api/organizers/categories`
Same CRUD pattern as Tags.

#### Tools — `/api/organizers/tools`
Same CRUD pattern as Tags.

---

### Foods & Units — `/api/foods`, `/api/units`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/foods` | List foods (paginated) |
| POST | `/api/foods` | Create food |
| GET | `/api/foods/{food_id}` | Get food |
| PUT | `/api/foods/{food_id}` | Update food |
| DELETE | `/api/foods/{food_id}` | Delete food |
| POST | `/api/foods/{food_id}/merge` | Merge food into another |
| GET | `/api/units` | List units (paginated) |
| POST | `/api/units` | Create unit |
| GET | `/api/units/{unit_id}` | Get unit |
| PUT | `/api/units/{unit_id}` | Update unit |
| DELETE | `/api/units/{unit_id}` | Delete unit |
| POST | `/api/units/{unit_id}/merge` | Merge unit into another |

---

### Parser — `/api/parser`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/parser/ingredient` | Parse single ingredient string |
| POST | `/api/parser/ingredients` | Parse multiple ingredient strings |

**POST /api/parser/ingredient** request:
```json
{ "ingredient": "2 cups all-purpose flour", "addPortionSize": false }
```
Response:
```json
{
  "input": "2 cups all-purpose flour",
  "ingredient": {
    "quantity": 2.0,
    "unit": { "name": "cup", ... },
    "food": { "name": "all-purpose flour", ... },
    "note": null,
    "originalText": "2 cups all-purpose flour"
  },
  "confidence": "high"
}
```

---

### Explore (Public) — `/api/explore`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/explore/groups/{group_slug}` | — | Public group info |
| GET | `/api/explore/groups/{group_slug}/recipes` | — | Public recipe list |
| GET | `/api/explore/groups/{group_slug}/recipes/{recipe_slug}` | — | Public recipe |
| GET | `/api/explore/groups/{group_slug}/cookbooks` | — | Public cookbooks |
| GET | `/api/explore/groups/{group_slug}/cookbooks/{item_id}` | — | Public cookbook |
| GET | `/api/explore/groups/{group_slug}/foods` | — | Public foods list |
| GET | `/api/explore/groups/{group_slug}/tags` | — | Public tags |
| GET | `/api/explore/groups/{group_slug}/categories` | — | Public categories |
| GET | `/api/explore/groups/{group_slug}/tools` | — | Public tools |

---

### Admin — `/api/admin`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admin/about` | Instance info / version / config |
| GET | `/api/admin/statistics` | User/recipe/group counts |
| GET | `/api/admin/users` | List all users |
| POST | `/api/admin/users` | Create user (admin) |
| GET | `/api/admin/users/{user_id}` | Get user |
| PUT | `/api/admin/users/{user_id}` | Update user |
| DELETE | `/api/admin/users/{user_id}` | Delete user |
| GET | `/api/admin/groups` | List all groups |
| POST | `/api/admin/groups` | Create group |
| GET | `/api/admin/groups/{group_id}` | Get group |
| PUT | `/api/admin/groups/{group_id}` | Update group |
| DELETE | `/api/admin/groups/{group_id}` | Delete group |
| GET | `/api/admin/households` | List all households |
| POST | `/api/admin/households` | Create household |
| GET | `/api/admin/households/{household_id}` | Get household |
| PUT | `/api/admin/households/{household_id}` | Update household |
| DELETE | `/api/admin/households/{household_id}` | Delete household |
| GET | `/api/admin/email` | Get email configuration status |
| POST | `/api/admin/email` | Test email configuration |
| GET | `/api/admin/backups` | List backups |
| POST | `/api/admin/backups` | Create backup |
| GET | `/api/admin/backups/{file_name}` | Download backup |
| DELETE | `/api/admin/backups/{file_name}` | Delete backup |
| POST | `/api/admin/backups/restore` | Restore from backup |
| GET | `/api/admin/debug` | Debug info |
| GET | `/api/admin/debug/statistics` | DB statistics |
| POST | `/api/admin/debug/statistics/calculate` | Recalculate statistics |

---

### Utility & Health

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/app/about` | — | Public instance info |
| GET | `/api/app/about/oidc` | — | OIDC configuration for frontend |
| GET | `/api/debug/version` | — | Version info |
| GET | `/healthz` | — | Health check (liveness) |
| GET | `/readyz` | — | Readiness check |

**GET /healthz** response (HTTP 200):
```json
{
  "status": "ok",
  "version": "2.0.0",
  "database": "connected"
}
```

---

### Media — Static Files

| Pattern | Description |
|---------|-------------|
| `/api/media/recipes/{recipe_id}/images/{file_name}` | Recipe images |
| `/api/media/recipes/{recipe_id}/assets/{file_name}` | Recipe assets |
| `/api/media/users/{user_id}/images/{file_name}` | User avatars |
| `/api/media/groups/{group_id}/images/{file_name}` | Group images |

Served by ASP.NET Core static file middleware from `DATA_DIR/`.

---

### OpenAI Integration — `/api/openai`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/openai/parse-ingredient` | JWT | AI ingredient parsing |
| POST | `/api/openai/parse-recipe` | JWT | AI recipe parsing from text |

Returns HTTP 424 (Failed Dependency) with `{"detail": "OpenAI is not configured"}` when `OPENAI_API_KEY` is absent — **not** a 500.

---

## Error Response Matrix

| Condition | HTTP Status | Body Shape |
|-----------|------------|------------|
| Unauthenticated | 401 | `{"detail": "Not authenticated"}` |
| Forbidden (wrong household) | 404 | `{"detail": "Not found"}` (resource existence not leaked) |
| Forbidden (insufficient role) | 403 | `{"detail": "Not enough permissions"}` |
| Not found | 404 | `{"detail": "Not found"}` |
| Validation failure | 422 | Pydantic `ValidationError` shape |
| Duplicate (unique constraint) | 422 | `{"detail": "..."}` |
| LDAP server unreachable | 401 | `{"detail": "LDAP server unavailable"}` |
| JS-rendering site scraped | 200 | Recipe object with `scraping_not_supported=true` |
| OpenAI not configured | 424 | `{"detail": "OpenAI is not configured"}` |
| Disk full during backup | 500 | `{"detail": "Backup failed: disk space exhausted"}` + Serilog Error |
| Server error | 500 | `{"detail": "Internal server error"}` |

---

## OpenAPI Compatibility Validation

The C# backend's `/api/openapi.json` must be diffed against the Python backend's output. Acceptance is:
- Zero missing paths
- Zero removed required fields
- Zero field type changes
- Zero status code changes

CI job: `scripts/validate-openapi-compat.sh` (generates both specs, runs `swagger-diff` or equivalent).
