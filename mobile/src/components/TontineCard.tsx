/**
 * TontineCard – reusable card component for tontine list display.
 * Shows name, current tour, amount, member count, status badge.
 * Displays urgency badge (J-3) when tour closing is within 3 days.
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import type { TontineSummary } from '../types/api';
import { formatMontant, daysUntil } from '../utils/format';
import {
  colors,
  spacing,
  fontSizes,
  borderRadius,
} from '../config/theme';
import { URGENCY_THRESHOLD_DAYS } from '../config/constants';

interface TontineCardProps {
  /** Tontine summary data */
  tontine: TontineSummary;
  /** Navigation handler */
  onPress: (tontineId: string) => void;
}

export function TontineCard({
  tontine,
  onPress,
}: TontineCardProps): React.JSX.Element {
  const daysRemaining =
    tontine.tourActuel?.dateCloture
      ? daysUntil(tontine.tourActuel.dateCloture)
      : null;
  const isUrgent = daysRemaining !== null && daysRemaining <= URGENCY_THRESHOLD_DAYS && daysRemaining > 0;
  const isExpired = daysRemaining === 0 && tontine.tourActuel?.dateCloture != null;

  return (
    <TouchableOpacity
      style={[styles.card, isUrgent ? styles.cardUrgent : undefined]}
      onPress={() => onPress(tontine.id)}
      activeOpacity={0.7}
      accessibilityRole="button"
      accessibilityLabel={`Tontine ${tontine.nom}, ${tontine.status}`}
    >
      <View style={styles.header}>
        <Text style={styles.title} numberOfLines={1}>
          {tontine.nom}
        </Text>
        <View style={styles.badges}>
          {isUrgent ? (
            <View style={styles.urgencyBadge} accessibilityLabel={`Urgent: ${daysRemaining} jours restants`}>
              <Text style={styles.urgencyText}>J-{daysRemaining}</Text>
            </View>
          ) : null}
          {isExpired ? (
            <View style={styles.expiredBadge} accessibilityLabel="Tour expiré">
              <Text style={styles.urgencyText}>Expiré</Text>
            </View>
          ) : null}
          <View
            style={[
              styles.statusBadge,
              tontine.status === 'Active'
                ? styles.statusActive
                : styles.statusDefault,
            ]}
          >
            <Text style={styles.statusText}>{tontine.status}</Text>
          </View>
        </View>
      </View>

      {tontine.tourActuel ? (
        <Text style={styles.tourInfo} numberOfLines={1}>
          Tour #{tontine.tourActuel.numero} – {tontine.tourActuel.beneficiaireNom}
        </Text>
      ) : null}

      <Text style={styles.amount}>
        {formatMontant(tontine.montantCotisation)} / {tontine.frequence}
      </Text>

      <Text style={styles.members}>
        {tontine.nombreMembres} membre{tontine.nombreMembres > 1 ? 's' : ''}
      </Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardUrgent: {
    borderColor: colors.error,
    borderWidth: 2,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  title: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    flex: 1,
    marginRight: spacing.sm,
  },
  badges: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  urgencyBadge: {
    backgroundColor: colors.error,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
  },
  expiredBadge: {
    backgroundColor: colors.textSecondary,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
  },
  urgencyText: {
    fontSize: fontSizes.xs,
    color: colors.textOnPrimary,
    fontWeight: '700',
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
  tourInfo: {
    fontSize: fontSizes.sm,
    color: colors.primary,
    fontWeight: '500',
    marginBottom: spacing.xs,
  },
  amount: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  members: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
});
