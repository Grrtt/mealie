# Gap Analysis: Organizers (Tags, Categories, Tools)

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/organizers/`

Each organizer type (tags, categories, tools) exposes:
- `GET /{type}` — paginated list with search/filter
- `POST /{type}` — create
- `GET /{type}/{id}` — get by ID
- `PUT /{type}/{id}` — update
- `DELETE /{type}/{id}` — delete
- `GET /{type}/slug/{slug}` — lookup by slug
- `GET /{type}/{id}/recipes` — recipes using this organizer

Categories additionally support:
- Hierarchy / nesting (parent-child relationships stored in `recipe_category` table)

Tools additionally support:
- `on_hand` flag — mark a tool as currently available

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Organizers/`

### Implemented
- Tags: full CRUD
- Categories: full CRUD
- Tools: full CRUD including `on_hand`

### Missing / Uncertain
- **Slug-based lookup** — `GET /{type}/slug/{slug}` may not be present
- **Recipes-by-organizer** — `GET /{type}/{id}/recipes` endpoint unclear
- **Category hierarchy** — parent/child nesting not visible in domain entity or config
- **Pagination + search** — present in Python list endpoints; unclear if C# list endpoints are paginated or searchable
- **`on_hand` for tools** — present in Python but verify it is stored and returned in C# responses

---

## Enhancement Opportunities (C#-Specific)

- Slug lookup is useful for frontend navigation (URLs use slugs); add `GET /organizers/tags/slug/{slug}` etc.
- Category nesting: add a `ParentId` FK to `Category` entity and EF config; expose `children` in response DTO
- Organizer merge endpoint: `POST /organizers/tags/{id}/merge/{targetId}` — reassign all recipes from source tag to target, then delete source (useful for de-duplication)
- Organizer usage count: include `recipeCount` in list response using a left join projection; avoids N+1 when rendering the organizer management UI
