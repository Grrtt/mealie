# Gap Analysis Summary

Quick-reference index of all gap analysis files and their priority.

| File | Domain | Completeness | Priority |
|---|---|---|---|
| [gap-authentication.md](gap-authentication.md) | Auth & Users | ~50% | 🔴 High |
| [gap-admin.md](gap-admin.md) | Admin | ~40% | 🔴 High |
| [gap-groups.md](gap-groups.md) | Groups | ~30% | 🔴 High |
| [gap-webhooks-events.md](gap-webhooks-events.md) | Webhooks & Events | ~40% | 🔴 High |
| [gap-shopping-lists.md](gap-shopping-lists.md) | Shopping Lists | ~60% | 🟠 Medium |
| [gap-scraper-importer.md](gap-scraper-importer.md) | Scraper / Importer | ~60% | 🟠 Medium |
| [gap-households.md](gap-households.md) | Households | ~75% | 🟠 Medium |
| [gap-recipes.md](gap-recipes.md) | Recipes | ~80% | 🟠 Medium |
| [gap-parser.md](gap-parser.md) | Ingredient Parser | ~60% | 🟠 Medium |
| [gap-media.md](gap-media.md) | Media & Images | ~60% | 🟠 Medium |
| [gap-organizers.md](gap-organizers.md) | Organizers | ~80% | 🟡 Low |
| [gap-units-foods.md](gap-units-foods.md) | Units & Foods | ~75% | 🟡 Low |
| [gap-explore.md](gap-explore.md) | Explore (Public) | ~70% | 🟡 Low |
| [gap-meal-planning.md](gap-meal-planning.md) | Meal Planning | ~90% + extras | ✅ Good |

## C# Enhancements Not in Python

These are structural improvements the C# backend has introduced or could introduce that don't exist in the Python backend:

- **Fill Day / Fill Week** — rules-driven meal plan population endpoints
- **Admin log cleaning** — maintenance endpoint for rotating log files
- **Search index rebuild** — force reindex of recipe full-text search
- **Domain events architecture** — once wired, allows clean decoupling of side-effects (webhooks, notifications, emails) from service logic
- **`IBackgroundTaskQueue`** — structured async task queue for seeders, migrations, bulk scrapes
- **Streaming ZIP responses** — export without writing temp files (not yet implemented but the .NET APIs make it easy)
- **Output caching** — explore/public endpoints benefit from `IOutputCache` middleware
- **Rate limiting** — ASP.NET Core built-in rate limiting on auth endpoints

## Critical Missing Wiring (Quick Wins)

These are things that exist in C# structurally but are not connected:

1. **Event dispatch not called from services** — `EventNotifierService` exists but Recipe/ShoppingList/MealPlan services never call it
2. **Seeder background worker** — tasks are queued but the worker that drains the queue may not be running
3. **Migration processor** — queue entries created for group migrations but no handler processes them
4. **OpenAI parser not integrated** — exists in its own controller, not wired as a strategy in the ingredient parser
