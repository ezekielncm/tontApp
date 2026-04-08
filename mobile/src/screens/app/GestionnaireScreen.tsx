/**
 * Gestionnaire screen – Admin actions for the current tour.
 * Features:
 * - List of late members (retardataires) with days late count
 * - "Relancer SMS" button to send reminders to selected late members
 * - "Clore le tour" button with confirmation dialog
 * - Immediate visual feedback on all interactions
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import type { MembreRetardataire } from '../../types/api';
import { tontineService } from '../../services/tontineService';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { useNetworkStatus } from '../../hooks/useNetworkStatus';
import {
  colors,
  spacing,
  fontSizes,
  borderRadius,
} from '../../config/theme';
import { QUERY_STALE_TIME_MS, QUERY_CACHE_TIME_MS } from '../../config/constants';

type Props = NativeStackScreenProps<AppStackParamList, 'Gestionnaire'>;

export function GestionnaireScreen({
  route,
  navigation,
}: Props): React.JSX.Element {
  const { tontineId, tourId } = route.params;
  const { isOnline, assertOnline } = useNetworkStatus();
  const queryClient = useQueryClient();

  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [isSendingSms, setIsSendingSms] = useState(false);
  const [isClosingTour, setIsClosingTour] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    data: retardataires,
    isLoading,
    error: fetchError,
    refetch,
    isRefetching,
  } = useQuery<MembreRetardataire[], Error>({
    queryKey: ['retardataires', tontineId, tourId],
    queryFn: () => tontineService.getRetardataires(tontineId, tourId),
    staleTime: QUERY_STALE_TIME_MS,
    gcTime: QUERY_CACHE_TIME_MS,
  });

  const toggleSelection = useCallback((membreId: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(membreId)) {
        next.delete(membreId);
      } else {
        next.add(membreId);
      }
      return next;
    });
  }, []);

  const selectAll = useCallback(() => {
    if (retardataires) {
      setSelectedIds(new Set(retardataires.map((r) => r.membreId)));
    }
  }, [retardataires]);

  const deselectAll = useCallback(() => {
    setSelectedIds(new Set());
  }, []);

  const handleRelancerSms = useCallback(async () => {
    setError(null);
    setSuccessMessage(null);

    try {
      assertOnline();
    } catch (e: unknown) {
      if (e instanceof Error) setError(e.message);
      return;
    }

    if (selectedIds.size === 0) {
      setError('Veuillez sélectionner au moins un membre.');
      return;
    }

    setIsSendingSms(true);
    try {
      await tontineService.relancerSms(tontineId, tourId, {
        membreIds: Array.from(selectedIds),
      });
      setSuccessMessage(
        `SMS de relance envoyé à ${selectedIds.size} membre${selectedIds.size > 1 ? 's' : ''}.`,
      );
      setSelectedIds(new Set());
    } catch (e: unknown) {
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError('Erreur lors de l\'envoi des SMS.');
      }
    } finally {
      setIsSendingSms(false);
    }
  }, [assertOnline, selectedIds, tontineId, tourId]);

  const handleCloreTour = useCallback(() => {
    Alert.alert(
      'Clore le tour',
      'Êtes-vous sûr de vouloir clore ce tour ? Cette action est irréversible.',
      [
        { text: 'Annuler', style: 'cancel' },
        {
          text: 'Clore le tour',
          style: 'destructive',
          onPress: async () => {
            setError(null);
            setSuccessMessage(null);

            try {
              assertOnline();
            } catch (e: unknown) {
              if (e instanceof Error) setError(e.message);
              return;
            }

            setIsClosingTour(true);
            try {
              await tontineService.cloreTour(tontineId, tourId);
              await queryClient.invalidateQueries({
                queryKey: ['tontine', tontineId],
              });
              await queryClient.invalidateQueries({
                queryKey: ['retardataires', tontineId, tourId],
              });
              Alert.alert(
                'Tour clôturé',
                'Le tour a été clôturé avec succès.',
                [
                  {
                    text: 'OK',
                    onPress: () => navigation.goBack(),
                  },
                ],
              );
            } catch (e: unknown) {
              if (e instanceof Error) {
                setError(e.message);
              } else {
                setError('Erreur lors de la clôture du tour.');
              }
            } finally {
              setIsClosingTour(false);
            }
          },
        },
      ],
    );
  }, [assertOnline, tontineId, tourId, queryClient, navigation]);

  const renderRetardataire = useCallback(
    (membre: MembreRetardataire) => {
      const isSelected = selectedIds.has(membre.membreId);
      return (
        <TouchableOpacity
          key={membre.membreId}
          style={[styles.membreRow, isSelected ? styles.membreRowSelected : undefined]}
          onPress={() => toggleSelection(membre.membreId)}
          activeOpacity={0.7}
          accessibilityRole="checkbox"
          accessibilityState={{ checked: isSelected }}
          accessibilityLabel={`${membre.nom}, ${membre.joursRetard} jours de retard`}
        >
          <View style={[styles.checkbox, isSelected ? styles.checkboxChecked : undefined]}>
            {isSelected ? <Text style={styles.checkmark}>✓</Text> : null}
          </View>
          <View style={styles.membreInfo}>
            <Text style={styles.membreNom}>{membre.nom}</Text>
            <Text style={styles.membreTelephone}>{membre.telephone}</Text>
          </View>
          <View style={styles.retardBadge}>
            <Text style={styles.retardText}>
              {membre.joursRetard}j retard
            </Text>
          </View>
        </TouchableOpacity>
      );
    },
    [selectedIds, toggleSelection],
  );

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={isRefetching}
          onRefresh={() => void refetch()}
          colors={[colors.primary]}
        />
      }
    >
      <Text style={styles.title}>Gestion du tour</Text>

      {!isOnline ? (
        <ErrorBanner message="Vous êtes hors ligne. Les actions nécessitent une connexion internet." />
      ) : null}

      {error || fetchError ? (
        <ErrorBanner
          message={error ?? fetchError?.message ?? 'Erreur inconnue'}
          onDismiss={() => setError(null)}
        />
      ) : null}

      {successMessage ? (
        <View style={styles.successBanner}>
          <Text style={styles.successText}>{successMessage}</Text>
        </View>
      ) : null}

      {/* Retardataires section */}
      <View style={styles.section}>
        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>
            Retardataires ({retardataires?.length ?? 0})
          </Text>
          {retardataires && retardataires.length > 0 ? (
            <TouchableOpacity
              onPress={
                selectedIds.size === retardataires.length
                  ? deselectAll
                  : selectAll
              }
              accessibilityRole="button"
            >
              <Text style={styles.selectAllText}>
                {selectedIds.size === retardataires.length
                  ? 'Tout désélectionner'
                  : 'Tout sélectionner'}
              </Text>
            </TouchableOpacity>
          ) : null}
        </View>

        {isLoading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={colors.primary} />
          </View>
        ) : retardataires && retardataires.length > 0 ? (
          retardataires.map(renderRetardataire)
        ) : (
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              🎉 Aucun retardataire ! Tous les membres sont à jour.
            </Text>
          </View>
        )}
      </View>

      {/* Actions */}
      <View style={styles.actions}>
        <PrimaryButton
          title={
            isSendingSms
              ? 'Envoi en cours...'
              : `📩 Relancer par SMS (${selectedIds.size})`
          }
          onPress={() => void handleRelancerSms()}
          loading={isSendingSms}
          disabled={
            !isOnline ||
            isSendingSms ||
            isClosingTour ||
            selectedIds.size === 0
          }
          style={styles.smsButton}
        />

        <PrimaryButton
          title={isClosingTour ? 'Clôture en cours...' : '🔒 Clore le tour'}
          onPress={handleCloreTour}
          loading={isClosingTour}
          disabled={!isOnline || isSendingSms || isClosingTour}
          style={styles.cloreButton}
        />
      </View>
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
  title: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.lg,
  },
  section: {
    marginBottom: spacing.lg,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  sectionTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  selectAllText: {
    fontSize: fontSizes.sm,
    color: colors.primary,
    fontWeight: '600',
  },
  membreRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
    borderWidth: 1,
    borderColor: colors.border,
  },
  membreRowSelected: {
    borderColor: colors.primary,
    backgroundColor: '#E8F5E9',
  },
  checkbox: {
    width: 24,
    height: 24,
    borderRadius: borderRadius.sm,
    borderWidth: 2,
    borderColor: colors.border,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: spacing.md,
  },
  checkboxChecked: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  checkmark: {
    color: colors.textOnPrimary,
    fontSize: fontSizes.sm,
    fontWeight: '700',
  },
  membreInfo: {
    flex: 1,
  },
  membreNom: {
    fontSize: fontSizes.md,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  membreTelephone: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  retardBadge: {
    backgroundColor: '#FFEBEE',
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: borderRadius.sm,
  },
  retardText: {
    fontSize: fontSizes.xs,
    fontWeight: '700',
    color: colors.error,
  },
  loadingContainer: {
    paddingVertical: spacing.xl,
    alignItems: 'center',
  },
  emptyContainer: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.lg,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  emptyText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
  },
  successBanner: {
    backgroundColor: '#E8F5E9',
    borderWidth: 1,
    borderColor: colors.primary,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  successText: {
    color: colors.primary,
    fontSize: fontSizes.sm,
    fontWeight: '600',
  },
  actions: {
    marginTop: spacing.md,
  },
  smsButton: {
    marginBottom: spacing.md,
    backgroundColor: colors.secondary,
  },
  cloreButton: {
    backgroundColor: colors.error,
  },
});
