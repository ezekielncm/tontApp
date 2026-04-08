/**
 * Zustand store for tontine-related state.
 *
 * Stores:
 * - List of active tontines (cached for offline reading)
 * - Loading state
 */

import { create } from 'zustand';
import type { TontineSummary } from '../types/api';

export interface TontineState {
  /** Cached list of user's active tontines */
  activeTontines: TontineSummary[];
  /** Whether tontines are currently being fetched */
  isLoading: boolean;
}

export interface TontineActions {
  /** Replace the active tontines list */
  setActiveTontines: (tontines: TontineSummary[]) => void;
  /** Set loading state */
  setLoading: (loading: boolean) => void;
  /** Clear tontine state (on logout) */
  clearTontines: () => void;
}

export const useTontineStore = create<TontineState & TontineActions>()(
  (set) => ({
    activeTontines: [],
    isLoading: false,

    setActiveTontines: (tontines: TontineSummary[]) => {
      set({ activeTontines: tontines, isLoading: false });
    },

    setLoading: (loading: boolean) => {
      set({ isLoading: loading });
    },

    clearTontines: () => {
      set({ activeTontines: [], isLoading: false });
    },
  }),
);
