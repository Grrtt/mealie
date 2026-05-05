const REDIRECT_KEY = "react-migration-intended-destination";

export function saveIntendedDestination(destination: string) {
  if (typeof window === "undefined" || !destination) return;
  window.sessionStorage.setItem(REDIRECT_KEY, destination);
}

export function readIntendedDestination() {
  if (typeof window === "undefined") return null;
  return window.sessionStorage.getItem(REDIRECT_KEY);
}

export function consumeIntendedDestination() {
  const destination = readIntendedDestination();
  if (typeof window !== "undefined") {
    window.sessionStorage.removeItem(REDIRECT_KEY);
  }
  return destination;
}
