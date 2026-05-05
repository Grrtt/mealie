import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { useTranslation } from "react-i18next";
import { supportedLocales } from "@/lib/i18n/locales";

export function LocaleSelector() {
  const { i18n } = useTranslation();

  return (
    <TextField
      select
      size="small"
      label="Locale"
      value={i18n.language}
      onChange={event => void i18n.changeLanguage(event.target.value)}
      sx={{ minWidth: 160, bgcolor: "background.paper", borderRadius: 1 }}
      inputProps={{ "aria-label": "Locale" }}
    >
      {supportedLocales.map(locale => (
        <MenuItem key={locale.code} value={locale.code}>
          {locale.code} · {locale.dir.toUpperCase()}
        </MenuItem>
      ))}
    </TextField>
  );
}
