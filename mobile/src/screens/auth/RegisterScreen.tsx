/**
 * Register screen with Zod validation, loading states, and error handling.
 *
 * Validates:
 * - Name: 2-100 characters
 * - Phone number: E.164 format (+226XXXXXXXX for Burkina Faso)
 * - Password: minimum 8 characters, at least one uppercase, one digit
 * - Password confirmation: must match
 *
 * Connected to the API via useAuth() hook.
 */

import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  TouchableOpacity,
} from 'react-native';
import { z } from 'zod';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AuthStackParamList } from '../../navigation/types';
import { useAuth } from '../../hooks/useAuth';
import { FormInput } from '../../components/FormInput';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { colors, spacing, fontSizes } from '../../config/theme';

type Props = NativeStackScreenProps<AuthStackParamList, 'Register'>;

// ─── Zod Schema ───────────────────────────────────────────────────────────────

const registerSchema = z
  .object({
    nom: z
      .string()
      .min(2, 'Le nom doit contenir au moins 2 caractères.')
      .max(100, 'Le nom ne peut pas dépasser 100 caractères.'),
    telephone: z
      .string()
      .min(1, 'Le numéro de téléphone est requis.')
      .regex(
        /^\+\d{10,15}$/,
        'Format invalide. Utilisez le format international (ex: +22670000000).',
      ),
    motDePasse: z
      .string()
      .min(8, 'Le mot de passe doit contenir au moins 8 caractères.')
      .regex(/[A-Z]/, 'Le mot de passe doit contenir au moins une majuscule.')
      .regex(/\d/, 'Le mot de passe doit contenir au moins un chiffre.'),
    confirmMotDePasse: z.string(),
  })
  .refine((data) => data.motDePasse === data.confirmMotDePasse, {
    message: 'Les mots de passe ne correspondent pas.',
    path: ['confirmMotDePasse'],
  });

type RegisterFormData = z.infer<typeof registerSchema>;
type FieldErrors = Partial<Record<keyof RegisterFormData, string>>;

// ─── Component ────────────────────────────────────────────────────────────────

export function RegisterScreen({ navigation }: Props): React.JSX.Element {
  const { register, isLoading, error, clearError } = useAuth();

  const [nom, setNom] = useState('');
  const [telephone, setTelephone] = useState('+226');
  const [motDePasse, setMotDePasse] = useState('');
  const [confirmMotDePasse, setConfirmMotDePasse] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const validate = useCallback((): RegisterFormData | null => {
    const result = registerSchema.safeParse({
      nom,
      telephone,
      motDePasse,
      confirmMotDePasse,
    });
    if (result.success) {
      setFieldErrors({});
      return result.data;
    }
    const errors: FieldErrors = {};
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof RegisterFormData | undefined;
      if (field && !errors[field]) {
        errors[field] = issue.message;
      }
    }
    setFieldErrors(errors);
    return null;
  }, [nom, telephone, motDePasse, confirmMotDePasse]);

  const handleRegister = useCallback(async () => {
    const data = validate();
    if (!data) return;
    await register(data.telephone, data.nom, data.motDePasse);
  }, [validate, register]);

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView
        contentContainerStyle={styles.scroll}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.header}>
          <Text style={styles.title}>Créer un compte</Text>
          <Text style={styles.subtitle}>
            Rejoignez TontinesApp
          </Text>
        </View>

        {error ? (
          <ErrorBanner message={error} onDismiss={clearError} />
        ) : null}

        <FormInput
          label="Nom complet"
          value={nom}
          onChangeText={setNom}
          error={fieldErrors.nom}
          textContentType="name"
          placeholder="Votre nom complet"
          autoCapitalize="words"
          editable={!isLoading}
        />

        <FormInput
          label="Numéro de téléphone"
          value={telephone}
          onChangeText={setTelephone}
          error={fieldErrors.telephone}
          keyboardType="phone-pad"
          textContentType="telephoneNumber"
          placeholder="+22670000000"
          editable={!isLoading}
        />

        <FormInput
          label="Mot de passe"
          value={motDePasse}
          onChangeText={setMotDePasse}
          error={fieldErrors.motDePasse}
          secureTextEntry
          textContentType="newPassword"
          placeholder="Min. 8 caractères, 1 majuscule, 1 chiffre"
          editable={!isLoading}
        />

        <FormInput
          label="Confirmer le mot de passe"
          value={confirmMotDePasse}
          onChangeText={setConfirmMotDePasse}
          error={fieldErrors.confirmMotDePasse}
          secureTextEntry
          textContentType="newPassword"
          placeholder="Retapez votre mot de passe"
          editable={!isLoading}
        />

        <PrimaryButton
          title="Créer mon compte"
          onPress={() => void handleRegister()}
          loading={isLoading}
        />

        <View style={styles.footer}>
          <Text style={styles.footerText}>Déjà un compte ?</Text>
          <TouchableOpacity
            onPress={() => navigation.navigate('Login')}
            disabled={isLoading}
          >
            <Text style={styles.link}> Se connecter</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

// ─── Styles ───────────────────────────────────────────────────────────────────

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  scroll: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: spacing.lg,
  },
  header: {
    alignItems: 'center',
    marginBottom: spacing.xl,
  },
  title: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.primary,
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: spacing.lg,
  },
  footerText: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  link: {
    fontSize: fontSizes.sm,
    color: colors.primary,
    fontWeight: '600',
  },
});
