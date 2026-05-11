import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { useTranslation } from "react-i18next";
import { type ThemePreference, useThemePreference } from "@/theme/themePreference";

export function ThemeModeSelector() {
  const { t } = useTranslation();
  const { preference, setPreference } = useThemePreference();

  return (
    <TextField
      select
      size="small"
      label={t("settings.theme.mode")}
      value={preference}
      onChange={event => setPreference(event.target.value as ThemePreference)}
      sx={{
        minWidth: 180,
        "& .MuiOutlinedInput-root": {
          bgcolor: "transparent",
        },
      }}
      inputProps={{ "aria-label": t("settings.theme.mode") }}
    >
      <MenuItem value="system">{t("settings.theme.default-to-system")}</MenuItem>
      <MenuItem value="light">{t("settings.theme.light")}</MenuItem>
      <MenuItem value="dark">{t("settings.theme.dark")}</MenuItem>
    </TextField>
  );
}
