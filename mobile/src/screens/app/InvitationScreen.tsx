/**
 * Invitation Screen.
 * Allows a gestionnaire to generate an invitation code for their tontine
 * and share it via WhatsApp/other apps using the PartagerInvitation component.
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ActivityIndicator,
  ScrollView,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { tontineService } from '../../services/tontineService';
import { PartagerInvitation } from '../../components/PartagerInvitation';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';
import type { GenererCodeInvitationResponse } from '../../types/api';

type Props = NativeStackScreenProps<AppStackParamList, 'Invitation'>;

export function InvitationScreen({
  route,
}: Props): React.JSX.Element {
  const { tontineId, tontineNom, montant } = route.params;
  const [invitation, setInvitation] =
    useState<GenererCodeInvitationResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleGenerate = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await tontineService.genererCodeInvitation(tontineId);
      setInvitation(result);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError(
          "Impossible de générer le code d'invitation. La tontine doit être en brouillon.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [tontineId]);

  const formatExpiration = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.scrollContent}
    >
      <View style={styles.header}>
        <Text style={styles.emoji}>📨</Text>
        <Text style={styles.title}>Inviter des membres</Text>
        <Text style={styles.subtitle}>
          Générez un code d'invitation pour permettre à de nouveaux membres de
          rejoindre <Text style={styles.tontineName}>{tontineNom}</Text>.
        </Text>
      </View>

      {error && <ErrorBanner message={error} />}

      {!invitation ? (
        <View style={styles.generateSection}>
          <View style={styles.infoCard}>
            <Text style={styles.infoIcon}>💡</Text>
            <Text style={styles.infoText}>
              Le code d'invitation est valable 7 jours et peut être utilisé une
              seule fois. Vous pouvez en générer autant que nécessaire.
            </Text>
          </View>

          <PrimaryButton
            title="Générer un code d'invitation"
            onPress={handleGenerate}
            loading={isLoading}
          />
        </View>
      ) : (
        <View style={styles.resultSection}>
          <View style={styles.codeCard}>
            <Text style={styles.codeLabel}>Code d'invitation</Text>
            <Text style={styles.codeValue}>{invitation.code}</Text>
            <Text style={styles.expiration}>
              Expire le {formatExpiration(invitation.expiration)}
            </Text>
          </View>

          <PartagerInvitation
            tontineNom={tontineNom}
            invitationCode={invitation.code}
            montant={montant}
          />

          <View style={styles.divider} />

          <PrimaryButton
            title="Générer un nouveau code"
            onPress={handleGenerate}
            loading={isLoading}
          />
        </View>
      )}

      {isLoading && !invitation && (
        <ActivityIndicator
          size="large"
          color={colors.primary}
          style={styles.loader}
        />
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  scrollContent: {
    padding: spacing.lg,
    paddingBottom: spacing.xxl,
  },
  header: {
    alignItems: 'center',
    marginBottom: spacing.xl,
  },
  emoji: {
    fontSize: 48,
    marginBottom: spacing.md,
  },
  title: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
    lineHeight: 22,
  },
  tontineName: {
    fontWeight: '700',
    color: colors.primary,
  },
  generateSection: {
    marginTop: spacing.lg,
  },
  resultSection: {
    marginTop: spacing.md,
  },
  codeCard: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.xl,
    alignItems: 'center',
    marginBottom: spacing.lg,
    borderWidth: 2,
    borderColor: colors.primary,
  },
  codeLabel: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    marginBottom: spacing.sm,
  },
  codeValue: {
    fontSize: 32,
    fontWeight: '800',
    color: colors.primary,
    letterSpacing: 4,
    marginBottom: spacing.sm,
  },
  expiration: {
    fontSize: fontSizes.xs,
    color: colors.textSecondary,
  },
  infoCard: {
    flexDirection: 'row',
    backgroundColor: colors.primaryLight,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.xl,
  },
  infoIcon: {
    fontSize: 16,
    marginRight: spacing.sm,
    marginTop: 2,
  },
  infoText: {
    flex: 1,
    fontSize: fontSizes.sm,
    color: colors.textPrimary,
    lineHeight: 20,
  },
  divider: {
    height: 1,
    backgroundColor: colors.border,
    marginVertical: spacing.lg,
  },
  loader: {
    marginTop: spacing.xl,
  },
});
