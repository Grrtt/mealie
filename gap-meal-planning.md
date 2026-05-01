# Gap Analysis: Meal Planning

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/households/controller_meal_plans.py`
**Service:** `mealie/services/household/meal_plan_service.py`

### Endpoints
- `GET /households/mealplans` — list plans (date range filter)
- `POST /households/mealplans` — create entry
- `GET /households/mealplans/today` — today's plans
- `GET /households/mealplans/random` — get a random recipe ID
- `POST /households/mealplans/random` — create an entry with random recipe
- `GET /households/mealplans/{id}` — get single
- `PUT /households/mealplans/{id}` — update
- `DELETE /households/mealplans/{id}` — delete

### Meal Plan Rules
- `GET /households/mealplans/rules` — list rules
- `POST /households/mealplans/rules` — create rule
- `GET /households/mealplans/rules/{id}` — get rule
- `PUT /households/mealplans/rules/{id}` — update rule
- `DELETE /households/mealplans/rules/{id}` — delete rule

### Business Logic
- Random recipe selection respects rules (tag/category filters)
- Event fired when meal plan entry is created/updated/deleted
- `meal_plan_event_service.py` sends emails/webhooks on plan day

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Households/MealPlansController.cs`, `MealPlanRulesController.cs`
**Service:** `Mealie.Application/Services/MealPlans/MealPlanService.cs`, `MealPlanRuleService.cs`

### Implemented (parity + extras)
- Full CRUD for meal plan entries
- Full CRUD for meal plan rules (tag/category constraints)
- `GET /mealplans/today`
- `POST /mealplans/random`
- **`POST /mealplans/fill-day`** — C# enhancement; not in Python
- **`POST /mealplans/fill-week`** — C# enhancement; not in Python; rules-driven with default fallback
- Random recipe selection with 3-step logic: rules → slug auto-filter → all-recipes fallback

### Missing
- **Domain events** — meal plan create/update/delete do not publish events
- **`meal_plan_event_service` equivalent** — no email/webhook dispatch on plan day
- **`GET /mealplans/random`** — Python exposes a read-only random recipe GET; C# only has POST

---

## Enhancement Opportunities (C#-Specific)

- Publish `MealPlanEntryCreated` / `MealPlanEntryUpdated` / `MealPlanEntryDeleted` domain events; webhook handler dispatches to configured URLs
- `IMealPlanNotificationService` — scheduled `IHostedService` that runs daily, finds today's plans, and sends emails/webhooks (equivalent to Python's `meal_plan_event_service`)
- Fill-week could expose a dry-run mode (`?preview=true`) returning what would be created without persisting
- Rule evaluation order — allow rules to have a priority/weight so more specific rules win over broad ones
