# Python Ingredient Parser — Reference Code

This directory contains the original Python ingredient parser logic preserved from the Python backend migration. It is **not** an executable module — imports reference the old `mealie.*` package structure which no longer exists. It is kept here as a reference for porting to C#.

## Structure

```
parser/
├── services/          # Core parser logic (from mealie/services/parser_services/)
│   ├── _base.py                  # ABCIngredientParser base class and data matcher
│   ├── ingredient_parser.py      # Main parser entrypoint and brute-force parser
│   ├── brute/                    # Brute-force regex/fraction parsing
│   ├── openai/                   # OpenAI-assisted parsing
│   └── parser_utils/             # Unit conversion and string utilities
└── routes/            # FastAPI route handlers (from mealie/routes/parser/)
    └── ingredient_parser.py      # POST /parser/ingredient and /parser/ingredients endpoints
```

## Key Dependencies (no longer present)

- `ingredient_parser` — third-party NLP library for ingredient parsing
- `mealie.schema.recipe.recipe_ingredient` — Pydantic schemas for `ParsedIngredient`, `RecipeIngredient`, etc.
- `mealie.schema.openai` — OpenAI request/response schemas
- `mealie.services.openai` — OpenAI service wrapper
- `mealie.repos` — SQLAlchemy repository layer for units/foods DB lookups
- `mealie.lang.providers` — i18n/translation support
- `pint` — unit conversion library
- `rapidfuzz` — fuzzy string matching
