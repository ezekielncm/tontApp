/**
 * Axios-based API client with:
 * - JWT Bearer token injection via request interceptor
 * - Automatic token refresh on 401 responses
 * - Request retry after successful refresh
 * - Request timeout for 3G environments
 * - Queue mechanism to prevent concurrent refresh calls
 */

import axios, {
  AxiosError,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import {
  API_BASE_URL,
  API_PREFIX,
  REQUEST_TIMEOUT_MS,
  MAX_RETRY_ATTEMPTS,
} from '../config/constants';
import { useAuthStore } from '../store/authStore';
import type { AuthResult, RefreshRequest } from '../types/api';

/** Extended config to track retry state */
interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retryCount?: number;
  _isRetry?: boolean;
}

const apiClient = axios.create({
  baseURL: `${API_BASE_URL}${API_PREFIX}`,
  timeout: REQUEST_TIMEOUT_MS,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
});

// ─── Request Interceptor: Attach JWT ───────────────────────────────────────────

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig): InternalAxiosRequestConfig => {
    const { accessToken } = useAuthStore.getState();
    if (accessToken && config.headers) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error: AxiosError) => Promise.reject(error),
);

// ─── Response Interceptor: Auto-refresh on 401 ────────────────────────────────

/**
 * Pending promise used to serialise concurrent 401-triggered refreshes.
 * When a 401 fires, the first caller sets this promise and starts the refresh.
 * Subsequent callers await the same promise instead of triggering parallel refreshes.
 */
let refreshPromise: Promise<AuthResult> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;

    // Only attempt refresh for 401 responses that haven't already been retried
    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._isRetry
    ) {
      return Promise.reject(error);
    }

    const { refreshToken, setTokens, clearAuth } = useAuthStore.getState();

    // No refresh token available – force logout
    if (!refreshToken) {
      clearAuth();
      return Promise.reject(error);
    }

    originalRequest._isRetry = true;
    originalRequest._retryCount = (originalRequest._retryCount ?? 0) + 1;

    if (originalRequest._retryCount > MAX_RETRY_ATTEMPTS) {
      clearAuth();
      return Promise.reject(error);
    }

    try {
      // Serialize concurrent refresh attempts
      if (!refreshPromise) {
        refreshPromise = performTokenRefresh(refreshToken);
      }

      const result = await refreshPromise;
      setTokens(result.accessToken, result.refreshToken);

      // Retry original request with new token
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${result.accessToken}`;
      }

      return apiClient(originalRequest);
    } catch (refreshError) {
      clearAuth();
      return Promise.reject(refreshError);
    } finally {
      refreshPromise = null;
    }
  },
);

/**
 * Performs the actual token refresh call.
 * Uses a raw axios instance to avoid interceptor loops.
 */
async function performTokenRefresh(
  currentRefreshToken: string,
): Promise<AuthResult> {
  const body: RefreshRequest = { refreshToken: currentRefreshToken };

  const response = await axios.post<AuthResult>(
    `${API_BASE_URL}${API_PREFIX}/auth/refresh`,
    body,
    {
      headers: { 'Content-Type': 'application/json' },
      timeout: REQUEST_TIMEOUT_MS,
    },
  );

  return response.data;
}

export default apiClient;
