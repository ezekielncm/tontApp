/**
 * Application-wide constants.
 * API_BASE_URL should point to the ASP.NET Core backend.
 * In production, this would be set via environment variables.
 */

/** Base URL for the API – override via EXPO_PUBLIC_API_URL env var */
export const API_BASE_URL: string =
  process.env.EXPO_PUBLIC_API_URL ?? 'http://192.168.11.101:8080';

/** API version prefix */
export const API_PREFIX = '/api/v1';

/** SecureStore keys */
export const SECURE_STORE_KEYS = {
  ACCESS_TOKEN: 'tontines_access_token',
  REFRESH_TOKEN: 'tontines_refresh_token',
} as const;

/** Request timeout in ms – aggressive for 3G environments */
export const REQUEST_TIMEOUT_MS = 15_000;

/** Maximum retry attempts for transient failures */
export const MAX_RETRY_ATTEMPTS = 2;

/** Stale time for React Query cache (5 minutes) */
export const QUERY_STALE_TIME_MS = 5 * 60 * 1_000;

/** Cache time for React Query (30 minutes) */
export const QUERY_CACHE_TIME_MS = 30 * 60 * 1_000;

/** Number of days before tour closing to show urgency badge */
export const URGENCY_THRESHOLD_DAYS = 3;

/** Payment polling interval in ms */
export const PAYMENT_POLL_INTERVAL_MS = 5_000;

/** Maximum payment polling duration in ms (2 minutes) */
export const PAYMENT_MAX_POLL_DURATION_MS = 2 * 60 * 1_000;

/** AsyncStorage key for onboarding completion flag */
export const ONBOARDING_STORAGE_KEY = 'onboarding_done';
