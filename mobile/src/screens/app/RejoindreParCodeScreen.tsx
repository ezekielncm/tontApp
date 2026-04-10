/**
 * Rejoindre par Code Screen.
 * Allows a user to join a tontine by entering an invitation code.
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { tontineService } from '../../services/tontineService';
import { useAuthStore } from '../../store/authStore';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { FormInput } from '../../components/FormInput';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';

type Props = NativeStackScreenProps<AppStackParamList, 'RejoindreParCode'>;

export function RejoindreParCodeScreen({
  navigation,
}: Props): React.JSX.Element {
  const [code, setCode] = useState('');
  const [memberName, setMemberName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const user = useAuthStore((s) => s.user);

  // Pre-fill member name from user profile
  React.useEffect(() => {
    if (user?.nom) {
      setMemberName(user.nom);
    }
  }, [user]);

  const handleJoin = useCallback(async () => {
    const trimmedCode = code.trim();
    const trimmedName = memberName.trim();

    if (!trimmedCode) {
      setError("Veuillez entrer le code d'invitation.");
      return;
    }
    if (!trimmedName || trimmedName.length < 2) {
      setError('Veuillez entrer un nom valide (minimum 2 caractères).');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await tontineService.rejoindreParCode({
        code: trimmedCode,
        memberName: trimmedName,
      });

      Alert.alert(
        'Bienvenue ! 🎉',
        'Vous avez rejoint la tontine avec succès.',
        [
          {
            text: 'OK',
            onPress: () => navigation.navigate('Home'),
          },
        ],
      );
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError(
          "Code invalide, expiré ou déjà utilisé. Demandez un nouveau code d'invitation.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [code, memberName, navigation]);

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.header}>
          <Text style={styles.emoji}>🤝</Text>
          <Text style={styles.title}>Rejoindre une tontine</Text>
          <Text style={styles.subtitle}>
            Entrez le code d'invitation que vous avez reçu pour rejoindre un
            groupe de tontine.
          </Text>
        </View>

        {error && <ErrorBanner message={error} />}

        <View style={styles.codeInputContainer}>
          <Text style={styles.label}>Code d'invitation</Text>
          <TextInput
            style={styles.codeInput}
            value={code}
            onChangeText={(text) => {
              setCode(text.toUpperCase());
              setError(null);
            }}
            placeholder="Ex: ABC123"
            placeholderTextColor={colors.textSecondary}
            autoCapitalize="characters"
            autoCorrect={false}
            maxLength={20}
          />
        </View>

        <FormInput
          label="Votre nom dans le groupe"
          value={memberName}
          onChangeText={(text) => {
            setMemberName(text);
            setError(null);
          }}
          placeholder="Entrez votre nom"
          autoCapitalize="words"
        />

        <View style={styles.infoCard}>
          <Text style={styles.infoIcon}>ℹ️</Text>
          <Text style={styles.infoText}>
            Vous ne pouvez rejoindre une tontine que si elle est encore en phase
            de recrutement (brouillon). Une fois activée, il n'est plus possible
            d'ajouter des membres.
          </Text>
        </View>

        <PrimaryButton
          title="Rejoindre"
          onPress={handleJoin}
          loading={isLoading}
          disabled={!code.trim() || !memberName.trim()}
        />
      </ScrollView>
    </KeyboardAvoidingView>
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
  label: {
    fontSize: fontSizes.sm,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  codeInputContainer: {
    marginBottom: spacing.lg,
  },
  codeInput: {
    borderWidth: 2,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    textAlign: 'center',
    letterSpacing: 4,
    backgroundColor: colors.surface,
  },
  infoCard: {
    flexDirection: 'row',
    backgroundColor: colors.primaryLight,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.xl,
    marginTop: spacing.md,
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
});
