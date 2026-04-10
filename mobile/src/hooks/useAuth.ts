/**
 * useAuth() hook – single entry point for authentication operations.
 *
 * Provides:
 * - login(telephone, motDePasse) → logs in and persists tokens
 * - register(telephone, nom, motDePasse) → registers and persists tokens
 * - logout() → revokes tokens server-side and clears local state
 * - refreshToken() → refreshes the JWT pair
 * - isLoading, error state for UI feedback
 *
 * All methods handle errors and expose them via the `error` field.
 */

import { useState, useCallback } from 'react';
import { AxiosError } from 'axios';
import { authService } from '../services/authService';
import { useAuthStore, type UserInfo } from '../store/authStore';
import { useTontineStore } from '../store/tontineStore';
import type { ApiError, AuthResult, RegisterResult } from '../types/api';

interface UseAuthReturn {
  /** Log in with phone and password */
  login: (telephone: string, motDePasse: string) => Promise<boolean>;
  /** Register a new account (sends OTP, does not authenticate) */
  register: (
    telephone: string,
    nom: string,
    motDePasse: string,
  ) => Promise<boolean>;
  /** Verify OTP and authenticate */
  verifierOtp: (telephone: string, nom: string, code: string) => Promise<boolean>;
  /** Log out (server revoke + local clear) */
  logout: () => Promise<void>;
  /** Manually refresh the token pair */
  refreshToken: () => Promise<boolean>;
  /** Whether an auth operation is in progress */
  isLoading: boolean;
  /** Last error message, null if no error */
  error: string | null;
  /** Clear the current error */
  clearError: () => void;
}

export function useAuth(): UseAuthReturn {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const setAuth = useAuthStore((s) => s.setAuth);
  const setTokens = useAuthStore((s) => s.setTokens);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const storedRefreshToken = useAuthStore((s) => s.refreshToken);
  const clearTontines = useTontineStore((s) => s.clearTontines);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  /**
   * Extract a user-friendly error message from an Axios error.
   */
  const extractErrorMessage = useCallback(
    (err: unknown): string => {
      if (err instanceof AxiosError) {
        const data = err.response?.data as ApiError | undefined;
        if (data?.error) {
          return data.error;
        }
        if (!err.response) {
          return 'Erreur réseau. Vérifiez votre connexion internet.';
        }
        return `Erreur serveur (${String(err.response.status)})`;
      }
      if (err instanceof Error) {
        return err.message;
      }
      return 'Une erreur inattendue est survenue.';
    },
    [],
  );

  /**
   * Build a UserInfo from an AuthResult.
   * The backend returns utilisateurId; we store it along with
   * the credentials used for the request until profile endpoint is available.
   */
  const buildUser = useCallback(
    (result: AuthResult, telephone: string, nom: string): UserInfo => ({
      id: result.utilisateurId,
      telephone,
      nom,
    }),
    [],
  );

  const login = useCallback(
    async (telephone: string, motDePasse: string): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await authService.login({ telephone, motDePasse });
        const user = buildUser(result, telephone, '');
        setAuth(user, result.accessToken, result.refreshToken);
        return true;
      } catch (err: unknown) {
        setError(extractErrorMessage(err));
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [setAuth, buildUser, extractErrorMessage],
  );

  const register = useCallback(
    async (
      telephone: string,
      nom: string,
      motDePasse: string,
    ): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        await authService.register({
          telephone,
          nom,
          motDePasse,
        });
        // OTP sent – caller should navigate to VerifierOtp screen
        return true;
      } catch (err: unknown) {
        setError(extractErrorMessage(err));
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [extractErrorMessage],
  );

  const verifierOtp = useCallback(
    async (telephone: string, nom: string, code: string): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await authService.verifierOtp({ telephone, code });
        const user = buildUser(result, telephone, nom);
        setAuth(user, result.accessToken, result.refreshToken);
        return true;
      } catch (err: unknown) {
        setError(extractErrorMessage(err));
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [setAuth, buildUser, extractErrorMessage],
  );

  const logout = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);
    try {
      await authService.logout();
    } catch {
      // Ignore server errors during logout – clear local state regardless
    } finally {
      clearAuth();
      clearTontines();
      setIsLoading(false);
    }
  }, [clearAuth, clearTontines]);

  const refreshToken = useCallback(async (): Promise<boolean> => {
    if (!storedRefreshToken) {
      setError('Aucun jeton de rafraîchissement disponible.');
      return false;
    }
    setIsLoading(true);
    setError(null);
    try {
      const result = await authService.refresh({
        refreshToken: storedRefreshToken,
      });
      setTokens(result.accessToken, result.refreshToken);
      return true;
    } catch (err: unknown) {
      setError(extractErrorMessage(err));
      clearAuth();
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [storedRefreshToken, setTokens, clearAuth, extractErrorMessage]);

  return {
    login,
    register,
    verifierOtp,
    logout,
    refreshToken,
    isLoading,
    error,
    clearError,
  };
}
