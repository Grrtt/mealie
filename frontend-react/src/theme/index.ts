import { createTheme } from "@mui/material/styles";

export function createAppTheme(direction: "ltr" | "rtl") {
  return createTheme({
    direction,
    palette: {
      mode: "light",
      primary: {
        main: "#E58325",
      },
      secondary: {
        main: "#973542",
      },
      background: {
        default: "#f6f7fb",
      },
    },
    shape: {
      borderRadius: 12,
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            direction,
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          colorPrimary: {
            backgroundImage: "none",
          },
        },
      },
    },
  });
}
