/**
 * Payment screen – Mobile Money (Orange Money) payment flow.
 * Features:
 * - Amount display with FCFA formatting
 * - "Payer via Orange Money" button with immediate visual feedback
 * - Polling every 5s for 2 minutes to check payment status
 * - Success/failure feedback with clear visual states
 * - Offline error handling
 */

import React, { useState, useCallback, useRef, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ActivityIndicator,
  ScrollView,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { paymentService } from '../../services/paymentService';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { ProgressBar } from '../../components/ProgressBar';
import { useNetworkStatus } from '../../hooks/useNetworkStatus';
import { formatMontant } from '../../utils/format';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';
import {
  PAYMENT_POLL_INTERVAL_MS,
  PAYMENT_MAX_POLL_DURATION_MS,
} from '../../config/constants';

type Props = NativeStackScreenProps<AppStackParamList, 'Paiement'>;

type PaymentPhase = 'idle' | 'initiating' | 'polling' | 'success' | 'failure' | 'timeout';

export function PaiementScreen({ route, navigation }: Props): React.JSX.Element {
  const { tontineId, tourId, montant } = route.params;
  const { isOnline, assertOnline } = useNetworkStatus();

  const [phase, setPhase] = useState<PaymentPhase>('idle');
  const [error, setError] = useState<string | null>(null);
  const [pollProgress, setPollProgress] = useState(0);
  const [versementId, setVersementId] = useState<string | null>(null);

  const pollTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const pollStartRef = useRef<number>(0);
  const isPollingRef = useRef(false);

  // Cleanup polling on unmount
  useEffect(() => {
    return () => {
      if (pollTimerRef.current) {
        clearInterval(pollTimerRef.current);
      }
    };
  }, []);

  const stopPolling = useCallback(() => {
    if (pollTimerRef.current) {
      clearInterval(pollTimerRef.current);
      pollTimerRef.current = null;
    }
    isPollingRef.current = false;
  }, []);

  const startPolling = useCallback(
    (vId: string) => {
      pollStartRef.current = Date.now();
      setPollProgress(0);

      pollTimerRef.current = setInterval(async () => {
        // Guard against concurrent poll requests
        if (isPollingRef.current) return;
        isPollingRef.current = true;

        const elapsed = Date.now() - pollStartRef.current;
        const progress = Math.min(elapsed / PAYMENT_MAX_POLL_DURATION_MS, 1);
        setPollProgress(progress);

        if (elapsed >= PAYMENT_MAX_POLL_DURATION_MS) {
          stopPolling();
          setPhase('timeout');
          return;
        }

        try {
          const status = await paymentService.getVersementStatus(vId);
          if (status.statut === 'confirme') {
            stopPolling();
            setPhase('success');
          } else if (status.statut === 'rejete') {
            stopPolling();
            setPhase('failure');
            setError('Le paiement a été rejeté. Veuillez réessayer.');
          }
          // 'en_attente' → keep polling
        } catch {
          // Transient error during poll — keep polling
        } finally {
          isPollingRef.current = false;
        }
      }, PAYMENT_POLL_INTERVAL_MS);
    },
    [stopPolling],
  );

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

    setPhase('initiating');
    try {
      const response = await paymentService.initierVersement({
        tontineId,
        tourId,
        montant,
      });
      setVersementId(response.versementId);
      setPhase('polling');
      startPolling(response.versementId);
    } catch (e: unknown) {
      setPhase('failure');
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError('Erreur lors de l\'initiation du paiement.');
      }
    }
  }, [assertOnline, tontineId, tourId, montant, startPolling]);

  const handleRetry = useCallback(() => {
    setPhase('idle');
    setError(null);
    setPollProgress(0);
    setVersementId(null);
  }, []);

  const handleGoBack = useCallback(() => {
    navigation.goBack();
  }, [navigation]);

  const isActionDisabled = !isOnline || phase === 'initiating' || phase === 'polling';

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Amount display */}
      <View style={styles.amountCard}>
        <Text style={styles.amountLabel}>Montant à payer</Text>
        <Text style={styles.amount}>{formatMontant(montant)}</Text>
      </View>

      {/* Offline warning */}
      {!isOnline ? (
        <ErrorBanner message="Vous êtes hors ligne. Le paiement nécessite une connexion internet." />
      ) : null}

      {/* Error banner */}
      {error ? (
        <ErrorBanner message={error} onDismiss={() => setError(null)} />
      ) : null}

      {/* Phase-specific content */}
      {phase === 'idle' ? (
        <View style={styles.section}>
          <Text style={styles.infoText}>
            Vous allez effectuer un paiement via Orange Money.
            Assurez-vous d&apos;avoir un solde suffisant.
          </Text>
          <PrimaryButton
            title="🟠 Payer via Orange Money"
            onPress={() => void handlePaiement()}
            disabled={isActionDisabled}
            style={styles.orangeButton}
          />
        </View>
      ) : null}

      {phase === 'initiating' ? (
        <View style={styles.statusContainer}>
          <ActivityIndicator size="large" color={colors.secondary} />
          <Text style={styles.statusText}>Initiation du paiement...</Text>
        </View>
      ) : null}

      {phase === 'polling' ? (
        <View style={styles.statusContainer}>
          <ActivityIndicator size="large" color={colors.secondary} />
          <Text style={styles.statusText}>
            En attente de confirmation Orange Money...
          </Text>
          <Text style={styles.statusSubtext}>
            Veuillez confirmer le paiement sur votre téléphone.
          </Text>
          <ProgressBar
            current={Math.round(pollProgress * 100)}
            max={100}
            label="Vérification en cours"
            height={8}
          />
        </View>
      ) : null}

      {phase === 'success' ? (
        <View style={styles.statusContainer}>
          <View style={styles.successIcon}>
            <Text style={styles.successEmoji}>✅</Text>
          </View>
          <Text style={styles.successTitle}>Paiement confirmé !</Text>
          <Text style={styles.successText}>
            Votre versement de {formatMontant(montant)} a été confirmé.
          </Text>
          <PrimaryButton
            title="Retour à la tontine"
            onPress={handleGoBack}
            style={styles.returnButton}
          />
        </View>
      ) : null}

      {phase === 'failure' ? (
        <View style={styles.statusContainer}>
          <View style={styles.failureIcon}>
            <Text style={styles.failureEmoji}>❌</Text>
          </View>
          <Text style={styles.failureTitle}>Paiement échoué</Text>
          <PrimaryButton
            title="Réessayer"
            onPress={handleRetry}
            style={styles.retryButton}
          />
          <PrimaryButton
            title="Retour"
            onPress={handleGoBack}
            style={styles.returnButtonSecondary}
          />
        </View>
      ) : null}

      {phase === 'timeout' ? (
        <View style={styles.statusContainer}>
          <View style={styles.timeoutIcon}>
            <Text style={styles.timeoutEmoji}>⏰</Text>
          </View>
          <Text style={styles.timeoutTitle}>Délai d&apos;attente dépassé</Text>
          <Text style={styles.timeoutText}>
            La confirmation n&apos;a pas été reçue dans les 2 minutes.
            Votre paiement est peut-être encore en cours de traitement.
            Veuillez vérifier votre solde Orange Money ou contactez le support
            si le montant a été débité.
          </Text>
          <PrimaryButton
            title="Réessayer"
            onPress={handleRetry}
            style={styles.retryButton}
          />
          <PrimaryButton
            title="Retour"
            onPress={handleGoBack}
            style={styles.returnButtonSecondary}
          />
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  content: {
    padding: spacing.lg,
    paddingBottom: spacing.xxl,
  },
  amountCard: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.xl,
    alignItems: 'center',
    marginBottom: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
  },
  amountLabel: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  amount: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.textPrimary,
  },
  section: {
    marginTop: spacing.md,
  },
  infoText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    marginBottom: spacing.lg,
    lineHeight: 24,
  },
  orangeButton: {
    backgroundColor: '#E65100',
  },
  statusContainer: {
    alignItems: 'center',
    paddingVertical: spacing.xl,
  },
  statusText: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    marginTop: spacing.md,
    textAlign: 'center',
  },
  statusSubtext: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    marginTop: spacing.sm,
    textAlign: 'center',
    marginBottom: spacing.lg,
  },
  successIcon: {
    marginBottom: spacing.md,
  },
  successEmoji: {
    fontSize: 64,
  },
  successTitle: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.primary,
    marginBottom: spacing.sm,
  },
  successText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
    marginBottom: spacing.lg,
  },
  failureIcon: {
    marginBottom: spacing.md,
  },
  failureEmoji: {
    fontSize: 64,
  },
  failureTitle: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.error,
    marginBottom: spacing.lg,
  },
  timeoutIcon: {
    marginBottom: spacing.md,
  },
  timeoutEmoji: {
    fontSize: 64,
  },
  timeoutTitle: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.secondary,
    marginBottom: spacing.sm,
  },
  timeoutText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
    marginBottom: spacing.lg,
    lineHeight: 22,
  },
  retryButton: {
    width: '100%',
    marginBottom: spacing.sm,
  },
  returnButton: {
    width: '100%',
  },
  returnButtonSecondary: {
    width: '100%',
    backgroundColor: colors.textSecondary,
  },
});
