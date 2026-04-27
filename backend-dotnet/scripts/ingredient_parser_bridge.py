#!/usr/bin/env python3
"""
Persistent stdin/stdout bridge for ingredient-parser-nlp.

Protocol:
  - Reads one JSON line from stdin: a list of ingredient strings
  - Writes one JSON line to stdout: a list of parsed result objects
  - Loops forever until stdin is closed

Each result object:
  {
    "input":    <original string>,
    "food":     <food name or null>,
    "quantity": <float or null>,
    "unit":     <unit string or null>,
    "note":     <note string or null>
  }
"""

import sys
import json
from ingredient_parser import parse_ingredient
from ingredient_parser.dataclasses import CompositeIngredientAmount


def parse_one(text: str) -> dict:
    try:
        result = parse_ingredient(text)

        # Food name
        food = result.name[0].text if result.name else None

        # Quantity + unit from the first amount
        quantity = None
        unit = None
        if result.amount:
            amt = result.amount[0]
            if isinstance(amt, CompositeIngredientAmount):
                amt = amt.amounts[0]
            try:
                quantity = float(amt.quantity) if amt.quantity else None
            except (ValueError, TypeError):
                quantity = None
            unit = str(amt.unit).strip() if amt.unit else None
            if unit == "None":
                unit = None

        # Note from preparation, comment, size, purpose
        note_parts = []
        for attr in ("size", "preparation", "comment", "purpose"):
            val = getattr(result, attr, None)
            if val and hasattr(val, "text") and val.text:
                note_parts.append(val.text.strip("()").strip())
        note = ", ".join(note_parts) if note_parts else None

        return {"input": text, "food": food, "quantity": quantity, "unit": unit, "note": note}
    except Exception as e:
        return {"input": text, "food": None, "quantity": None, "unit": None, "note": None, "error": str(e)}


def main():
    # Pre-warm: redirect stdout to stderr temporarily so any NLTK download messages
    # don't pollute the JSON protocol channel.
    real_stdout = sys.stdout
    sys.stdout = sys.stderr
    try:
        parse_one("1 cup water")
    except Exception:
        pass
    finally:
        sys.stdout = real_stdout

    real_stdout.write("READY\n")
    real_stdout.flush()

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            ingredients = json.loads(line)
            results = [parse_one(ing) for ing in ingredients]
            print(json.dumps(results), flush=True)
        except Exception as e:
            print(json.dumps([{"error": str(e)}]), flush=True)


if __name__ == "__main__":
    main()
