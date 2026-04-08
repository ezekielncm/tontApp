/**
 * Home screen – displays the user's active tontines list.
 * Uses React Query for cache-first offline reading.
 * Navigation to TontineDetail and Profil.
 */

import React, { useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import type { TontineSummary } from '../../types/api';
import { tontineService } from '../../services/tontineService';
import { ErrorBanner } from '../../components/ErrorBanner';
import {
  colors,
  spacing,
  fontSizes,
  borderRadius,
} from '../../config/theme';
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
      <TouchableOpacity
        style={styles.card}
        onPress={() => handleNavigateDetail(item.id)}
        activeOpacity={0.7}
      >
        <View style={styles.cardHeader}>
          <Text style={styles.cardTitle} numberOfLines={1}>
            {item.nom}
          </Text>
          <View
            style={[
              styles.statusBadge,
              item.status === 'Active'
                ? styles.statusActive
                : styles.statusDefault,
            ]}
          >
            <Text style={styles.statusText}>{item.status}</Text>
          </View>
        </View>
        <Text style={styles.cardAmount}>
          {item.montantCotisation.toLocaleString('fr-FR')} {item.devise} /{' '}
          {item.frequence}
        </Text>
        <Text style={styles.cardMembers}>
          {item.nombreMembres} membre{item.nombreMembres > 1 ? 's' : ''}
        </Text>
      </TouchableOpacity>
    ),
    [handleNavigateDetail],
  );

  const keyExtractor = useCallback((item: TontineSummary) => item.id, []);

  return (
    <View style={styles.container}>
      <View style={styles.topBar}>
        <Text style={styles.greeting}>Mes Tontines</Text>
        <TouchableOpacity onPress={handleNavigateProfil}>
          <Text style={styles.profilLink}>Profil</Text>
        </TouchableOpacity>
      </View>

      {error ? (
        <ErrorBanner message={error.message} />
      ) : null}

      {isLoading && !tontines ? (
        <View style={styles.centered}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
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
  card: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  cardTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    flex: 1,
    marginRight: spacing.sm,
  },
  statusBadge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
  },
  statusActive: {
    backgroundColor: colors.primaryLight,
  },
  statusDefault: {
    backgroundColor: colors.disabled,
  },
  statusText: {
    fontSize: fontSizes.xs,
    color: colors.textOnPrimary,
    fontWeight: '600',
  },
  cardAmount: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  cardMembers: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
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
