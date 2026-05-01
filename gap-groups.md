# Gap Analysis: Groups

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/groups/`
**Services:** `mealie/services/group/`

### Group Self-Management
- `GET /groups/self` — current group info
- `PUT /groups/self` — update group settings
- `GET /groups/members` — list members
- `PUT /groups/members/{id}` — update member role
- `DELETE /groups/members/{id}` — remove member

### Household Invitations
- `GET /groups/invitations` — list invite tokens
- `POST /groups/invitations` — generate invite token
- `DELETE /groups/invitations/{id}` — revoke token
- `POST /groups/invitations/email` — send invite by email

### Labels (Multi-purpose)
- `GET /groups/labels` — list labels
- `POST /groups/labels` — create label
- `GET /groups/labels/{id}` — get label
- `PUT /groups/labels/{id}` — update label
- `DELETE /groups/labels/{id}` — delete label

### Data Export
- `GET /groups/exports` — list available exports
- `POST /groups/exports` — create export (async)
- `GET /groups/exports/{token}` — download export file
- `DELETE /groups/exports/{token}` — delete export

### Migrations (Import from other apps)

Supported formats:
| Format | Notes |
|---|---|
| Chowdown | Jekyll-based recipe blog |
| CopyMeThat | Web scraper export |
| Mealie Alpha | Legacy Mealie v0 format |
| Nextcloud Cookbook | Nextcloud app export |
| Paprika | Paprika 3 export ZIP |
| Tandoor | Tandoor Recipes export |
| PlanToEat | PlanToEat export |
| MyRecipeBox | MyRecipeBox export |
| RecipeKeeper | RecipeKeeper export |
| Cookn | Cookn export |

**Endpoints:**
- `GET /groups/migrations` — list previous migration reports
- `POST /groups/migrations` — upload & run migration (multipart file + type)
- `GET /groups/migrations/{id}` — get migration report

### Seeder (Data initialization)
- `POST /groups/seeders/foods` — seed ingredient foods for a locale
- `POST /groups/seeders/units` — seed units for a locale
- `POST /groups/seeders/labels` — seed shopping labels for a locale

### Reports
- `GET /groups/reports` — list reports (migration, export results)
- `GET /groups/reports/{id}` — get report with item details

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Groups/`
**Services:** `Mealie.Application/Services/Groups/`

### Implemented
- `GET /groups/self`, `PUT /groups/self`
- Labels CRUD
- Seeders: foods, units, labels (queued via `IBackgroundTaskQueue`)
- Exports: list, create (queued), download, delete

### Missing
- **Member management** — no list/update/remove member endpoints
- **Household invitations** — no invite token CRUD or email sending
- **Migrations** — queue entry created but no migration processors implemented; 0 of 10 formats supported
- **Migration reports** — no report tracking or retrieval
- **Group reports** — no reports endpoint
- **Seeder execution** — queued but unclear if background worker processes them
- **`POST /groups/invitations/email`** — no email invite sending

---

## Enhancement Opportunities (C#-Specific)

- `IMigrationHandler` interface with one implementation per format; `IMigrationHandlerFactory` resolves by format name — clean strategy pattern
- `IMigrationReportService` — persist migration results (successes, failures, skipped) to a `group_reports` table; expose via API
- Invitation tokens: generate a cryptographically random token, store expiry, validate on registration — use `IInvitationService`
- Seeder background worker: use `IHostedService` to drain the `IBackgroundTaskQueue` and execute seeder logic; publish progress events
- Export: use streaming ZIP response rather than temp files; attach download token to an expiring signed URL
