import { queryOptions } from "@tanstack/react-query";
import { apiClient, ApiError, getAuthToken, setAuthToken } from "@/lib/api/client";
import type {
  AppInfo,
  AppStartupInfo,
  PrivateUser,
  RegistrationPayload,
  Token,
} from "@/lib/api/contracts";
import { queryClient } from "@/lib/query/queryClient";

export const currentUserQueryKey = ["auth", "current-user"] as const;

export const currentUserQueryOptions = queryOptions({
  queryKey: currentUserQueryKey,
  queryFn: async (): Promise<PrivateUser | null> => {
    if (!getAuthToken()) {
      return null;
    }

    try {
      return await apiClient.get<PrivateUser>("/api/users/self", { suppressAuthRedirect: true });
    }
    catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setAuthToken(null);
        return null;
      }

      throw error;
    }
  },
});

export async function ensureCurrentUser() {
  return await queryClient.ensureQueryData(currentUserQueryOptions);
}

async function fetchCurrentUser() {
  return await queryClient.fetchQuery(currentUserQueryOptions);
}

export async function hydrateSession() {
  await queryClient.prefetchQuery(currentUserQueryOptions);
}

export async function signInWithPassword(payload: {
  username: string;
  password: string;
  rememberMe: boolean;
}) {
  const formData = new FormData();
  formData.append("username", payload.username);
  formData.append("password", payload.password);
  formData.append("remember_me", String(payload.rememberMe));

  const token = await apiClient.post<Token>("/api/auth/token", formData, {
    suppressAuthRedirect: true,
  });

  setAuthToken(token.access_token);
  await queryClient.removeQueries({ queryKey: currentUserQueryKey });
  return await fetchCurrentUser();
}

export async function signInWithOidcCallback(search: string) {
  const queryString = search.startsWith("?") ? search : `?${search}`;
  const token = await apiClient.get<Token>(`/api/auth/oauth/callback${queryString}`, {
    suppressAuthRedirect: true,
  });

  setAuthToken(token.access_token);
  await queryClient.removeQueries({ queryKey: currentUserQueryKey });
  return await fetchCurrentUser();
}

export function beginOidcSignIn() {
  window.location.assign(apiClient.resolvePath("/api/auth/oauth"));
}

export async function refreshSession() {
  const token = await apiClient.get<Token>("/api/auth/refresh", {
    suppressAuthRedirect: true,
  });

  setAuthToken(token.access_token);
  await queryClient.removeQueries({ queryKey: currentUserQueryKey });
  return await fetchCurrentUser();
}

export async function signOut() {
  try {
    await apiClient.post("/api/auth/logout", undefined, {
      suppressAuthRedirect: true,
    });
  }
  finally {
    setAuthToken(null);
    queryClient.setQueryData(currentUserQueryKey, null);
  }
}

export async function registerUser(payload: RegistrationPayload) {
  return await apiClient.post<{ detail: string }>("/api/users/register", payload, {
    suppressAuthRedirect: true,
  });
}

export async function sendForgotPassword(email: string) {
  return await apiClient.post<{ detail: string }>(
    "/api/users/forgot-password",
    { email },
    { suppressAuthRedirect: true },
  );
}

export async function resetPassword(token: string, newPassword: string) {
  return await apiClient.post<{ detail: string }>(
    "/api/users/reset-password",
    { token, newPassword },
    { suppressAuthRedirect: true },
  );
}

export async function getAppInfo() {
  return await apiClient.get<AppInfo>("/api/app/about", {
    suppressAuthRedirect: true,
  });
}

export async function getStartupInfo() {
  return await apiClient.get<AppStartupInfo>("/api/app/about/startup-info", {
    suppressAuthRedirect: true,
  });
}
