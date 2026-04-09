const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

interface FetchOptions extends RequestInit {
  token?: string;
}

/**
 * Server-side fetch helper that forwards JWT token to the ASP.NET API.
 * Always uses absolute URLs for SSR compatibility.
 */
export async function apiFetch<T>(
  path: string,
  options: FetchOptions = {}
): Promise<T> {
  const { token, ...fetchOptions } = options;
  const url = `${API_BASE_URL}${path}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(fetchOptions.headers as Record<string, string>),
  };

  const response = await fetch(url, {
    ...fetchOptions,
    headers,
    cache: "no-store", // SSR — always fresh data
  });

  if (!response.ok) {
    const error = await response.text().catch(() => "Unknown error");
    throw new Error(`API ${response.status}: ${error}`);
  }

  return response.json() as Promise<T>;
}

/**
 * Get the JWT token from cookies in server components.
 */
export function getTokenFromCookies(
  cookieHeader: string | null
): string | null {
  if (!cookieHeader) return null;
  const match = cookieHeader.match(/accessToken=([^;]+)/);
  return match ? match[1] : null;
}
