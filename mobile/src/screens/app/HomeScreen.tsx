/**
 * Home screen – displays the user's active tontines list.
 * Uses React Query for cache-first offline reading.
 * Features: TontineCard with urgency badge (J-3), skeleton loaders, pull-to-refresh.
 */

import React, { useCallback, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  RefreshControl,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import type { TontineSummary } from '../../types/api';
import { tontineService } from '../../services/tontineService';
import { ErrorBanner } from '../../components/ErrorBanner';
import { TontineCard } from '../../components/TontineCard';
import { HomeScreenSkeleton } from '../../components/SkeletonLoader';
import { colors, spacing, fontSizes } from '../../config/theme';
import { registerForPushNotifications } from '../../services/notificationService';
import {
  QUERY_STALE_TIME_MS,
  QUERY_CACHE_TIME_MS,
} from '../../config/constants';

type Props = NativeStackScreenProps<AppStackParamList, 'Home'>;

export function HomeScreen({ navigation }: Props): React.JSX.Element {
  const {
    data: tontines,
    isLoading,
    error,
    refetch,
    isRefetching,
  } = useQuery<TontineSummary[], Error>({
    queryKey: ['tontines'],
    queryFn: tontineService.getMyTontines,
    staleTime: QUERY_STALE_TIME_MS,
    gcTime: QUERY_CACHE_TIME_MS,
  });

  // Request push notification permission on first mount
  useEffect(() => {
    void registerForPushNotifications();
  }, []);

  const handleNavigateDetail = useCallback(
    (tontineId: string) => {
      navigation.navigate('TontineDetail', { tontineId });
    },
    [navigation],
  );

  const handleNavigateProfil = useCallback(() => {
    navigation.navigate('Profil');
  }, [navigation]);

  const renderItem = useCallback(
    ({ item }: { item: TontineSummary }) => (
      <TontineCard tontine={item} onPress={handleNavigateDetail} />
    ),
    [handleNavigateDetail],
  );

  const keyExtractor = useCallback((item: TontineSummary) => item.id, []);

  return (
    <View style={styles.container}>
      <View style={styles.topBar}>
        <Text style={styles.greeting}>Mes Tontines</Text>
        <TouchableOpacity
          onPress={handleNavigateProfil}
          accessibilityRole="button"
          accessibilityLabel="Voir mon profil"
        >
          <Text style={styles.profilLink}>Profil</Text>
        </TouchableOpacity>
      </View>

      {error ? <ErrorBanner message={error.message} /> : null}

      {isLoading && !tontines ? (
        <HomeScreenSkeleton />
      ) : (
        <FlatList
          data={tontines ?? []}
          renderItem={renderItem}
          keyExtractor={keyExtractor}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={isRefetching}
              onRefresh={() => void refetch()}
              colors={[colors.primary]}
            />
          }
          ListEmptyComponent={
            <View style={styles.centered}>
              <Text style={styles.emptyText}>
                Aucune tontine pour le moment.
              </Text>
            </View>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  topBar: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
  greeting: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
  },
  profilLink: {
    fontSize: fontSizes.md,
    color: colors.primary,
    fontWeight: '600',
  },
  list: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl,
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: spacing.xxl,
  },
  emptyText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
});
