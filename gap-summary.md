# Gap Analysis Summary

Quick-reference index of all gap analysis files and their priority.  Updated 2026-06-18.

> **Note:** The individual `gap-*.md` files are snapshots from the initial port assessment and many
> of their findings have since been resolved.  This summary reflects the current state; individual
> files may be stale.

| File | Domain | Completeness | Priority |
|---|---|---|---|
| [gap-authentication.md](gap-authentication.md) | Auth & Users | ~90% | 🟡 Low |
| [gap-admin.md](gap-admin.md) | Admin | ~85% | 🟢 Done |
| [gap-groups.md](gap-groups.md) | Groups | ~85% | 🟢 Done |
| [gap-webhooks-events.md](gap-webhooks-events.md) | Webhooks & Events | ~85% | 🟡 Low |
| [gap-shopping-lists.md](gap-shopping-lists.md) | Shopping Lists | ~85% | 🟢 Done |
| [gap-scraper-importer.md](gap-scraper-importer.md) | Scraper / Importer | ~60% | 🟠 Medium |
| [gap-households.md](gap-households.md) | Households | ~85% | 🟢 Done |
| [gap-recipes.md](gap-recipes.md) | Recipes | ~90% | 🟢 Done |
| [gap-parser.md](gap-parser.md) | Ingredient Parser | ~85% | 🟢 Done |
| [gap-media.md](gap-media.md) | Media & Images | ~85% | 🟡 Low |
| [gap-organizers.md](gap-organizers.md) | Organizers | ~80% | 🟡 Low |
| [gap-units-foods.md](gap-units-foods.md) | Units & Foods | ~85% | 🟢 Done |
| [gap-explore.md](gap-explore.md) | Explore (Public) | ~85% | 🟢 Done |
| [gap-meal-planning.md](gap-meal-planning.md) | Meal Planning | ~90% + extras | ✅ Good |

## Recently Resolved (2026-06-18)

- **OCR image import** — Full SSE-streaming endpoint with AI vision (OpenAI/Azure/Ollama/custom). Upload an image, extract recipe text, create recipe with `IsOcr=true`.
- **User signup webhook** — `UserSignedUpEvent` is now published from `RegistrationService` so Apprise notifiers fire on new user registration.
- **Event notifier test endpoint** — `POST .../test` now actually sends an HTTP POST with a `{event_type: "test"}` payload instead of just logging.
- **Media authorization** — `MediaController` no longer serves private recipe images anonymously. Public recipe images remain accessible; private recipe images, user profile images, and assets require authentication.

## Known Gaps (Real, Verified)

### Import formats — 5 of 10 ported
Only these migration parsers exist in C#: Chowdown, Paprika, Nextcloud Cookbook, Tandoor, Mealie Backup.
Not ported: CopyMeThat, Mealie Alpha, PlanToEat, MyRecipeBox, RecipeKeeper, Cookn.
This was not specified as a requirement for the C# migration.

### Apprise non-HTTP protocol URLs (by design)
Only `http://` and `https://` Apprise URLs are delivered. Protocol URLs (`slack://`, `discord://`, etc.) are explicitly skipped because there is no practical C# equivalent of the Python Apprise library. The intended architecture is to point Apprise URLs at an Apprise API server.

### Streaming ZIP responses
Not yet implemented — the `gap-summary.md` "C# Enhancements" section flags this. Export currently uses buffered approach.

### Token-signed media URLs
The `gap-media.md` spec for `IMediaTokenService` with HMAC-SHA256 signing was not implemented. Instead, media auth uses a simpler approach: check if the recipe is public/household is non-private for anonymous access; require authentication otherwise. Token-signed URLs (which don't require cookies) may still be desirable for shared recipe images loaded via `<img>` tags in emails.

## C# Enhancements Over Python

These structural improvements exist in the C# backend that the Python backend lacked:

- **Fill Day / Fill Week** — rules-driven meal plan population endpoints
- **Admin log cleaning** — maintenance endpoint for rotating log files
- **Search index rebuild** — force reindex of recipe full-text search (Lucene)
- **Domain events architecture (MediatR)** — fully wired; decouples search indexing, cache invalidation, timeline events, and Apprise webhooks from service logic
- **Background services** — `MigrationBackgroundService`, `SeedBackgroundService`, `ImageScrapeBackgroundService` drain structured channels
- **Output caching** — explore/public endpoints benefit from `IOutputCache` middleware
- **Rate limiting** — ASP.NET Core built-in rate limiting on auth endpoints
- **OCR via AI vision** — recipe creation from photo (new capability, not in Python)

## Critical Missing Wiring (Previously Listed — All Resolved)

~~1. Event dispatch not called from services~~ — Services publish via `IMediator`. All events including `UserSignedUpEvent` are now published.
~~2. Seeder background worker~~ — `SeedBackgroundService` is registered and running.
~~3. Migration processor~~ — `MigrationBackgroundService` is registered and processing the queue.
~~4. OpenAI parser not integrated~~ — Strategy pattern fully wired: `OpenAiParserStrategy`, `AzureOpenAiParserStrategy`, `OllamaParserStrategy`, `CustomAiParserStrategy`.
