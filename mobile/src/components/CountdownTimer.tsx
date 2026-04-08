/**
 * Countdown timer component.
 * Shows hours:minutes:seconds until a target date.
 * Updates every second. Shows "Terminé" when expired.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, spacing, fontSizes, borderRadius } from '../config/theme';
import { timeRemaining } from '../utils/format';

interface CountdownTimerProps {
  /** ISO 8601 target date string */
  targetDate: string;
  /** Label displayed above the countdown */
  label?: string;
}

export function CountdownTimer({
  targetDate,
  label = 'Temps restant',
}: CountdownTimerProps): React.JSX.Element {
  const computeRemaining = useCallback(
    () => timeRemaining(targetDate),
    [targetDate],
  );
  const [remaining, setRemaining] = useState(computeRemaining);

  useEffect(() => {
    setRemaining(computeRemaining());
    const interval = setInterval(() => {
      setRemaining(computeRemaining());
    }, 1000);
    return () => clearInterval(interval);
  }, [computeRemaining]);

  const pad = (n: number): string => n.toString().padStart(2, '0');

  const isUrgent = !remaining.isExpired && remaining.totalSeconds <= 3 * 24 * 3600;

  return (
    <View
      style={styles.container}
      accessibilityLabel={
        remaining.isExpired
          ? 'Temps écoulé'
          : `${label}: ${remaining.hours} heures ${remaining.minutes} minutes ${remaining.seconds} secondes`
      }
      accessibilityRole="timer"
    >
      <Text style={styles.label}>{label}</Text>
      {remaining.isExpired ? (
        <Text style={styles.expired}>Terminé</Text>
      ) : (
        <View style={styles.timerRow}>
          <View style={[styles.timerBlock, isUrgent ? styles.urgent : undefined]}>
            <Text style={[styles.timerDigit, isUrgent ? styles.urgentText : undefined]}>
              {pad(remaining.hours)}
            </Text>
            <Text style={styles.timerUnit}>h</Text>
          </View>
          <Text style={[styles.separator, isUrgent ? styles.urgentText : undefined]}>:</Text>
          <View style={[styles.timerBlock, isUrgent ? styles.urgent : undefined]}>
            <Text style={[styles.timerDigit, isUrgent ? styles.urgentText : undefined]}>
              {pad(remaining.minutes)}
            </Text>
            <Text style={styles.timerUnit}>m</Text>
          </View>
          <Text style={[styles.separator, isUrgent ? styles.urgentText : undefined]}>:</Text>
          <View style={[styles.timerBlock, isUrgent ? styles.urgent : undefined]}>
            <Text style={[styles.timerDigit, isUrgent ? styles.urgentText : undefined]}>
              {pad(remaining.seconds)}
            </Text>
            <Text style={styles.timerUnit}>s</Text>
          </View>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    marginVertical: spacing.md,
  },
  label: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    marginBottom: spacing.sm,
    fontWeight: '500',
  },
  timerRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  timerBlock: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    minWidth: 60,
  },
  timerDigit: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
  },
  timerUnit: {
    fontSize: fontSizes.xs,
    color: colors.textSecondary,
    marginTop: 2,
  },
  separator: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginHorizontal: spacing.xs,
  },
  expired: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.error,
  },
  urgent: {
    borderColor: colors.error,
    backgroundColor: '#FFEBEE',
  },
  urgentText: {
    color: colors.error,
  },
});
