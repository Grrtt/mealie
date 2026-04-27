#!/usr/bin/env bash
# clear-recipes.sh — wipe all recipes (and related data) from the dev SQLite DB.
# Safe to rerun. Stops the API container, copies the DB out, clears it, copies back.

set -euo pipefail

COMPOSE_FILE="$(cd "$(dirname "$0")/../.." && pwd)/docker-compose.dotnet.yml"
CONTAINER="mealie_api"
REMOTE_DB="/app/data/mealie.db"
LOCAL_DB="/tmp/mealie_clear_$$.db"

echo "▶ Stopping $CONTAINER..."
docker compose -f "$COMPOSE_FILE" stop mealie-api

echo "▶ Copying DB out of container..."
docker cp "$CONTAINER:$REMOTE_DB" "$LOCAL_DB"

echo "▶ Clearing recipe tables..."
sqlite3 "$LOCAL_DB" "
  PRAGMA journal_mode=DELETE;
  DELETE FROM shopping_list_item_recipe_reference;
  DELETE FROM shopping_list_recipe_reference;
  DELETE FROM recipes_to_categories;
  DELETE FROM recipes_to_tags;
  DELETE FROM recipes_to_tools;
  DELETE FROM users_to_recipes;
  DELETE FROM recipe_timeline_events;
  DELETE FROM recipe_share_tokens;
  DELETE FROM recipe_comments;
  DELETE FROM recipe_assets;
  DELETE FROM notes;
  DELETE FROM recipe_instructions;
  DELETE FROM recipes_ingredients;
  DELETE FROM recipes;
  DELETE FROM report_entries;
  DELETE FROM reports;
  DELETE FROM ingredient_foods_aliases;
  DELETE FROM ingredient_foods;
  VACUUM;
"

echo "▶ Copying DB back..."
docker cp "$LOCAL_DB" "$CONTAINER:$REMOTE_DB"
rm -f "$LOCAL_DB"

echo "▶ Removing queued migration files..."
docker run --rm \
  -v mealie-data:/app/data \
  alpine:3 \
  sh -c "rm -rf /app/data/migration-queue && echo 'Removed migration-queue dir'"

echo "▶ Restarting $CONTAINER..."
docker compose -f "$COMPOSE_FILE" start mealie-api

echo "✓ Recipes cleared, migration files removed, and container restarted."
