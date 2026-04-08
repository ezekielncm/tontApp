/**
 * Profile screen – displays user info and provides logout.
 */

import React, { useCallback } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import { useAuthStore } from '../../store/authStore';
import { useAuth } from '../../hooks/useAuth';
import { PrimaryButton } from '../../components/PrimaryButton';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';

export function ProfilScreen(): React.JSX.Element {
  const user = useAuthStore((s) => s.user);
  const { logout, isLoading } = useAuth();

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
    <View style={styles.container}>
      <View style={styles.card}>
        <View style={styles.avatar}>
          <Text style={styles.avatarText}>
            {user?.nom ? user.nom.charAt(0).toUpperCase() : '?'}
          </Text>
        </View>
        <Text style={styles.name}>{user?.nom || 'Utilisateur'}</Text>
        <Text style={styles.phone}>{user?.telephone || ''}</Text>
      </View>

      <PrimaryButton
        title="Se déconnecter"
        onPress={handleLogout}
        loading={isLoading}
        style={styles.logoutButton}
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
  card: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.xl,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.xl,
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
  logoutButton: {
    backgroundColor: colors.error,
  },
});
