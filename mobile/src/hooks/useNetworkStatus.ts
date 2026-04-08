/**
 * Offline-aware network status hook.
 * Provides isOnline flag and an explicit error message for write attempts
 * when offline (per constraint: "écriture = erreur explicite").
 */

import { useState, useEffect, useCallback } from 'react';
import { AppState, type AppStateStatus } from 'react-native';
import { onlineManager } from '@tanstack/react-query';

interface UseNetworkStatusReturn {
  /** Whether the device currently has network connectivity */
  isOnline: boolean;
  /** Assert that the device is online; throws a user-friendly error if not */
  assertOnline: () => void;
}

export function useNetworkStatus(): UseNetworkStatusReturn {
  const [isOnline, setIsOnline] = useState(onlineManager.isOnline());

  useEffect(() => {
    const unsubscribe = onlineManager.subscribe((online) => {
      setIsOnline(online);
    });
    return () => {
      unsubscribe();
    };
  }, []);

  // Refetch online status when app comes back to foreground
  useEffect(() => {
    const handleAppState = (state: AppStateStatus) => {
      if (state === 'active') {
        setIsOnline(onlineManager.isOnline());
      }
    };

    const subscription = AppState.addEventListener('change', handleAppState);
    return () => {
      subscription.remove();
    };
  }, []);

  const assertOnline = useCallback(() => {
    if (!isOnline) {
      throw new Error(
        'Vous êtes hors ligne. Cette action nécessite une connexion internet.',
      );
    }
  }, [isOnline]);

  return { isOnline, assertOnline };
}
