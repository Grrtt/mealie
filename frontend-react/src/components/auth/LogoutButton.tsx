import type { ButtonProps } from "@mui/material/Button";
import Button from "@mui/material/Button";
import { useNavigate } from "@tanstack/react-router";
import { signOut } from "@/features/auth/session";

type Props = Pick<ButtonProps, "color" | "fullWidth" | "size" | "variant">;

export function LogoutButton({
  color = "inherit",
  fullWidth = false,
  size = "medium",
  variant = "outlined",
}: Props = {}) {
  const navigate = useNavigate();

  return (
    <Button
      color={color}
      fullWidth={fullWidth}
      size={size}
      variant={variant}
      onClick={async () => {
        await signOut();
        await navigate({ to: "/login" });
      }}
    >
      Logout
    </Button>
  );
}
