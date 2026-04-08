/**
 * Progress bar component.
 * Displays a horizontal bar with completion percentage.
 * Accessible and performant.
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, spacing, fontSizes, borderRadius } from '../config/theme';

interface ProgressBarProps {
  /** Current value (0–max) */
  current: number;
  /** Maximum value */
  max: number;
  /** Optional label displayed above the bar */
  label?: string;
  /** Show percentage text */
  showPercentage?: boolean;
  /** Bar height in px */
  height?: number;
}

export function ProgressBar({
  current,
  max,
  label,
  showPercentage = true,
  height = 12,
}: ProgressBarProps): React.JSX.Element {
  const safeMax = Math.max(max, 1);
  const percentage = Math.min(Math.round((current / safeMax) * 100), 100);
  const barColor = percentage >= 100 ? colors.primary : percentage >= 50 ? colors.secondary : colors.error;

  return (
    <View style={styles.container} accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: safeMax, now: current }}>
      {label ? (
        <View style={styles.labelRow}>
          <Text style={styles.label}>{label}</Text>
          {showPercentage ? (
            <Text style={styles.percentage}>{percentage}%</Text>
          ) : null}
        </View>
      ) : showPercentage ? (
        <Text style={[styles.percentage, styles.percentageOnly]}>{percentage}%</Text>
      ) : null}
      <View style={[styles.track, { height }]}>
        <View
          style={[
            styles.fill,
            { width: `${percentage}%`, height, backgroundColor: barColor },
          ]}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginVertical: spacing.sm,
  },
  labelRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  label: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  percentage: {
    fontSize: fontSizes.sm,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  percentageOnly: {
    textAlign: 'right',
    marginBottom: spacing.xs,
  },
  track: {
    backgroundColor: colors.border,
    borderRadius: borderRadius.full,
    overflow: 'hidden',
  },
  fill: {
    borderRadius: borderRadius.full,
  },
});
