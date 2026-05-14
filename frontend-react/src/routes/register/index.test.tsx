import { render, screen, waitFor } from "@testing-library/react";
import { forwardRef } from "react";
import type { ComponentPropsWithoutRef } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RegisterRouteComponent } from "@/routes/register/index";

const navigateMock = vi.fn();
const useSearchMock = vi.fn();
const getAppInfoMock = vi.fn();
const registerUserMock = vi.fn();
const resolveRegistrationInviteMock = vi.fn();

vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();

  return {
    ...actual,
    Link: forwardRef<HTMLAnchorElement, { to: string } & ComponentPropsWithoutRef<"a">>((props, ref) => {
      const { to, ...anchorProps } = props;
      return <a ref={ref} href={to} {...anchorProps} />;
    }),
    useNavigate: () => navigateMock,
    useSearch: () => useSearchMock(),
  };
});

vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/features/auth/defaultLanding", () => ({
  getDefaultLandingRoute: vi.fn(),
}));

vi.mock("@/features/auth/session", () => ({
  getAppInfo: (...args: unknown[]) => getAppInfoMock(...args),
  registerUser: (...args: unknown[]) => registerUserMock(...args),
  resolveRegistrationInvite: (...args: unknown[]) => resolveRegistrationInviteMock(...args),
}));

describe("RegisterRouteComponent", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useSearchMock.mockReturnValue({ invite: "secure-invite-token" });
    getAppInfoMock.mockResolvedValue({ allowSignup: false });
    registerUserMock.mockResolvedValue({ detail: "Registration successful", authenticated: false, user: null });
    resolveRegistrationInviteMock.mockResolvedValue({ email: "invitee@example.com" });
  });

  it("keeps secure invite registration available when self-signup is disabled", async () => {
    render(<RegisterRouteComponent />);

    expect(await screen.findByText("user-registration.secure-invite-loaded")).toBeVisible();
    expect(resolveRegistrationInviteMock).toHaveBeenCalledWith("secure-invite-token");
    expect(screen.queryByText("user.invite-only")).not.toBeInTheDocument();

    const email = screen.getByLabelText("user.email");
    expect(email).toHaveValue("invitee@example.com");
    expect(email).toBeDisabled();

    await waitFor(() => expect(screen.getByRole("button", { name: "user.register" })).toBeEnabled());
  });
});
