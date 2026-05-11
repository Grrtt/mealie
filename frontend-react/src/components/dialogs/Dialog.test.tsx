import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";

describe("Dialog", () => {
  const originalShowModal = HTMLDialogElement.prototype.showModal;
  const originalClose = HTMLDialogElement.prototype.close;

  beforeEach(() => {
    HTMLDialogElement.prototype.showModal = function showModal() {
      this.setAttribute("open", "true");
    };

    HTMLDialogElement.prototype.close = function close() {
      this.removeAttribute("open");
    };
  });

  afterEach(() => {
    cleanup();
    HTMLDialogElement.prototype.showModal = originalShowModal;
    HTMLDialogElement.prototype.close = originalClose;
  });

  it("renders a native dialog element when opened", () => {
    render(
      <Dialog open onClose={() => undefined} fullWidth maxWidth="sm">
        <DialogTitle>Test dialog</DialogTitle>
        <DialogContent>Dialog body</DialogContent>
        <DialogActions>
          <button type="button">Close</button>
        </DialogActions>
      </Dialog>,
    );

    const dialog = document.querySelector("dialog");
    expect(dialog).not.toBeNull();
    expect(dialog?.hasAttribute("open")).toBe(true);
    expect(screen.getByRole("heading", { name: "Test dialog" })).toBeVisible();
  });

  it("removes the native dialog open state when controlled closed", async () => {
    const { rerender } = render(
      <Dialog open onClose={() => undefined} fullWidth maxWidth="sm">
        <DialogTitle>Test dialog</DialogTitle>
        <DialogContent>Dialog body</DialogContent>
      </Dialog>,
    );

    const dialog = document.querySelector("dialog");
    expect(dialog).not.toBeNull();
    expect(dialog?.hasAttribute("open")).toBe(true);

    rerender(
      <Dialog open={false} onClose={() => undefined} fullWidth maxWidth="sm">
        <DialogTitle>Test dialog</DialogTitle>
        <DialogContent>Dialog body</DialogContent>
      </Dialog>,
    );

    await waitFor(() => expect(dialog?.hasAttribute("open")).toBe(false));
  });
});
