import { useEffect } from "react";

export async function resolveIndexRedirect() {
  return null;
}

export function IndexRouteComponent() {
  useEffect(() => {
    document.title = "Mealie";
  }, []);

  return null;
}
