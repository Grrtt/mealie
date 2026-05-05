import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { z } from "zod";
import { FormTextField } from "@/components/forms";
import { useZodForm } from "@/lib/forms/useZodForm";

const schema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters"),
});

function Harness() {
  const form = useZodForm(schema, {
    defaultValues: {
      email: "",
      password: "",
    },
  });

  return (
    <Stack component="form" spacing={2} onSubmit={form.handleSubmit(() => undefined)} aria-label="Login form">
      <FormTextField control={form.control} name="email" label="Email" autoFocus />
      <FormTextField control={form.control} name="password" label="Password" type="password" />
      <Button type="submit" variant="contained">Submit</Button>
    </Stack>
  );
}

describe("form accessibility", () => {
  it("keeps labels, invalid state, and helper text exposed to assistive tech", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    const email = screen.getByLabelText("Email");
    const password = screen.getByLabelText("Password");

    expect(screen.getByRole("form", { name: /login form/i })).toBeInTheDocument();
    expect(email).toHaveAttribute("id");
    expect(password).toHaveAttribute("id");

    await user.click(screen.getByRole("button", { name: /submit/i }));

    expect(email).toHaveAttribute("aria-invalid", "true");
    expect(password).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByText("Email is required")).toBeVisible();
    expect(screen.getByText("Password must be at least 8 characters")).toBeVisible();
  });
});
