/**
 * Historique Versements Screen.
 * Lists all payments made by the authenticated user,
 * optionally filtered by tontine.
 */

import React, { useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { tontineService } from '../../services/tontineService';
import { formatMontant } from '../../utils/format';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';
import { QUERY_STALE_TIME_MS } from '../../config/constants';
import type { VersementDto } from '../../types/api';

type Props = NativeStackScreenProps<AppStackParamList, 'HistoriqueVersements'>;

function StatusBadgeForVersement({ statut }: { statut: string }): React.JSX.Element {
  const config: Record<string, { label: string; color: string; bg: string }> = {
    Confirme: { label: 'Confirmé', color: '#1B5E20', bg: '#E8F5E9' },
    EnAttente: { label: 'En attente', color: '#E65100', bg: '#FFF3E0' },
    Rejete: { label: 'Rejeté', color: '#B71C1C', bg: '#FFEBEE' },
    Initie: { label: 'Initié', color: '#0D47A1', bg: '#E3F2FD' },
  };

  const s = config[statut] ?? { label: statut, color: colors.textSecondary, bg: colors.border };

  return (
    <View style={[styles.badge, { backgroundColor: s.bg }]}>
      <Text style={[styles.badgeText, { color: s.color }]}>{s.label}</Text>
    </View>
  );
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString('fr-FR', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function HistoriqueVersementsScreen({
  route,
}: Props): React.JSX.Element {
  const tontineId = route.params?.tontineId;

  const {
    data: versements,
    isLoading,
    isError,
    refetch,
    isRefetching,
  } = useQuery({
    queryKey: ['mesVersements', tontineId],
    queryFn: () => tontineService.getMesVersements(tontineId),
    staleTime: QUERY_STALE_TIME_MS,
  });

  const renderItem = useCallback(
    ({ item }: { item: VersementDto }) => (
      <View style={styles.card}>
        <View style={styles.cardHeader}>
          <Text style={styles.montant}>{formatMontant(item.montant)}</Text>
          <StatusBadgeForVersement statut={item.statut} />
        </View>
        <Text style={styles.date}>{formatDate(item.createdAt)}</Text>
        {item.referenceExterne && (
          <Text style={styles.reference}>Réf: {item.referenceExterne}</Text>
        )}
        {item.confirmedAt && (
          <Text style={styles.confirmedAt}>
            Confirmé le {formatDate(item.confirmedAt)}
          </Text>
        )}
      </View>
    ),
    [],
  );

  const renderEmpty = useCallback(
    () => (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyEmoji}>💸</Text>
        <Text style={styles.emptyTitle}>Aucun versement</Text>
        <Text style={styles.emptySubtitle}>
          Vous n'avez effectué aucun paiement pour le moment.
        </Text>
      </View>
    ),
    [],
  );

  if (isLoading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color={colors.primary} />
        <Text style={styles.loadingText}>Chargement des versements...</Text>
      </View>
    );
  }

  if (isError) {
    return (
      <View style={styles.centered}>
        <Text style={styles.emptyEmoji}>⚠️</Text>
        <Text style={styles.errorText}>
          Impossible de charger l'historique.
        </Text>
      </View>
    );
  }

  // Summary stats
  const totalConfirme =
    versements
      ?.filter((v) => v.statut === 'Confirme')
      .reduce((sum, v) => sum + v.montant, 0) ?? 0;
  const count = versements?.length ?? 0;

  return (
    <View style={styles.container}>
      {count > 0 && (
        <View style={styles.summaryCard}>
          <View style={styles.summaryItem}>
            <Text style={styles.summaryValue}>{count}</Text>
            <Text style={styles.summaryLabel}>Versements</Text>
          </View>
          <View style={styles.summaryDivider} />
          <View style={styles.summaryItem}>
            <Text style={styles.summaryValue}>
              {formatMontant(totalConfirme)}
            </Text>
            <Text style={styles.summaryLabel}>Total confirmé</Text>
          </View>
        </View>
      )}

      <FlatList
        data={versements}
        keyExtractor={(item) => item.id}
        renderItem={renderItem}
        ListEmptyComponent={renderEmpty}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={isRefetching}
            onRefresh={refetch}
            colors={[colors.primary]}
            tintColor={colors.primary}
          />
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.lg,
  },
  loadingText: {
    marginTop: spacing.md,
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
  errorText: {
    fontSize: fontSizes.md,
    color: colors.error,
    textAlign: 'center',
    marginTop: spacing.sm,
  },
  summaryCard: {
    flexDirection: 'row',
    backgroundColor: colors.primary,
    margin: spacing.md,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
  },
  summaryItem: {
    flex: 1,
    alignItems: 'center',
  },
  summaryDivider: {
    width: 1,
    backgroundColor: 'rgba(255,255,255,0.3)',
  },
  summaryValue: {
    fontSize: fontSizes.lg,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  summaryLabel: {
    fontSize: fontSizes.xs,
    color: 'rgba(255,255,255,0.8)',
    marginTop: spacing.xs,
  },
  listContent: {
    padding: spacing.md,
    paddingBottom: spacing.xxl,
    flexGrow: 1,
  },
  card: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
    borderLeftWidth: 4,
    borderLeftColor: colors.primary,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  montant: {
    fontSize: fontSizes.lg,
    fontWeight: '700',
    color: colors.textPrimary,
  },
  date: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  reference: {
    fontSize: fontSizes.xs,
    color: colors.textSecondary,
    marginTop: spacing.xs,
  },
  confirmedAt: {
    fontSize: fontSizes.xs,
    color: colors.primary,
    marginTop: spacing.xs,
  },
  badge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
  },
  badgeText: {
    fontSize: fontSizes.xs,
    fontWeight: '600',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: spacing.xxl * 2,
  },
  emptyEmoji: {
    fontSize: 48,
    marginBottom: spacing.md,
  },
  emptyTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  emptySubtitle: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
  },
});
