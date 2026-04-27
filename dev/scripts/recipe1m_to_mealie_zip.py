"""
Convert Recipe1M+ layer1.json to a Mealie-compatible Nextcloud Cookbook ZIP.

Recipe1M+ dataset: https://im2recipe.csail.mit.edu/
The dataset requires registration to download. Once you have it, point
--input at your layer1.json file.

The output ZIP can be imported in Mealie via:
  Group Settings → Data → Migrations → Nextcloud Cookbook

Usage:
    uv run python dev/scripts/recipe1m_to_mealie_zip.py --input layer1.json --output mealie_recipes.zip
    uv run python dev/scripts/recipe1m_to_mealie_zip.py --input layer1.json --output mealie_recipes.zip --limit 500
"""

import argparse
import json
import zipfile
from pathlib import Path

from slugify import slugify


def recipe1m_to_nextcloud(entry: dict) -> dict:
    """Convert a single Recipe1M+ entry to Nextcloud Cookbook / schema.org JSON-LD."""
    title: str = entry.get("title", "Untitled Recipe")

    ingredients = [ing["text"] for ing in entry.get("ingredients", []) if ing.get("text")]

    instructions = [
        {"@type": "HowToStep", "text": step["text"]}
        for step in entry.get("instructions", [])
        if step.get("text")
    ]

    return {
        "@context": "http://schema.org",
        "@type": "Recipe",
        "name": title,
        "recipeIngredient": ingredients,
        "recipeInstructions": instructions,
        "url": entry.get("url", ""),
    }


def build_zip(layer1_path: Path, output_path: Path, limit: int | None) -> None:
    print(f"Reading {layer1_path} ...")
    with layer1_path.open() as f:
        entries: list[dict] = json.load(f)

    if limit is not None:
        entries = entries[:limit]

    total = len(entries)
    print(f"Converting {total} recipes ...")

    seen_slugs: dict[str, int] = {}

    with zipfile.ZipFile(output_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for i, entry in enumerate(entries):
            if i % 1000 == 0:
                print(f"  {i}/{total}")

            title = entry.get("title", "Untitled Recipe")
            base_slug = slugify(title) or f"recipe-{i}"

            # Deduplicate slugs
            count = seen_slugs.get(base_slug, 0)
            seen_slugs[base_slug] = count + 1
            slug = base_slug if count == 0 else f"{base_slug}-{count}"

            recipe_json = recipe1m_to_nextcloud(entry)
            zf.writestr(f"{slug}/recipe.json", json.dumps(recipe_json, ensure_ascii=False, indent=2))

    print(f"Done. Written to {output_path}  ({output_path.stat().st_size / 1_048_576:.1f} MB)")


def main() -> None:
    parser = argparse.ArgumentParser(description="Convert Recipe1M+ layer1.json to a Mealie Nextcloud ZIP.")
    parser.add_argument("--input", required=True, type=Path, help="Path to Recipe1M+ layer1.json")
    parser.add_argument("--output", default=Path("mealie_recipes.zip"), type=Path, help="Output ZIP path")
    parser.add_argument("--limit", type=int, default=None, help="Max number of recipes to include (default: all)")
    args = parser.parse_args()

    if not args.input.exists():
        print(f"Error: input file not found: {args.input}")
        raise SystemExit(1)

    build_zip(args.input, args.output, args.limit)


if __name__ == "__main__":
    main()
