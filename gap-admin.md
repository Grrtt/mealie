# Gap Analysis: Admin

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/admin/`

### User Management
- `GET /admin/users` — paginated user list
- `POST /admin/users` — create user
- `GET /admin/users/{id}` — get user
- `PUT /admin/users/{id}` — update user (including group/household reassignment)
- `DELETE /admin/users/{id}` — delete user
- `PUT /admin/users/{id}/reset-password` — force password reset

### Group Management
- `GET /admin/groups` — list groups
- `POST /admin/groups` — create group
- `GET /admin/groups/{id}` — get group
- `PUT /admin/groups/{id}` — update group
- `DELETE /admin/groups/{id}` — delete group

### Household Management
- `GET /admin/households` — list households
- `POST /admin/households` — create household
- `GET /admin/households/{id}` — get household
- `PUT /admin/households/{id}` — update household
- `DELETE /admin/households/{id}` — delete household

### Backup & Restore
- `GET /admin/backups` — list backups
- `POST /admin/backups` — create backup (full ZIP)
- `GET /admin/backups/{file_name}` — download backup
- `DELETE /admin/backups/{file_name}` — delete backup
- `POST /admin/backups/{file_name}/restore` — restore from backup

### Maintenance
- `GET /admin/maintenance` — storage statistics
- `POST /admin/maintenance/clean/images` — remove orphaned images
- `POST /admin/maintenance/clean/temp` — clear temp folder
- `POST /admin/maintenance/clean/recipe-folders` — remove orphaned recipe folders
- `POST /admin/maintenance/storage-details` — disk usage breakdown

### Email
- `GET /admin/email` — email config status
- `POST /admin/email/test` — send test email

### Analytics / Statistics
- `GET /admin/analytics` — aggregate counts (users, groups, recipes, etc.)

### Debug
- `GET /admin/debug` — server info, version, config flags
- `GET /admin/debug/openapi` — OpenAPI schema info

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Admin/`

### Implemented
- Email config: get, test
- Maintenance: clean images, temp, recipe folders
- **Log cleaning** — C# addition not in Python
- **Search index rebuild** — C# addition not in Python

### Missing
- **User management** — no admin user CRUD
- **Group management** — no admin group CRUD
- **Household management** — no admin household CRUD
- **Backup & restore** — no implementation
- **Analytics/statistics** — no endpoint
- **Server debug info** — not present
- **Maintenance storage details** — not implemented

---

## Enhancement Opportunities (C#-Specific)

- Backup as a streaming ZIP using `System.IO.Compression.ZipArchive` written to response stream — no temp file needed
- `IAdminAnalyticsService` — single query using EF Core `GroupBy` projections for counts across users, groups, recipes, shopping lists
- `IBackupScheduler` — `IHostedService` that runs nightly backups and prunes old ones beyond a configurable retention count
- Structured audit log: admin actions (user delete, group create, etc.) written to a dedicated `admin_audit_log` table with actor, action, target, timestamp
- Admin dashboard stats endpoint can leverage read replicas or `AsNoTracking()` for performance
