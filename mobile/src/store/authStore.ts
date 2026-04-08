/**
 * Zustand auth store with expo-secure-store persistence.
 *
 * State:
 * - user (id, telephone, nom)
 * - accessToken / refreshToken (JWT pair)
 * - isAuthenticated derived flag
 *
 * Persistence:
 * - Tokens are persisted to SecureStore (encrypted at rest on device)
 * - User profile is persisted alongside tokens
 * - On app start, hydrate() must be called to restore state
 */

import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import { SECURE_STORE_KEYS } from '../config/constants';

const USER_STORE_KEY = 'tontines_user';

export interface UserInfo {
  id: string;
  telephone: string;
  nom: string;
}

export interface AuthState {
  /** Current user profile, null when logged out */
  user: UserInfo | null;
  /** JWT access token */
  accessToken: string | null;
  /** JWT refresh token */
  refreshToken: string | null;
  /** Whether the store has been hydrated from SecureStore */
  isHydrated: boolean;
  /** Derived: true when we have a user and access token */
  isAuthenticated: boolean;
}

export interface AuthActions {
  /** Persist tokens to SecureStore and update state */
  setTokens: (accessToken: string, refreshToken: string) => void;
  /** Set user info and persist to SecureStore */
  setUser: (user: UserInfo) => void;
  /** Full login: set user + tokens + persist everything */
  setAuth: (user: UserInfo, accessToken: string, refreshToken: string) => void;
  /** Clear all auth state and SecureStore entries */
  clearAuth: () => void;
  /** Hydrate state from SecureStore on app start */
  hydrate: () => Promise<void>;
}

export const useAuthStore = create<AuthState & AuthActions>()((set) => ({
  // ─── Initial State ─────────────────────────────────────────────────────────
  user: null,
  accessToken: null,
  refreshToken: null,
  isHydrated: false,
  isAuthenticated: false,

  // ─── Actions ───────────────────────────────────────────────────────────────

  setTokens: (accessToken: string, refreshToken: string) => {
    // Update state immediately for responsiveness, persist in background
    set((state) => ({
      accessToken,
      refreshToken,
      isAuthenticated: state.user !== null,
    }));
    void Promise.all([
      SecureStore.setItemAsync(SECURE_STORE_KEYS.ACCESS_TOKEN, accessToken),
      SecureStore.setItemAsync(SECURE_STORE_KEYS.REFRESH_TOKEN, refreshToken),
    ]);
  },

  setUser: (user: UserInfo) => {
    set({
      user,
      isAuthenticated: true,
    });
    void SecureStore.setItemAsync(USER_STORE_KEY, JSON.stringify(user));
  },

  setAuth: (user: UserInfo, accessToken: string, refreshToken: string) => {
    // Update state immediately for responsiveness, persist in background
    set({
      user,
      accessToken,
      refreshToken,
      isAuthenticated: true,
    });
    void Promise.all([
      SecureStore.setItemAsync(SECURE_STORE_KEYS.ACCESS_TOKEN, accessToken),
      SecureStore.setItemAsync(SECURE_STORE_KEYS.REFRESH_TOKEN, refreshToken),
      SecureStore.setItemAsync(USER_STORE_KEY, JSON.stringify(user)),
    ]);
  },

  clearAuth: () => {
    set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
    });
    void Promise.all([
      SecureStore.deleteItemAsync(SECURE_STORE_KEYS.ACCESS_TOKEN),
      SecureStore.deleteItemAsync(SECURE_STORE_KEYS.REFRESH_TOKEN),
      SecureStore.deleteItemAsync(USER_STORE_KEY),
    ]);
  },

  hydrate: async () => {
    try {
      const [accessToken, refreshToken, userJson] = await Promise.all([
        SecureStore.getItemAsync(SECURE_STORE_KEYS.ACCESS_TOKEN),
        SecureStore.getItemAsync(SECURE_STORE_KEYS.REFRESH_TOKEN),
        SecureStore.getItemAsync(USER_STORE_KEY),
      ]);

      const user = userJson ? (JSON.parse(userJson) as UserInfo) : null;

      set({
        accessToken: accessToken ?? null,
        refreshToken: refreshToken ?? null,
        user,
        isAuthenticated: !!(user && accessToken),
        isHydrated: true,
      });
    } catch {
      // If SecureStore fails, start with clean state
      set({ isHydrated: true });
    }
  },
}));
