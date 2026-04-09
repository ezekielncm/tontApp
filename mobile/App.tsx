/**
 * TontinesApp – Root component.
 *
 * Sets up:
 * - React Query provider with offline-first caching
 * - Navigation container (AuthStack / AppStack based on auth state)
 * - StatusBar configuration (light theme only for MVP)
 */

import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RootNavigator } from './src/navigation/RootNavigator';
import { QUERY_STALE_TIME_MS, QUERY_CACHE_TIME_MS } from './src/config/constants';
import { setupNotificationHandler } from './src/services/notificationService';

// Configure foreground notification display before component tree mounts
setupNotificationHandler();

/**
 * React Query client configured for offline-first reading:
 * - staleTime: data is considered fresh for 5 minutes
 * - gcTime: cached data kept for 30 minutes
 * - retry: 2 attempts for failed queries
 * - networkMode: 'offlineFirst' serves cached data when offline
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: QUERY_STALE_TIME_MS,
      gcTime: QUERY_CACHE_TIME_MS,
      retry: 2,
      networkMode: 'offlineFirst',
    },
    mutations: {
      networkMode: 'online',
    },
  },
});

export default function App(): React.JSX.Element {
  return (
    <QueryClientProvider client={queryClient}>
      <StatusBar style="dark" />
      <RootNavigator />
    </QueryClientProvider>
  );
}
