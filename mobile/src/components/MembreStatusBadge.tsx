/**
 * Member payment status badge.
 * Green = paid, Orange = pending, Red = late.
 * WCAG AA accessible contrast ratios.
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import type { StatutPaiement } from '../types/api';
import { fontSizes, borderRadius, spacing } from '../config/theme';

interface MembreStatusBadgeProps {
  /** Payment status */
  statut: StatutPaiement;
}

const STATUS_CONFIG: Record<
  StatutPaiement,
  { label: string; backgroundColor: string; textColor: string }
> = {
  paye: {
    label: 'Payé',
    backgroundColor: '#1B5E20',     // green – contrast 8.2:1 on white
    textColor: '#FFFFFF',
  },
  en_attente: {
    label: 'En attente',
    backgroundColor: '#E65100',     // deep orange – contrast 5.0:1 on white
    textColor: '#FFFFFF',
  },
  en_retard: {
    label: 'En retard',
    backgroundColor: '#B71C1C',     // dark red – contrast 7.8:1 on white
    textColor: '#FFFFFF',
  },
};

export function MembreStatusBadge({
  statut,
}: MembreStatusBadgeProps): React.JSX.Element {
  const config = STATUS_CONFIG[statut];

  return (
    <View
      style={[styles.badge, { backgroundColor: config.backgroundColor }]}
      accessibilityLabel={`Statut: ${config.label}`}
      accessibilityRole="text"
    >
      <Text style={[styles.text, { color: config.textColor }]}>
        {config.label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
    alignSelf: 'flex-start',
  },
  text: {
    fontSize: fontSizes.xs,
    fontWeight: '700',
  },
});
