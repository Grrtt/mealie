import { getPersistedLocale } from "@/lib/i18n/persistedLocale";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly data: unknown,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type RequestOptions = RequestInit & {
  suppressAuthRedirect?: boolean;
};

function normalizePath(path: string) {
  return path.startsWith("/") ? path : `/${path}`;
}

function getBasePath() {
  return import.meta.env.BASE_URL === "/"
    ? ""
    : import.meta.env.BASE_URL.replace(/\/$/, "");
}

function buildHeaders(headers?: HeadersInit) {
  const nextHeaders = new Headers(headers);
  const locale = getPersistedLocale();

  nextHeaders.set("Accept-Language", locale);

  return nextHeaders;
}

async function parseBody(response: Response) {
  if (response.status === 204) return null;
  const contentType = response.headers.get("content-type") || "";

  if (contentType.includes("application/json")) {
    return await response.json();
  }

  return await response.text();
}

function redirectToLogin() {
  if (typeof window === "undefined") return;
  if (window.location.pathname.includes("/login")) return;
  window.location.assign(resolvePath("/login"));
}

async function request<T>(path: string, init?: RequestOptions) {
  const response = await fetch(resolvePath(path), {
    credentials: "include",
    ...init,
    headers: buildHeaders(init?.headers),
  });

  const payload = await parseBody(response);

  if (!response.ok) {
    if (response.status === 401) {
      if (!init?.suppressAuthRedirect) {
        redirectToLogin();
      }
    }

    const message
      = typeof payload === "object" && payload && "detail" in payload
        ? String((payload as { detail?: unknown }).detail)
        : response.statusText;

    throw new ApiError(message || "Request failed", response.status, payload);
  }

  return payload as T;
}

function withBody<T>(path: string, method: "POST" | "PUT" | "PATCH", body?: BodyInit | object, init?: RequestOptions) {
  const isBodyObject = body != null && !(body instanceof FormData) && typeof body === "object";

  return request<T>(path, {
    method,
    body: isBodyObject ? JSON.stringify(body) : (body as BodyInit | null | undefined),
    ...init,
    headers: {
      ...(isBodyObject ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });
}

export function resolvePath(path: string) {
  return `${getBasePath()}${normalizePath(path)}`;
}

export const apiClient = {
  get: <T>(path: string, init?: RequestOptions) => request<T>(path, { method: "GET", ...init }),
  post: <T>(path: string, body?: BodyInit | object, init?: RequestOptions) => withBody<T>(path, "POST", body, init),
  put: <T>(path: string, body?: BodyInit | object, init?: RequestOptions) => withBody<T>(path, "PUT", body, init),
  patch: <T>(path: string, body?: BodyInit | object, init?: RequestOptions) => withBody<T>(path, "PATCH", body, init),
  delete: <T>(path: string, init?: RequestOptions) => request<T>(path, { method: "DELETE", ...init }),
  resolvePath,
};
