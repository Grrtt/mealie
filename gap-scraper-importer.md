# Gap Analysis: Recipe Scraper / Importer

## Python (FastAPI) — Full Feature Set

**Service:** `mealie/services/scraper/`

### Scrape Strategies

| Strategy | Endpoint | Description |
|---|---|---|
| URL scrape | `POST /recipes/scrape/url` | Fetches URL, extracts recipe via JSON-LD or site-specific scrapers |
| URL bulk | `POST /recipes/scrape/url/bulk` | Queues multiple URL scrapes |
| HTML scrape | `POST /recipes/scrape/html` | Parse raw HTML string |
| JSON-LD | `POST /recipes/scrape/json-ld` | Parse raw JSON-LD object |
| Test scrape | `POST /recipes/test-scrape/url` | Returns parsed data without saving |

### URL Scraping
- Powered by `recipe-scrapers` Python library — supports 200+ websites natively
- Falls back to generic JSON-LD extraction if no site-specific scraper exists
- On completion, fires `RecipeCreated` event

### ZIP Import
- `POST /recipes/bulk-actions/import` — upload a ZIP containing Mealie-exported recipes (JSON + images)
- Parsed and inserted in background task
- Supports re-import (update existing by slug)

### ZIP / JSON Export
- `POST /recipes/bulk-actions/export` — creates a ZIP of selected recipes with images
- `GET /recipes/{slug}/zip` — single-recipe ZIP download

### Scrape Result Normalization
- Quantities normalized (fractions → decimals)
- Units matched to group DB
- Foods matched to group DB
- Images downloaded and stored locally
- Ingredient strings parsed via NLP parser

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Recipes/`

### Implemented
- URL scrape (SSE streaming)
- HTML scrape
- JSON-LD scrape
- Test scrape
- Bulk URL scrape (queued)

### Missing
- **`recipe-scrapers` equivalent** — Python library has 200+ site-specific scrapers; C# calls an external scraper or uses generic JSON-LD only; site-specific support unknown
- **ZIP import** — endpoint exists but service logic not implemented
- **Single-recipe ZIP download** — not present
- **ZIP export** — endpoint exists but service logic not implemented
- **Post-scrape normalization** — unit/food DB matching unclear
- **Image download on scrape** — unclear if scraped image URLs are fetched and stored locally

---

## Enhancement Opportunities (C#-Specific)

- Call the Python `recipe-scrapers` as a sidecar service via `HttpClient` to retain 200+ site coverage while building a native C# equivalent incrementally
- `IRecipeImportService` — handles ZIP extraction using `System.IO.Compression`, deserializes recipe JSON, calls `IRecipeService.CreateAsync` for each, downloads images
- `IRecipeExportService` — streams a ZIP to the response using `ZipArchive` over `Response.Body`; no temp file needed
- Post-scrape normalization pipeline: `IIngredientNormalizationPipeline` with steps: parse string → match food → match unit → fallback to raw
- Store scrape source URL in recipe `extras` or a dedicated `source_url` column for deduplication (prevent importing the same recipe twice)
