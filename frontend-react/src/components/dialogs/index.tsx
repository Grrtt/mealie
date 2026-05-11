import { type ComponentPropsWithoutRef, type ReactNode, createContext, useContext, useEffect, useId, useMemo, useRef } from "react";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { alpha } from "@mui/material/styles";
import type { SxProps, Theme } from "@mui/material/styles";
import { createPortal } from "react-dom";

type DialogMaxWidth = "xs" | "sm" | "md" | "lg" | "xl" | false;

type DialogProps = {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
  fullWidth?: boolean;
  maxWidth?: DialogMaxWidth;
};

type DialogSectionProps = {
  children: ReactNode;
  sx?: SxProps<Theme>;
};

const dialogWidths: Record<Exclude<DialogMaxWidth, false>, number> = {
  xs: 444,
  sm: 600,
  md: 900,
  lg: 1200,
  xl: 1536,
};

const DialogContext = createContext<{ titleId: string } | null>(null);

function useDialogContext() {
  const context = useContext(DialogContext);
  if (!context) {
    throw new Error("Dialog sections must be rendered inside the dialog component.");
  }

  return context;
}

function showDialog(dialog: HTMLDialogElement) {
  if (typeof dialog.showModal === "function") {
    dialog.showModal();
    return;
  }

  dialog.setAttribute("open", "true");
}

function closeDialog(dialog: HTMLDialogElement) {
  if (typeof dialog.close === "function") {
    dialog.close();
    return;
  }

  dialog.removeAttribute("open");
}

function dialogWidthStyles(maxWidth: DialogMaxWidth, fullWidth: boolean) {
  if (maxWidth === false) {
    return {
      width: fullWidth ? "calc(100vw - 32px)" : "auto",
      maxWidth: "calc(100vw - 32px)",
    };
  }

  const width = dialogWidths[maxWidth ?? "sm"];
  return {
    width: fullWidth ? `min(calc(100vw - 32px), ${width}px)` : "auto",
    maxWidth: `${width}px`,
  };
}

export function Dialog({ open, onClose, children, fullWidth = false, maxWidth = "sm" }: DialogProps) {
  const dialogRef = useRef<HTMLDialogElement | null>(null);
  const titleId = useId();
  const sizing = useMemo(() => dialogWidthStyles(maxWidth, fullWidth), [fullWidth, maxWidth]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open) {
      if (!dialog.open) {
        showDialog(dialog);
      }
      return;
    }

    if (dialog.open) {
      closeDialog(dialog);
    }
  }, [open]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    const handleCancel = (event: Event) => {
      event.preventDefault();
      onClose();
    };

    const handleClick = (event: MouseEvent) => {
      if (event.target === dialog) {
        onClose();
      }
    };

    dialog.addEventListener("cancel", handleCancel);
    dialog.addEventListener("click", handleClick);

    return () => {
      dialog.removeEventListener("cancel", handleCancel);
      dialog.removeEventListener("click", handleClick);
    };
  }, [onClose]);

  if (typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      aria-modal="true"
      style={{
        padding: 0,
        border: "none",
        background: "transparent",
        overflow: "visible",
        maxHeight: "calc(100vh - 32px)",
        ...sizing,
      }}
    >
      <DialogContext.Provider value={{ titleId }}>
        <Box
          sx={theme => ({
            display: "flex",
            flexDirection: "column",
            backgroundColor: theme.palette.background.paper,
            color: theme.palette.text.primary,
            borderRadius: 3,
            border: `1px solid ${alpha(theme.palette.common.black, 0.08)}`,
            boxShadow: "0 24px 72px rgba(15, 23, 42, 0.24)",
            maxHeight: "calc(100vh - 32px)",
            overflow: "hidden",
          })}
        >
          {children}
        </Box>
      </DialogContext.Provider>
      <style>
        {`dialog::backdrop { background: rgba(15, 23, 42, 0.48); backdrop-filter: blur(2px); }`}
      </style>
    </dialog>,
    document.body,
  );
}

export function DialogTitle({ children, sx }: DialogSectionProps) {
  const { titleId } = useDialogContext();

  return (
    <Box
      sx={[
        {
          px: { xs: 2.5, md: 3 },
          pt: { xs: 2.5, md: 3 },
          pb: 1.5,
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      <Typography id={titleId} component="h2" variant="h6">
        {children}
      </Typography>
    </Box>
  );
}

export function DialogContent({ children, sx }: DialogSectionProps) {
  return (
    <Box
      sx={[
        {
          px: { xs: 2.5, md: 3 },
          pb: { xs: 2.5, md: 3 },
          overflowY: "auto",
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      {children}
    </Box>
  );
}

export function DialogActions({ children, sx }: DialogSectionProps) {
  return (
    <Stack
      direction="row"
      spacing={1}
      justifyContent="flex-end"
      useFlexGap
      sx={[
        {
          px: { xs: 2.5, md: 3 },
          pb: { xs: 2.5, md: 3 },
          pt: 1,
          flexWrap: "wrap",
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      {children}
    </Stack>
  );
}

export type DialogTitleProps = ComponentPropsWithoutRef<typeof DialogTitle>;
export type DialogContentProps = ComponentPropsWithoutRef<typeof DialogContent>;
export type DialogActionsProps = ComponentPropsWithoutRef<typeof DialogActions>;
