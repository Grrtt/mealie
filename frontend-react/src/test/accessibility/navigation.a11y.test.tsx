import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { MainNav } from "@/components/navigation/MainNav";

describe("navigation accessibility", () => {
  it("exposes the main navigation as a landmark with named links in keyboard order", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MainNav groupSlug="home" />
      </QueryClientProvider>,
    );

    expect(screen.getByRole("navigation", { name: /primary navigation/i })).toBeInTheDocument();

    const expectedLinks = ["Recipes", "Finder", "Meal planner", "Shopping lists", "Timeline", "Cookbooks"];
    for (const label of expectedLinks) {
      expect(screen.getByRole("link", { name: label })).toBeVisible();
    }
    expect(screen.getByRole("button", { name: "Organizers" })).toBeVisible();

    await user.tab();
    expect(screen.getByRole("link", { name: "Recipes" })).toHaveFocus();
    await user.tab();
    expect(screen.getByRole("link", { name: "Finder" })).toHaveFocus();
  });
});
