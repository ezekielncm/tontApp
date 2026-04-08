/**
 * Dismissable error banner component.
 * Displays API errors or validation messages.
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { colors, spacing, fontSizes, borderRadius } from '../config/theme';

interface ErrorBannerProps {
  /** Error message to display */
  message: string;
  /** Callback to dismiss the banner */
  onDismiss?: () => void;
}

export function ErrorBanner({
  message,
  onDismiss,
}: ErrorBannerProps): React.JSX.Element {
  return (
    <View style={styles.container}>
      <Text style={styles.message}>{message}</Text>
      {onDismiss ? (
        <TouchableOpacity
          onPress={onDismiss}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          accessibilityRole="button"
          accessibilityLabel="Fermer le message d'erreur"
        >
          <Text style={styles.dismiss}>✕</Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.errorLight,
    borderWidth: 1,
    borderColor: colors.error,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
  },
  message: {
    flex: 1,
    color: colors.error,
    fontSize: fontSizes.sm,
  },
  dismiss: {
    color: colors.error,
    fontSize: fontSizes.lg,
    fontWeight: '700',
    marginLeft: spacing.sm,
  },
});
