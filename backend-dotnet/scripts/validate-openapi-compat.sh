#!/usr/bin/env bash
# Validate OpenAPI compatibility between Python and C# backends
# Usage: ./scripts/validate-openapi-compat.sh
# Requirements: curl, jq, diff (all standard Unix tools)

set -euo pipefail

PYTHON_URL="${PYTHON_URL:-http://localhost:9000}"
CSHARP_URL="${CSHARP_URL:-http://localhost:9001}"
TIMEOUT="${TIMEOUT:-30}"

echo "🔍 Fetching OpenAPI specs..."
echo "   Python: $PYTHON_URL/openapi.json"
echo "   C#:     $CSHARP_URL/openapi.json (Swashbuckle: /swagger/v1/swagger.json)"

# Fetch specs
PYTHON_SPEC=$(curl --silent --fail --max-time "$TIMEOUT" "$PYTHON_URL/openapi.json" 2>/dev/null || echo "{}")
CSHARP_SPEC=$(curl --silent --fail --max-time "$TIMEOUT" "$CSHARP_URL/swagger/v1/swagger.json" 2>/dev/null || echo "{}")

if [ "$PYTHON_SPEC" = "{}" ] || [ "$CSHARP_SPEC" = "{}" ]; then
    echo "⚠️  One or both backends unavailable. Skipping live comparison."
    echo "   To run: start both backends and set PYTHON_URL and CSHARP_URL env vars."
    exit 0
fi

# Extract paths from each spec
echo "$PYTHON_SPEC" | jq -r '.paths | keys[]' | sort > /tmp/python_paths.txt
echo "$CSHARP_SPEC" | jq -r '.paths | keys[]' | sort > /tmp/csharp_paths.txt

MISSING=$(comm -23 /tmp/python_paths.txt /tmp/csharp_paths.txt | wc -l)
EXTRA=$(comm -13 /tmp/python_paths.txt /tmp/csharp_paths.txt | wc -l)

echo ""
echo "📊 Path comparison results:"
echo "   Python paths: $(wc -l < /tmp/python_paths.txt)"
echo "   C# paths:     $(wc -l < /tmp/csharp_paths.txt)"
echo "   Missing in C#: $MISSING"
echo "   Extra in C#:   $EXTRA"

if [ "$MISSING" -gt 0 ]; then
    echo ""
    echo "❌ Paths in Python but missing from C#:"
    comm -23 /tmp/python_paths.txt /tmp/csharp_paths.txt | head -20
    echo ""
    echo "FAIL: OpenAPI compatibility check failed — C# backend is missing $MISSING paths"
    exit 1
fi

echo ""
echo "✅ All Python API paths are present in C# backend!"
exit 0
