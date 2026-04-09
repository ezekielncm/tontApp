/**
 * Profile screen – displays user info, credit score badge, and provides logout.
 */

import React, { useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Alert,
  ActivityIndicator,
  ScrollView,
} from 'react-native';
import { useAuthStore } from '../../store/authStore';
import { useAuth } from '../../hooks/useAuth';
import { useProfilCredit } from '../../hooks/useProfilCredit';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ScoreBadge } from '../../components/ScoreBadge';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';

export function ProfilScreen(): React.JSX.Element {
  const user = useAuthStore((s) => s.user);
  const { logout, isLoading: isLoggingOut } = useAuth();
  const { data: profilCredit, isLoading: isLoadingCredit } = useProfilCredit(
    user?.id,
  );

  const handleLogout = useCallback(() => {
    Alert.alert(
      'Déconnexion',
      'Voulez-vous vraiment vous déconnecter ?',
      [
        { text: 'Annuler', style: 'cancel' },
        {
          text: 'Déconnecter',
          style: 'destructive',
          onPress: () => void logout(),
        },
      ],
    );
  }, [logout]);

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.contentContainer}
    >
      {/* User info card */}
      <View style={styles.card}>
        <View style={styles.avatar}>
          <Text style={styles.avatarText}>
            {user?.nom ? user.nom.charAt(0).toUpperCase() : '?'}
          </Text>
        </View>
        <Text style={styles.name}>{user?.nom || 'Utilisateur'}</Text>
        <Text style={styles.phone}>{user?.telephone || ''}</Text>
      </View>

      {/* Credit score section */}
      <View style={styles.creditSection}>
        <Text style={styles.sectionTitle}>Score Crédit</Text>

        {isLoadingCredit ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={colors.primary} />
            <Text style={styles.loadingText}>Chargement du score…</Text>
          </View>
        ) : profilCredit == null ? (
          <View style={styles.emptyCard}>
            <Text style={styles.emptyText}>
              Aucun profil crédit disponible. Effectuez des versements pour
              construire votre score.
            </Text>
          </View>
        ) : profilCredit.donneesInsuffisantes ? (
          <View style={styles.insufficientCard}>
            <Text style={styles.insufficientTitle}>
              Données insuffisantes
            </Text>
            <Text style={styles.insufficientText}>
              Complétez au moins 1 cycle de tontine pour obtenir votre score
              crédit.
            </Text>
          </View>
        ) : (
          <>
            <ScoreBadge
              score={profilCredit.score}
              niveau={profilCredit.niveau}
            />

            {/* Score components breakdown */}
            <View style={styles.composantesCard}>
              <Text style={styles.composantesTitle}>
                Détail des composantes
              </Text>

              <View style={styles.composanteRow}>
                <Text style={styles.composanteLabel}>Cycles complétés</Text>
                <Text style={styles.composanteValue}>
                  {profilCredit.composantes.cyclesCompletes} (
                  {profilCredit.composantes.contributionCycles} pts)
                </Text>
              </View>

              <View style={styles.composanteSeparator} />

              <View style={styles.composanteRow}>
                <Text style={styles.composanteLabel}>
                  Taux de ponctualité
                </Text>
                <Text style={styles.composanteValue}>
                  {Math.round(profilCredit.composantes.tauxPonctualite * 100)}%
                  ({profilCredit.composantes.contributionPonctualite} pts)
                </Text>
              </View>

              <View style={styles.composanteSeparator} />

              <View style={styles.composanteRow}>
                <Text style={styles.composanteLabel}>Ancienneté</Text>
                <Text style={styles.composanteValue}>
                  {profilCredit.composantes.ancienneteEnMois} mois (
                  {profilCredit.composantes.contributionAnciennete} pts)
                </Text>
              </View>
            </View>
          </>
        )}
      </View>

      <PrimaryButton
        title="Se déconnecter"
        onPress={handleLogout}
        loading={isLoggingOut}
        style={styles.logoutButton}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  contentContainer: {
    padding: spacing.lg,
  },
  card: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.xl,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.lg,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  avatarText: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.textOnPrimary,
  },
  name: {
    fontSize: fontSizes.xl,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  phone: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
  creditSection: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  loadingContainer: {
    alignItems: 'center',
    padding: spacing.xl,
  },
  loadingText: {
    marginTop: spacing.sm,
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  emptyCard: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
  },
  emptyText: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    textAlign: 'center',
  },
  insufficientCard: {
    backgroundColor: '#FFF8E1',
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: '#FFD54F',
  },
  insufficientTitle: {
    fontSize: fontSizes.md,
    fontWeight: '600',
    color: '#F57F17',
    marginBottom: spacing.xs,
  },
  insufficientText: {
    fontSize: fontSizes.sm,
    color: '#795548',
  },
  composantesCard: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
    marginTop: spacing.md,
  },
  composantesTitle: {
    fontSize: fontSizes.md,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  composanteRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.sm,
  },
  composanteLabel: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  composanteValue: {
    fontSize: fontSizes.sm,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  composanteSeparator: {
    height: 1,
    backgroundColor: colors.border,
  },
  logoutButton: {
    backgroundColor: colors.error,
  },
});
