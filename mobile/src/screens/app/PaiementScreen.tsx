/**
 * Payment screen – placeholder for Mobile Money payment flow.
 * Displays payment information and a confirmation button.
 * Write operations show an explicit offline error.
 */

import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { useNetworkStatus } from '../../hooks/useNetworkStatus';
import { colors, spacing, fontSizes } from '../../config/theme';

type Props = NativeStackScreenProps<AppStackParamList, 'Paiement'>;

export function PaiementScreen({ route }: Props): React.JSX.Element {
  const { tontineId, tourId } = route.params;
  const { isOnline, assertOnline } = useNetworkStatus();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handlePaiement = useCallback(async () => {
    setError(null);
    try {
      assertOnline();
    } catch (e: unknown) {
      if (e instanceof Error) {
        setError(e.message);
      }
      return;
    }

    setIsLoading(true);
    try {
      // TODO: Implement payment API call
      // await paymentService.createVersement({ tontineId, tourId, montant });
      Alert.alert(
        'Paiement',
        'Fonctionnalité de paiement en cours de développement.',
      );
    } catch (e: unknown) {
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError('Erreur lors du paiement.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [assertOnline, tontineId, tourId]);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Paiement</Text>

      {!isOnline ? (
        <ErrorBanner message="Vous êtes hors ligne. Le paiement nécessite une connexion internet." />
      ) : null}

      {error ? (
        <ErrorBanner message={error} onDismiss={() => setError(null)} />
      ) : null}

      <View style={styles.info}>
        <Text style={styles.label}>Tontine ID</Text>
        <Text style={styles.value}>{tontineId}</Text>
      </View>

      <View style={styles.info}>
        <Text style={styles.label}>Tour ID</Text>
        <Text style={styles.value}>{tourId}</Text>
      </View>

      <PrimaryButton
        title="Confirmer le paiement"
        onPress={() => void handlePaiement()}
        loading={isLoading}
        disabled={!isOnline}
        style={styles.button}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
    padding: spacing.lg,
  },
  title: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.lg,
  },
  info: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    marginBottom: spacing.sm,
  },
  label: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
  value: {
    fontSize: fontSizes.md,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  button: {
    marginTop: spacing.xl,
  },
});
