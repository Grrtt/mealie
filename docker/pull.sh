#!/bin/bash
set -e

BASE_URL="https://raw.githubusercontent.com/Grrtt/mealie/mealie-next"

if [ -f docker-compose.yml ]; then
  echo "docker-compose.yml already exists. Skipping to avoid overwriting."
  echo "Delete it first if you want a fresh copy."
  exit 0
fi

echo "Pulling docker-compose.yml..."
curl -fsSL "$BASE_URL/docker/docker-compose.prod.yml" -o docker-compose.yml

echo "Done. Edit docker-compose.yml as needed, then run:"
echo "  docker compose up -d"
