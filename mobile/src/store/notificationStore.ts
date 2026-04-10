/**
 * Zustand notification store.
 * Stores in-app notifications locally.
 * Populated from push notification handler (expo-notifications listener).
 */

import { create } from 'zustand';
import type { NotificationItem } from '../types/api';

export interface NotificationState {
  notifications: NotificationItem[];
  addNotification: (notification: NotificationItem) => void;
  markAsRead: (id: string) => void;
  clearAll: () => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],

  addNotification: (notification) =>
    set((state) => ({
      notifications: [notification, ...state.notifications],
    })),

  markAsRead: (id) =>
    set((state) => ({
      notifications: state.notifications.map((n) =>
        n.id === id ? { ...n, lu: true } : n,
      ),
    })),

  clearAll: () => set({ notifications: [] }),
}));
