# Gap Analysis: Households

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/households/`

### Household Self
- `GET /households/self` — current household info
- `PUT /households/self` — update name/settings
- `GET /households/self/members` — list members with roles
- `PUT /households/self/members/{id}` — update member household role

### Preferences
- `GET /households/self/preferences` — get household preferences
- `PUT /households/self/preferences` — update preferences (default meal type, public visibility, etc.)

### Cookbooks
- `GET /households/cookbooks` — list cookbooks
- `POST /households/cookbooks` — create cookbook
- `PUT /households/cookbooks/{id}` — update cookbook (filters, name)
- `DELETE /households/cookbooks/{id}` — delete cookbook
- `GET /households/cookbooks/{id}/recipes` — get recipes in cookbook

### Webhooks
- `GET /households/webhooks` — list webhooks
- `POST /households/webhooks` — create webhook
- `GET /households/webhooks/{id}` — get webhook
- `PUT /households/webhooks/{id}` — update webhook
- `DELETE /households/webhooks/{id}` — delete webhook
- `POST /households/webhooks/{id}/test` — fire a test event
- `POST /households/webhooks/rerun` — rerun webhooks for a date

### Event Notifiers
- `GET /households/notifiers` — list notifiers (Apprise URLs)
- `POST /households/notifiers` — create notifier
- `GET /households/notifiers/{id}` — get notifier
- `PUT /households/notifiers/{id}` — update notifier
- `DELETE /households/notifiers/{id}` — delete notifier
- `POST /households/notifiers/{id}/test` — test notifier

### Shopping (see gap-shopping-lists.md)

### Meal Plans (see gap-meal-planning.md)

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Households/`

### Implemented
- Household self: get, update
- Preferences: get, update
- Cookbooks: full CRUD + recipes endpoint
- Webhooks: CRUD + test
- Event notifiers: CRUD + test
- Shopping lists: CRUD + recipe add/remove (see gap-shopping-lists.md)
- Meal plans: CRUD + fill-day/fill-week (see gap-meal-planning.md)

### Missing
- **Member management** — no list/update household members endpoint
- **Webhook rerun** — `POST /webhooks/rerun` not implemented
- **Cookbook recipe filtering** — cookbook filter criteria (tags, categories, tools) may not be applied when fetching cookbook recipes

---

## Enhancement Opportunities (C#-Specific)

- Webhook rerun: `POST /webhooks/rerun?date=YYYY-MM-DD` — look up that day's meal plan entries and re-fire webhooks; useful for testing automations
- `ICookbookQueryService` — builds a dynamic LINQ query from the cookbook's stored filter criteria (tags, categories, tools, require images) rather than returning all household recipes
- Member role changes should publish a `HouseholdMemberRoleChanged` event for audit trail
- Preferences update should validate that `defaultHouseholdMealPlanDay` is a valid day-of-week string before persisting
