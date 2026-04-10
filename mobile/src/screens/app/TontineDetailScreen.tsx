/**
 * Tontine detail screen – shows full tontine information.
 * Features: progress bar (% completed), member status badges (green/orange/red),
 * countdown timer to tour closing, skeleton loader, navigation to Paiement & Gestionnaire.
 */

import React, { useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  RefreshControl,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import type { TontineMember } from '../../types/api';
import { tontineService } from '../../services/tontineService';
import { useAuthStore } from '../../store/authStore';
import { ErrorBanner } from '../../components/ErrorBanner';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ProgressBar } from '../../components/ProgressBar';
import { MembreStatusBadge } from '../../components/MembreStatusBadge';
import { CountdownTimer } from '../../components/CountdownTimer';
import { TontineDetailSkeleton } from '../../components/SkeletonLoader';
import { formatMontant } from '../../utils/format';
import {
  colors,
  spacing,
  fontSizes,
  borderRadius,
} from '../../config/theme';
import { QUERY_STALE_TIME_MS, QUERY_CACHE_TIME_MS } from '../../config/constants';

type Props = NativeStackScreenProps<AppStackParamList, 'TontineDetail'>;

export function TontineDetailScreen({
  route,
  navigation,
}: Props): React.JSX.Element {
  const { tontineId } = route.params;
  const userId = useAuthStore((s) => s.user?.id);

  const {
    data: tontine,
    isLoading,
    error,
    refetch,
    isRefetching,
  } = useQuery({
    queryKey: ['tontine', tontineId],
    queryFn: () => tontineService.getTontineById(tontineId),
    staleTime: QUERY_STALE_TIME_MS,
    gcTime: QUERY_CACHE_TIME_MS,
  });

  const isGestionnaire = tontine?.gestionnaireId === userId;

  const handleNavigatePaiement = useCallback(() => {
    if (tontine?.tourActuel) {
      navigation.navigate('Paiement', {
        tontineId,
        tourId: tontine.tourActuel.id,
        montant: tontine.montantCotisation,
      });
    }
  }, [navigation, tontineId, tontine]);

  const handleNavigateGestionnaire = useCallback(() => {
    if (tontine?.tourActuel) {
      navigation.navigate('Gestionnaire', {
        tontineId,
        tourId: tontine.tourActuel.id,
      });
    }
  }, [navigation, tontineId, tontine]);

  const handleNavigateInvitation = useCallback(() => {
    if (tontine) {
      navigation.navigate('Invitation', {
        tontineId,
        tontineNom: tontine.nom,
        montant: tontine.montantCotisation,
      });
    }
  }, [navigation, tontineId, tontine]);

  const handleNavigateHistorique = useCallback(() => {
    navigation.navigate('HistoriqueVersements', { tontineId });
  }, [navigation, tontineId]);

  const renderMember = useCallback(
    (member: TontineMember) => (
      <View key={member.id} style={styles.memberRow}>
        <View style={styles.memberInfo}>
          <Text style={styles.memberName}>{member.nom}</Text>
          <Text style={styles.memberPhone}>{member.telephone}</Text>
        </View>
        {member.statutPaiement ? (
          <MembreStatusBadge statut={member.statutPaiement} />
        ) : null}
      </View>
    ),
    [],
  );

  if (isLoading) {
    return (
      <View style={styles.container}>
        <TontineDetailSkeleton />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.container}>
        <View style={styles.content}>
          <ErrorBanner message={error.message} />
        </View>
      </View>
    );
  }

  if (!tontine) {
    return (
      <View style={styles.centered}>
        <Text style={styles.emptyText}>Tontine introuvable.</Text>
      </View>
    );
  }

  const tour = tontine.tourActuel;

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
      <Text style={styles.title}>{tontine.nom}</Text>
      <Text style={styles.description}>{tontine.description}</Text>

      <View style={styles.infoRow}>
        <Text style={styles.label}>Statut</Text>
        <Text style={styles.value}>{tontine.status}</Text>
      </View>
      <View style={styles.infoRow}>
        <Text style={styles.label}>Cotisation</Text>
        <Text style={styles.value}>
          {formatMontant(tontine.montantCotisation)}
        </Text>
      </View>
      <View style={styles.infoRow}>
        <Text style={styles.label}>Fréquence</Text>
        <Text style={styles.value}>{tontine.frequence}</Text>
      </View>
      <View style={styles.infoRow}>
        <Text style={styles.label}>Gestionnaire</Text>
        <Text style={styles.value}>{tontine.gestionnaireName}</Text>
      </View>

      {tour ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Tour en cours</Text>
          <Text style={styles.tourInfo}>
            Tour #{tour.numero} – {tour.beneficiaireNom}
          </Text>

          <ProgressBar
            current={tour.nombrePaiementsRecus}
            max={tour.nombrePaiementsAttendus}
            label={`Paiements : ${tour.nombrePaiementsRecus}/${tour.nombrePaiementsAttendus}`}
          />

          {tour.dateCloture ? (
            <CountdownTimer
              targetDate={tour.dateCloture}
              label="Fin du tour"
            />
          ) : null}

          <PrimaryButton
            title="Effectuer un paiement"
            onPress={handleNavigatePaiement}
            disabled={!tour.estOuvert}
            style={styles.actionButton}
          />

          {isGestionnaire ? (
            <PrimaryButton
              title="Gérer le tour"
              onPress={handleNavigateGestionnaire}
              style={styles.gestionnaireButton}
            />
          ) : null}
        </View>
      ) : null}

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>
          Membres ({tontine.membres?.length ?? 0})
        </Text>
        {tontine.membres?.map(renderMember)}
      </View>

      {isGestionnaire && tontine.status === 'Draft' ? (
        <PrimaryButton
          title="📨 Inviter des membres"
          onPress={handleNavigateInvitation}
          style={styles.actionButton}
        />
      ) : null}

      <PrimaryButton
        title="📜 Historique de mes versements"
        onPress={handleNavigateHistorique}
        style={styles.actionButton}
      />
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
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.background,
  },
  title: {
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  description: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    marginBottom: spacing.lg,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
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
  section: {
    marginTop: spacing.lg,
  },
  sectionTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  tourInfo: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    marginBottom: spacing.sm,
  },
  actionButton: {
    marginTop: spacing.md,
  },
  gestionnaireButton: {
    marginTop: spacing.sm,
    backgroundColor: colors.secondary,
  },
  memberRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  memberInfo: {
    flex: 1,
    marginRight: spacing.sm,
  },
  memberName: {
    fontSize: fontSizes.md,
    color: colors.textPrimary,
  },
  memberPhone: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  emptyText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
});
