import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RecipeDetailView } from "@/components/recipes/RecipeDetailView";
import type { Recipe } from "@/lib/api/contracts";

const navigateMock = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

describe("RecipeDetailView", () => {
  const request = vi.fn();
  const release = vi.fn();

  const recipe: Recipe = {
    id: "recipe-1",
    slug: "best-hummus",
    name: "Best Hummus",
    description: "Smooth, lemony hummus.",
    recipeYield: "4 servings",
    totalTime: "20 mins",
    prepTime: "10 mins",
    cookTime: "10 mins",
    recipeIngredient: [
      {
        referenceId: "ing-1",
        quantity: 2,
        unit: { id: "cup", name: "cups", abbreviation: "c", useAbbreviation: true },
        food: { id: "chickpeas", name: "chickpeas" },
      },
      {
        referenceId: "ing-2",
        food: { id: "tahini", name: "tahini" },
        note: "stir well before using",
      },
    ],
    recipeInstructions: [
      {
        id: "step-1",
        summary: "Blend the base",
        text: "Add everything to a food processor and blend until smooth.",
      },
    ],
    tools: [
      {
        id: "tool-1",
        slug: "food-processor",
        name: "Food processor",
      },
    ],
    recipeCategory: [{ id: "cat-1", slug: "dips", name: "Dips" }],
    tags: [{ id: "tag-1", slug: "vegan", name: "Vegan" }],
    notes: [{ title: "Tip", text: "Reserve extra olive oil for serving." }],
  };

  beforeEach(() => {
    request.mockResolvedValue({
      released: false,
      release,
      addEventListener: () => undefined,
    });
    release.mockResolvedValue(undefined);
    Object.defineProperty(window.navigator, "wakeLock", {
      configurable: true,
      value: { request },
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders Vue-style cooking controls and checklists", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <RecipeDetailView
          currentUser={null}
          groupSlug="home"
          onEdit={() => undefined}
          onRecipeRefresh={() => undefined}
          recipe={recipe}
        />
      </QueryClientProvider>,
    );

    expect(screen.getByRole("heading", { name: "Ingredients" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Cooking mode" })).toBeVisible();
    expect(screen.getByRole("checkbox", { name: /2 c chickpeas/i })).not.toBeChecked();
    expect(screen.getByRole("switch", { name: "Keep screen awake" })).toBeChecked();

    await user.click(screen.getByRole("button", { name: "More actions" }));
    expect(screen.getByRole("menuitem", { name: "Print" })).toBeVisible();
    expect(screen.getByRole("menuitem", { name: "Print preferences" })).toBeVisible();
    await user.keyboard("{Escape}");

    await user.click(screen.getByRole("button", { name: "Cooking mode" }));
    expect(screen.getByRole("button", { name: "Exit cooking mode" })).toBeVisible();

    await user.click(screen.getByRole("checkbox", { name: /2 c chickpeas/i }));
    expect(screen.getByRole("checkbox", { name: /2 c chickpeas/i })).toBeChecked();

    await waitFor(() => expect(request).toHaveBeenCalledWith("screen"));
  });
});
