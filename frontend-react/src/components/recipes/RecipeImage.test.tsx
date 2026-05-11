import { act, render, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RecipeImage } from "@/components/recipes/RecipeImage";

class MockImage {
  onload: null | (() => void) = null;
  onerror: null | (() => void) = null;
  decoding = "";
  complete = false;
  naturalWidth = 0;
  private _src = "";

  set src(value: string) {
    this._src = value;
  }

  get src() {
    return this._src;
  }

  triggerLoad() {
    this.complete = true;
    this.naturalWidth = 1200;
    this.onload?.();
  }

  triggerError() {
    this.complete = true;
    this.naturalWidth = 0;
    this.onerror?.();
  }
}

describe("RecipeImage", () => {
  const createdImages: MockImage[] = [];

  beforeEach(() => {
    createdImages.length = 0;

    vi.stubGlobal("Image", class extends MockImage {
      constructor() {
        super();
        createdImages.push(this);
      }
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("waits to render the image element until the file finishes loading", async () => {
    const { container } = render(<RecipeImage alt="Recipe image" src="/example.webp" wrapperSx={{ minHeight: 180 }} />);

    expect(container.querySelector("img")).toBeNull();
    expect(createdImages).toHaveLength(1);

    await act(async () => {
      createdImages[0]?.triggerLoad();
    });

    await waitFor(() => expect(container.querySelector('img[alt="Recipe image"]')).not.toBeNull());
  });

  it("keeps the placeholder visible when the image request fails", async () => {
    const { container } = render(<RecipeImage alt="Recipe image" src="/missing.webp" wrapperSx={{ minHeight: 180 }} />);

    expect(createdImages).toHaveLength(1);

    await act(async () => {
      createdImages[0]?.triggerError();
    });

    await waitFor(() => expect(container.querySelector("img")).toBeNull());
  });

  it("retries failed images so they can appear without a refresh", async () => {
    vi.useFakeTimers();
    const { container } = render(<RecipeImage alt="Recipe image" src="/eventual.webp" wrapperSx={{ minHeight: 180 }} />);

    expect(createdImages).toHaveLength(1);

    await act(async () => {
      createdImages[0]?.triggerError();
    });

    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    expect(createdImages).toHaveLength(2);

    await act(async () => {
      createdImages[1]?.triggerLoad();
      vi.advanceTimersByTime(20);
    });

    expect(container.querySelector('img[alt="Recipe image"]')).not.toBeNull();
  });
});
