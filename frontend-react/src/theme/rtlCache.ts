import createCache from "@emotion/cache";
import rtlPlugin from "stylis-plugin-rtl";

export function createDirectionalCache(direction: "ltr" | "rtl") {
  return createCache({
    key: direction === "rtl" ? "mui-rtl" : "mui",
    stylisPlugins: direction === "rtl" ? [rtlPlugin] : [],
  });
}
