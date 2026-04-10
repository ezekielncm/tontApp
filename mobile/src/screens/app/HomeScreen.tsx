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

  const handleCreateTontine = useCallback(() => {
    navigation.navigate('CreateTontine');
  }, [navigation]);

  const handleNavigateNotifications = useCallback(() => {
    navigation.navigate('Notifications');
  }, [navigation]);

  const handleNavigateRejoindre = useCallback(() => {
    navigation.navigate('RejoindreParCode');
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
        <View style={styles.topBarActions}>
          <TouchableOpacity
            onPress={handleNavigateRejoindre}
            accessibilityRole="button"
            accessibilityLabel="Rejoindre une tontine"
            style={styles.topBarButton}
          >
            <Text style={styles.topBarIcon}>🤝</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={handleNavigateNotifications}
            accessibilityRole="button"
            accessibilityLabel="Notifications"
            style={styles.topBarButton}
          >
            <Text style={styles.topBarIcon}>🔔</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={handleNavigateProfil}
            accessibilityRole="button"
            accessibilityLabel="Voir mon profil"
          >
            <Text style={styles.profilLink}>Profil</Text>
          </TouchableOpacity>
        </View>
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
              <TouchableOpacity
                style={styles.createButtonEmpty}
                onPress={handleCreateTontine}
                activeOpacity={0.7}
              >
                <Text style={styles.createButtonEmptyText}>
                  + Créer ma première tontine
                </Text>
              </TouchableOpacity>
            </View>
          }
        />
      )}

      {/* Floating Action Button */}
      <TouchableOpacity
        style={styles.fab}
        onPress={handleCreateTontine}
        activeOpacity={0.8}
        accessibilityRole="button"
        accessibilityLabel="Créer une tontine"
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
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
  topBarActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  topBarButton: {
    padding: spacing.xs,
  },
  topBarIcon: {
    fontSize: 22,
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
    marginBottom: spacing.lg,
  },
  createButtonEmpty: {
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.xl,
    paddingVertical: spacing.md,
    borderRadius: 12,
  },
  createButtonEmptyText: {
    color: colors.textOnPrimary,
    fontSize: fontSizes.md,
    fontWeight: '600',
  },
  fab: {
    position: 'absolute',
    right: spacing.lg,
    bottom: spacing.xl,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    elevation: 6,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.27,
    shadowRadius: 4.65,
  },
  fabText: {
    fontSize: 28,
    color: colors.textOnPrimary,
    fontWeight: '300',
    lineHeight: 30,
  },
});
