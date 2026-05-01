# Gap Analysis: Explore (Public / Unauthenticated)

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/explore/`

All endpoints are unauthenticated but scoped to a public household/group.

### Endpoints
- `GET /explore/households/{household_slug}` — public household profile
- `GET /explore/households/{household_slug}/recipes` — paginated public recipes
- `GET /explore/households/{household_slug}/recipes/{recipe_slug}` — single public recipe
- `GET /explore/households/{household_slug}/cookbooks` — public cookbooks
- `GET /explore/households/{household_slug}/cookbooks/{id}` — single cookbook with recipes
- `GET /explore/households/{household_slug}/foods` — public foods list
- `GET /explore/households/{household_slug}/tags` — public tags
- `GET /explore/households/{household_slug}/categories` — public categories
- `GET /explore/households/{household_slug}/tools` — public tools

### Visibility Rules
- Household must have `allow_recipe_sharing = true`
- Individual recipes must have `is_public = true`
- Cookbooks must have `is_public = true`

### Shared Recipe Access
- `GET /shared/recipes/{token_id}` — access a recipe via share token (any recipe, regardless of household public setting)

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Explore/`

### Implemented
- Explore controller exists
- Public recipe endpoints likely present (used by frontend)

### Missing / Uncertain
- **Public foods/tags/categories/tools** — likely not implemented
- **Public cookbook endpoint** — unclear
- **Household slug routing** — unclear if household is resolved by slug or ID
- **Visibility enforcement** — unclear if `allow_recipe_sharing` and `is_public` flags are checked on every explore endpoint
- **Share token validation** — `GET /shared/recipes/{token_id}` behavior and token expiry enforcement unclear

---

## Enhancement Opportunities (C#-Specific)

- Explore endpoints should use `IOutputCache` (ASP.NET Core 7+) with a short TTL since they're public and read-heavy
- `IPublicVisibilityFilter` — a query extension method that applies `where r.IsPublic && h.AllowRecipeSharing` in one place across all explore queries
- Share token: store `ExpiresAt` (nullable), validate in `IShareTokenService`; return 410 Gone for expired tokens vs 404 for invalid
- Public endpoints should never expose internal IDs (household ID, group ID) — use slugs only in all public response DTOs
