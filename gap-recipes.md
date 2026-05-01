# Gap Analysis: Recipes

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/recipe/`
**Services:** `mealie/services/recipe/`

### Endpoints
- Full CRUD: GET, POST, PUT, PATCH, DELETE
- `GET /recipes/{slug}` — fetch by slug
- `GET /recipes/{slug}/timeline` — timeline events
- `POST /recipes/{slug}/timeline` — add event
- `POST /recipes/{slug}/assets` — upload asset file
- `DELETE /recipes/{slug}/assets/{file_name}` — remove asset
- `GET /recipes/{slug}/share` — list share tokens
- `POST /recipes/{slug}/share` — create share token
- `DELETE /recipes/{slug}/share/{token_id}` — revoke token
- `GET /recipes/{slug}/comments` — list comments
- `POST /recipes/{slug}/comments` — add comment
- `PUT /recipes/{slug}/comments/{id}` — edit comment
- `DELETE /recipes/{slug}/comments/{id}` — delete comment
- `PUT /recipes/{slug}/image` — replace image
- `POST /recipes/{slug}/image` — upload image
- `DELETE /recipes/{slug}/image/{file_name}` — delete image
- `GET /recipes/suggestions` — AI-powered recipe suggestions
- `GET /recipes/last-made` — recently cooked recipes
- `POST /recipes/bulk-actions/tag` — bulk tag
- `POST /recipes/bulk-actions/categorize` — bulk categorize
- `POST /recipes/bulk-actions/delete` — bulk delete
- `POST /recipes/bulk-actions/export` — bulk export to ZIP
- `POST /recipes/bulk-actions/import` — import from ZIP

### Scraper Endpoints
- `POST /recipes/scrape/url` — scrape from URL (streams SSE progress)
- `POST /recipes/scrape/url/bulk` — bulk scrape URLs
- `POST /recipes/scrape/html` — scrape from raw HTML
- `POST /recipes/scrape/json-ld` — parse JSON-LD
- `POST /recipes/test-scrape/url` — test scrape (no save)

### Business Logic
- Event published on create, update, delete
- Last-made date tracking updated when meal plan is marked done
- Timeline images stored alongside events
- Share token expiration enforced
- Bulk export writes ZIP asynchronously with progress

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Recipes/`

### Implemented
- Full CRUD
- Image upload/replace/delete
- Asset upload/delete
- Recipe share tokens (create, list, delete)
- Comments CRUD
- Timeline events (list, add)
- Scrape from URL (SSE streaming)
- Scrape from HTML
- Scrape from JSON-LD
- Test scrape
- Bulk scrape URLs
- Bulk tag, categorize, delete

### Missing / Incomplete
- **ZIP import** — endpoint exists (`POST /bulk-actions/import`) but import logic not implemented in service
- **ZIP export** — endpoint exists (`POST /bulk-actions/export`) but export logic not implemented
- **Timeline event image upload** — stubbed, not functional
- **Recipe suggestions** — not implemented
- **Last-made tracking** — not wired into meal plan completion
- **Domain events not published** — recipe create/update/delete do not fire events to the event bus

---

## Enhancement Opportunities (C#-Specific)

- Publish `RecipeCreated`, `RecipeUpdated`, `RecipeDeleted` domain events from `RecipeService`; handlers dispatch to webhooks
- Use `IFormFile` pipeline with image processing middleware (resize, convert to WebP) on upload
- Background job (via `IHostedService` or Hangfire) for bulk export/import instead of blocking the request
- `IRecipeSlugValidator` service to enforce unique slug generation with group scope
