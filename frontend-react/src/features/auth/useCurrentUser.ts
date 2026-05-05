import { useQuery } from "@tanstack/react-query";
import { currentUserQueryOptions } from "@/features/auth/session";

export function useCurrentUser() {
  return useQuery(currentUserQueryOptions);
}
