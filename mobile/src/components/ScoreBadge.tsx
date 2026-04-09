/**
 * Visual badge displaying the credit score with color coding.
 * Green: Excellent (80-100), Blue: Bon (60-79), Orange: Moyen (40-59), Red: Faible (0-39)
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { fontSizes, spacing, borderRadius } from '../config/theme';

interface ScoreBadgeProps {
  score: number;
  niveau: string;
}

const NIVEAU_COLORS: Record<string, { bg: string; text: string; label: string }> = {
  Excellent: { bg: '#E8F5E9', text: '#2E7D32', label: 'Excellent' },
  Bon: { bg: '#E3F2FD', text: '#1565C0', label: 'Bon' },
  Moyen: { bg: '#FFF3E0', text: '#E65100', label: 'Moyen' },
  Faible: { bg: '#FFEBEE', text: '#C62828', label: 'Faible' },
};

export function ScoreBadge({ score, niveau }: ScoreBadgeProps): React.JSX.Element {
  const config = NIVEAU_COLORS[niveau] ?? NIVEAU_COLORS.Faible;

  return (
    <View style={[styles.container, { backgroundColor: config.bg }]}>
      <Text style={[styles.score, { color: config.text }]}>{score}</Text>
      <Text style={[styles.label, { color: config.text }]}>/100</Text>
      <View style={[styles.badge, { backgroundColor: config.text }]}>
        <Text style={styles.badgeText}>{config.label}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
  },
  score: {
    fontSize: 48,
    fontWeight: '700',
  },
  label: {
    fontSize: fontSizes.md,
    fontWeight: '500',
    marginTop: -spacing.xs,
  },
  badge: {
    marginTop: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.xs,
    borderRadius: borderRadius.full,
  },
  badgeText: {
    color: '#FFFFFF',
    fontSize: fontSizes.sm,
    fontWeight: '600',
  },
});
