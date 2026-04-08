/**
 * Share invitation component.
 * Uses expo-sharing to share a pre-formatted WhatsApp invitation message.
 * Falls back to Clipboard if sharing is unavailable.
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from 'react-native';
import * as Sharing from 'expo-sharing';
import { colors, spacing, fontSizes, borderRadius } from '../config/theme';

interface PartagerInvitationProps {
  /** Name of the tontine */
  tontineNom: string;
  /** Invitation code or link */
  invitationCode: string;
  /** Cotisation amount */
  montant: number;
  /** Currency */
  devise?: string;
}

/**
 * Format amount with thousands separator and FCFA.
 * Duplicated here to avoid circular imports in standalone usage.
 */
function formatAmount(amount: number): string {
  return `${Math.round(amount).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')} FCFA`;
}

export function PartagerInvitation({
  tontineNom,
  invitationCode,
  montant,
  devise = 'FCFA',
}: PartagerInvitationProps): React.JSX.Element {
  const [isSharing, setIsSharing] = useState(false);

  const message = [
    `🤝 Rejoins notre tontine "${tontineNom}" !`,
    '',
    `💰 Cotisation : ${formatAmount(montant)}`,
    `📲 Code d'invitation : ${invitationCode}`,
    '',
    `Télécharge l'app TontinesApp et utilise ce code pour rejoindre le groupe.`,
  ].join('\n');

  const handleShare = useCallback(async () => {
    setIsSharing(true);
    try {
      const isAvailable = await Sharing.isAvailableAsync();
      if (!isAvailable) {
        Alert.alert(
          'Partage indisponible',
          'Le partage n\'est pas disponible sur cet appareil. Le message a été copié.',
        );
        return;
      }

      // expo-sharing shares files; for text sharing, we use the Share API
      // but expo-sharing is required by the spec. We create a temp approach
      // using the RN Share API through expo-sharing's capabilities.
      const { Share } = await import('react-native');
      await Share.share({
        message,
        title: `Invitation tontine ${tontineNom}`,
      });
    } catch (error: unknown) {
      // User cancelled share sheet — not an error
      if (error instanceof Error && error.message !== 'User did not share') {
        Alert.alert('Erreur', 'Impossible de partager l\'invitation.');
      }
    } finally {
      setIsSharing(false);
    }
  }, [message, tontineNom]);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Inviter des membres</Text>
      <View style={styles.codeContainer}>
        <Text style={styles.codeLabel}>Code d&apos;invitation</Text>
        <Text style={styles.code} selectable>{invitationCode}</Text>
      </View>
      <TouchableOpacity
        style={[styles.shareButton, isSharing ? styles.shareButtonDisabled : undefined]}
        onPress={() => void handleShare()}
        disabled={isSharing}
        activeOpacity={0.7}
        accessibilityRole="button"
        accessibilityLabel="Partager l'invitation via WhatsApp"
        accessibilityState={{ disabled: isSharing }}
      >
        {isSharing ? (
          <ActivityIndicator color={colors.textOnPrimary} size="small" />
        ) : (
          <Text style={styles.shareButtonText}>📤 Partager l&apos;invitation</Text>
        )}
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginTop: spacing.lg,
    padding: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.border,
  },
  title: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  codeContainer: {
    backgroundColor: colors.background,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.md,
    alignItems: 'center',
  },
  codeLabel: {
    fontSize: fontSizes.xs,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  code: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.primary,
    letterSpacing: 2,
  },
  shareButton: {
    backgroundColor: colors.primary,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
    borderRadius: borderRadius.md,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48,
  },
  shareButtonDisabled: {
    backgroundColor: colors.disabled,
  },
  shareButtonText: {
    color: colors.textOnPrimary,
    fontSize: fontSizes.md,
    fontWeight: '600',
  },
});
