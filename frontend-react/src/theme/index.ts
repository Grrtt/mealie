import { alpha, createTheme } from "@mui/material/styles";
import type { PaletteMode, Theme } from "@mui/material/styles";

const focusIndicatorOuterColor = "#E58325";

function c40FocusRingBase() {
  return {
    boxShadow: "0 0 0 0 rgba(255,255,255,0)",
    outline: "none",
    position: "relative",
    zIndex: 1,
    overflow: "visible",
    transition: "box-shadow 140ms ease-out",
    "&::before": {
      content: "\"\"",
      position: "absolute",
      inset: -6,
      border: `2px dashed ${focusIndicatorOuterColor}`,
      borderRadius: "inherit",
      pointerEvents: "none",
      opacity: 0,
      transition: "opacity 140ms ease-out",
    },
  };
}

function c40FocusRingActive(mode: PaletteMode) {
  return {
    boxShadow: `0 0 0 3px ${mode === "dark" ? "#10131A" : "#FFFFFF"}`,
    "&::before": {
      opacity: 1,
    },
  };
}

export function createAppTheme(direction: "ltr" | "rtl", mode: PaletteMode) {
  const isDark = mode === "dark";

  const baseTheme = createTheme({
    direction,
    palette: {
      mode,
      primary: {
        light: "#F2A154",
        main: "#E58325",
        dark: isDark ? "#FFC17A" : "#A24E00",
        contrastText: "#172033",
      },
      secondary: {
        light: isDark ? "#D48A95" : "#B05A67",
        main: isDark ? "#C56A79" : "#973542",
        dark: isDark ? "#F3BBC4" : "#7A2A35",
        contrastText: "#FFFFFF",
      },
      text: {
        primary: isDark ? "#F5F7FB" : "#172033",
        secondary: isDark ? "#B4C0D0" : "#475467",
      },
      background: {
        default: isDark ? "#10131A" : "#f6f7fb",
        paper: isDark ? "#161D28" : "#ffffff",
      },
      divider: isDark ? alpha("#D7DEEA", 0.12) : alpha("#172033", 0.08),
    },
    shape: {
      borderRadius: 12,
    },
  });

  return createTheme(baseTheme, {
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            direction,
            backgroundColor: baseTheme.palette.background.default,
            color: baseTheme.palette.text.primary,
          },
          "a[href]:not(.MuiButtonBase-root), button:not(.MuiButtonBase-root), summary, [tabindex]:not([tabindex=\"-1\"]):not(.MuiButtonBase-root)": c40FocusRingBase(),
          "a[href]:not(.MuiButtonBase-root):focus-visible, button:not(.MuiButtonBase-root):focus-visible, summary:focus-visible, [tabindex]:not([tabindex=\"-1\"]):not(.MuiButtonBase-root):focus-visible": c40FocusRingActive(mode),
        },
      },
      MuiAppBar: {
        styleOverrides: {
          colorPrimary: {
            backgroundImage: "none",
          },
        },
      },
      MuiButtonBase: {
        defaultProps: {
          disableRipple: true,
          disableTouchRipple: true,
          focusRipple: false,
        },
        styleOverrides: {
          root: {
            "&:not(.MuiSwitch-switchBase)": c40FocusRingBase(),
            "&.Mui-focusVisible:not(.MuiCardActionArea-root):not(.MuiSwitch-switchBase)": c40FocusRingActive(mode),
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            "&:has(> .MuiCardActionArea-root)": c40FocusRingBase(),
            "&:has(> .MuiCardActionArea-root.Mui-focusVisible)": c40FocusRingActive(mode),
          },
        },
      },
      MuiCardActionArea: {
        styleOverrides: {
          root: {
            borderRadius: "inherit",
          },
          focusHighlight: {
            backgroundColor: "transparent",
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            fontWeight: 700,
            borderRadius: "inherit",
          },
          outlinedPrimary: ({ theme }: { theme: Theme }) => ({
            color: theme.palette.primary.dark,
            borderColor: alpha(theme.palette.primary.dark, 0.35),
            "&:hover": {
              borderColor: theme.palette.primary.dark,
              backgroundColor: alpha(theme.palette.primary.main, 0.08),
            },
          }),
          textPrimary: ({ theme }: { theme: Theme }) => ({
            color: theme.palette.primary.dark,
            "&:hover": {
              backgroundColor: alpha(theme.palette.primary.main, 0.08),
            },
          }),
          containedPrimary: ({ theme }: { theme: Theme }) => ({
            backgroundColor: theme.palette.primary.main,
            color: theme.palette.primary.contrastText,
            "&:hover": {
              backgroundColor: "#D97716",
            },
          }),
        },
      },
      MuiChip: {
        styleOverrides: {
          outlinedPrimary: ({ theme }: { theme: Theme }) => ({
            color: theme.palette.primary.dark,
            borderColor: alpha(theme.palette.primary.dark, 0.35),
            backgroundColor: alpha(theme.palette.primary.main, 0.08),
          }),
          filledPrimary: ({ theme }: { theme: Theme }) => ({
            backgroundColor: theme.palette.primary.main,
            color: theme.palette.primary.contrastText,
          }),
        },
      },
      MuiFormControl: {
        styleOverrides: {
          root: ({ theme }: { theme: Theme }) => ({
            "& .MuiInputLabel-root": {
              position: "static",
              transform: "none",
              maxWidth: "none",
              marginBottom: theme.spacing(0.75),
              color: theme.palette.text.secondary,
              fontSize: theme.typography.body2.fontSize,
              lineHeight: theme.typography.body2.lineHeight,
              pointerEvents: "auto",
            },
            "& .MuiInputLabel-root.Mui-focused": {
              color: theme.palette.text.primary,
            },
            "& .MuiInputLabel-root.Mui-disabled": {
              color: theme.palette.text.disabled,
            },
            "& .MuiInputLabel-asterisk": {
              color: theme.palette.error.main,
            },
          }),
        },
      },
      MuiInputLabel: {
        defaultProps: {
          shrink: true,
        },
      },
      MuiSvgIcon: {
        styleOverrides: {
          colorPrimary: ({ theme }: { theme: Theme }) => ({
            color: theme.palette.primary.dark,
          }),
        },
      },
      MuiCheckbox: {
        styleOverrides: {
          root: ({ theme }: { theme: Theme }) => ({
            "&.Mui-checked, &.MuiCheckbox-indeterminate": {
              color: theme.palette.primary.dark,
            },
          }),
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: ({ theme }: { theme: Theme }) => ({
            ...c40FocusRingBase(),
            marginTop: 0,
            "&:has(input:focus-visible), &:has(textarea:focus-visible)": {
              ...c40FocusRingActive(mode),
              "& .MuiOutlinedInput-notchedOutline": {
                borderColor: theme.palette.primary.dark,
              },
            },
          }),
          notchedOutline: {
            top: 0,
            "& legend": {
              display: "none",
            },
          },
          input: {
            "&::placeholder": {
              opacity: 0.8,
            },
          },
        },
      },
      MuiSwitch: {
        styleOverrides: {
          root: {
            ...c40FocusRingBase(),
            borderRadius: 999,
            overflow: "visible",
            "&:has(.MuiSwitch-switchBase.Mui-focusVisible)": c40FocusRingActive(mode),
          },
          switchBase: ({ theme }: { theme: Theme }) => ({
            borderRadius: "50%",
            "&.Mui-checked": {
              color: theme.palette.primary.main,
              "& + .MuiSwitch-track": {
                backgroundColor: alpha(theme.palette.primary.main, 0.5),
              },
            },
          }),
          track: ({ theme }: { theme: Theme }) => ({
            backgroundColor: alpha(theme.palette.text.secondary, 0.35),
          }),
        },
      },
    },
  });
}
