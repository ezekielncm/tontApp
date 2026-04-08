/**
 * Tontine detail screen – shows full tontine information.
 * Uses React Query for cache-first offline reading.
 */

import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  TouchableOpacity,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { tontineService } from '../../services/tontineService';
import { ErrorBanner } from '../../components/ErrorBanner';
import { PrimaryButton } from '../../components/PrimaryButton';
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

  const { data: tontine, isLoading, error } = useQuery({
    queryKey: ['tontine', tontineId],
    queryFn: () => tontineService.getTontineById(tontineId),
    staleTime: QUERY_STALE_TIME_MS,
    gcTime: QUERY_CACHE_TIME_MS,
  });

  if (isLoading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.container}>
        <ErrorBanner message={error.message} />
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

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
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
          {tontine.montantCotisation.toLocaleString('fr-FR')}{' '}
          {tontine.devise}
        </Text>
      </View>
      <View style={styles.infoRow}>
        <Text style={styles.label}>Fréquence</Text>
        <Text style={styles.value}>{tontine.frequence}</Text>
      </View>
      <View style={styles.infoRow}>
        <Text style={styles.label}>Membres</Text>
        <Text style={styles.value}>{tontine.nombreMembres}</Text>
      </View>

      {tontine.tourActuel ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Tour en cours</Text>
          <Text style={styles.tourInfo}>
            Tour #{tontine.tourActuel.numero} –{' '}
            {tontine.tourActuel.beneficiaireNom}
          </Text>
          <PrimaryButton
            title="Effectuer un paiement"
            onPress={() =>
              navigation.navigate('Paiement', {
                tontineId,
                tourId: tontine.tourActuel?.id ?? '',
              })
            }
            disabled={!tontine.tourActuel.estOuvert}
          />
        </View>
      ) : null}

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>
          Membres ({tontine.membres.length})
        </Text>
        {tontine.membres.map((m) => (
          <TouchableOpacity key={m.id} style={styles.memberRow}>
            <Text style={styles.memberName}>{m.nom}</Text>
            <Text style={styles.memberPhone}>{m.telephone}</Text>
          </TouchableOpacity>
        ))}
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
    marginBottom: spacing.md,
  },
  memberRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
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
