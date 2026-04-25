#!/bin/bash
set -e

BASE_URL="https://raw.githubusercontent.com/Grrtt/mealie/mealie-next"

echo "Pulling docker-compose.prod.yml..."
curl -fsSL "$BASE_URL/docker/docker-compose.prod.yml" -o docker-compose.prod.yml

echo "Done. Edit docker-compose.prod.yml as needed, then run:"
echo "  docker compose -f docker-compose.prod.yml up -d"
