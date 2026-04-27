"""
Download dpapathanasiou/recipes from GitHub and convert to a Mealie-compatible
Nextcloud Cookbook ZIP.

The output ZIP can be imported in Mealie via:
  Group Settings → Data → Migrations → Nextcloud Cookbook

Usage:
    uv run python dev/scripts/dpapathanasiou_to_mealie_zip.py
    uv run python dev/scripts/dpapathanasiou_to_mealie_zip.py --output my_recipes.zip

    # Split into chunks of 2000 recipes each (e.g. to stay under server upload limits):
    uv run python dev/scripts/dpapathanasiou_to_mealie_zip.py --chunk-size 2000
    # Produces: mealie_recipes_001.zip, mealie_recipes_002.zip, ...
"""

import argparse
import io
import json
import zipfile
from pathlib import Path, PurePosixPath

import httpx
from slugify import slugify

REPO_ZIP_URL = "https://github.com/dpapathanasiou/recipes/archive/refs/heads/master.zip"


def convert(entry: dict) -> dict:
    """Convert a dpapathanasiou recipe dict to Nextcloud Cookbook / schema.org JSON-LD."""
    return {
        "@context": "http://schema.org",
        "@type": "Recipe",
        "name": entry.get("title", "Untitled"),
        "url": entry.get("url", ""),
        "keywords": ", ".join(entry.get("tags", [])),
        "recipeIngredient": entry.get("ingredients", []),
        "recipeInstructions": [
            {"@type": "HowToStep", "text": step}
            for step in entry.get("directions", [])
        ],
    }


def _chunk_output_path(base: Path, chunk: int) -> Path:
    return base.with_name(f"{base.stem}_{chunk:03d}{base.suffix}")


def build_zip(output_path: Path, chunk_size: int | None) -> None:
    print(f"Downloading {REPO_ZIP_URL} ...")
    with httpx.Client(follow_redirects=True, timeout=60) as client:
        response = client.get(REPO_ZIP_URL)
        response.raise_for_status()

    repo_bytes = io.BytesIO(response.content)
    print(f"Downloaded {len(response.content) / 1_048_576:.1f} MB. Converting ...")

    with zipfile.ZipFile(repo_bytes) as src_zip:
        recipe_files = [
            n for n in src_zip.namelist()
            if PurePosixPath(n).parts[1:2] == ("index",) and n.endswith(".json")
        ]
        print(f"Found {len(recipe_files)} recipes.")

        seen_slugs: dict[str, int] = {}
        chunk_num = 1
        converted = 0

        def open_zip(path: Path) -> zipfile.ZipFile:
            print(f"  Writing {path} ...")
            return zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED)

        current_path = _chunk_output_path(output_path, chunk_num) if chunk_size else output_path
        out_zip = open_zip(current_path)
        chunk_count = 0

        try:
            for name in recipe_files:
                try:
                    entry = json.loads(src_zip.read(name))
                except Exception as e:
                    print(f"  Skipping {name}: {e}")
                    continue

                title = entry.get("title", "")
                base_slug = slugify(title) or PurePosixPath(name).stem

                count = seen_slugs.get(base_slug, 0)
                seen_slugs[base_slug] = count + 1
                slug = base_slug if count == 0 else f"{base_slug}-{count}"

                out_zip.writestr(
                    f"{slug}/recipe.json",
                    json.dumps(convert(entry), ensure_ascii=False, indent=2),
                )
                converted += 1
                chunk_count += 1

                if chunk_size and chunk_count >= chunk_size:
                    out_zip.close()
                    print(f"    {current_path.stat().st_size / 1_048_576:.1f} MB")
                    chunk_num += 1
                    chunk_count = 0
                    current_path = _chunk_output_path(output_path, chunk_num)
                    out_zip = open_zip(current_path)
        finally:
            out_zip.close()

        print(f"    {current_path.stat().st_size / 1_048_576:.1f} MB")
        print(f"Done. {converted} recipes written across {chunk_num} file(s).")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Download dpapathanasiou/recipes and convert to a Mealie Nextcloud ZIP."
    )
    parser.add_argument(
        "--output", default=Path("mealie_recipes.zip"), type=Path, help="Output ZIP path (default: mealie_recipes.zip)"
    )
    parser.add_argument(
        "--chunk-size", type=int, default=None, metavar="N",
        help="Split output into ZIPs of N recipes each (e.g. --chunk-size 2000)"
    )
    args = parser.parse_args()
    build_zip(args.output, args.chunk_size)


if __name__ == "__main__":
    main()
