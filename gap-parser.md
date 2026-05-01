# Gap Analysis: Ingredient Parser

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/parser/`
**Service:** `mealie/services/scraper/`

### Endpoints
- `POST /parser/ingredient` — parse a single ingredient string
- `POST /parser/ingredients` — batch parse (array of strings)

### Parsing Strategies
| Strategy | Description |
|---|---|
| `nlp` | Default; uses a trained NLP model (`crfsuite`) to extract quantity, unit, food, note |
| `brute` | Regex-based fallback when NLP confidence is low |
| `openai` | Optional; sends ingredient string to GPT for structured extraction |

### Output
Each parsed ingredient returns:
- `quantity` (float)
- `unit` (matched to DB unit or raw string)
- `food` (matched to DB food or raw string)
- `note` (preparation notes, e.g. "finely chopped")
- `original` (input string)
- `confidence` (NLP model score)

### Food & Unit Matching
- After NLP extraction, food name is matched against the group's `ingredient_foods` table by name/alias
- Unit name is matched against `units` table by name/abbreviation/plural
- If no match, raw string is kept

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Parser/`

### Implemented
- `POST /parser/ingredient` — single ingredient
- `POST /parser/ingredients` — batch
- OpenAI controller exists separately

### Missing / Uncertain
- **NLP strategy** — the Python `crfsuite` NLP model is not portable to C#; no equivalent model present
- **Strategy selection** — no visible `parserName` parameter routing to different strategies
- **Confidence scoring** — unclear if returned
- **Food/unit DB matching** — unclear if parsed food/unit names are matched against group's DB records
- **OpenAI integration** — exists in a separate controller; unclear if it's wired into the parser endpoint as a strategy

---

## Enhancement Opportunities (C#-Specific)

- `IIngredientParserStrategy` interface with implementations: `RegexParserStrategy`, `OpenAiParserStrategy`; selected by a `parser` query param
- For NLP: call the Python parser as an internal sidecar service via `HttpClient` until a .NET NLP model is available, or use ML.NET with a ported model
- `IFoodMatcher` / `IUnitMatcher` services that query the group's foods/units table and return the best match by name, alias, or plural form — wrap in a fuzzy string comparison (Levenshtein or Jaro-Winkler)
- Cache the group's foods and units in `IMemoryCache` keyed by `groupId` with a short TTL; parsing is called frequently during recipe import
- Return `confidence` score in the DTO so the frontend can indicate low-confidence parses for user review
