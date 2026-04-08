/**
 * Login screen with Zod validation, loading states, and error handling.
 *
 * Validates:
 * - Phone number: E.164 format (+226XXXXXXXX for Burkina Faso)
 * - Password: minimum 8 characters
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

type Props = NativeStackScreenProps<AuthStackParamList, 'Login'>;

// ─── Zod Schema ───────────────────────────────────────────────────────────────

const loginSchema = z.object({
  telephone: z
    .string()
    .min(1, 'Le numéro de téléphone est requis.')
    .regex(
      /^\+\d{1,15}$/,
      'Format invalide. Utilisez le format international (ex: +22670000000).',
    ),
  motDePasse: z
    .string()
    .min(8, 'Le mot de passe doit contenir au moins 8 caractères.'),
});

type LoginFormData = z.infer<typeof loginSchema>;
type FieldErrors = Partial<Record<keyof LoginFormData, string>>;

// ─── Component ────────────────────────────────────────────────────────────────

export function LoginScreen({ navigation }: Props): React.JSX.Element {
  const { login, isLoading, error, clearError } = useAuth();

  const [telephone, setTelephone] = useState('+226');
  const [motDePasse, setMotDePasse] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const validate = useCallback((): LoginFormData | null => {
    const result = loginSchema.safeParse({ telephone, motDePasse });
    if (result.success) {
      setFieldErrors({});
      return result.data;
    }
    const errors: FieldErrors = {};
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof LoginFormData | undefined;
      if (field && !errors[field]) {
        errors[field] = issue.message;
      }
    }
    setFieldErrors(errors);
    return null;
  }, [telephone, motDePasse]);

  const handleLogin = useCallback(async () => {
    const data = validate();
    if (!data) return;
    await login(data.telephone, data.motDePasse);
  }, [validate, login]);

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
          <Text style={styles.title}>TontinesApp</Text>
          <Text style={styles.subtitle}>
            Connectez-vous à votre compte
          </Text>
        </View>

        {error ? (
          <ErrorBanner message={error} onDismiss={clearError} />
        ) : null}

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
          textContentType="password"
          placeholder="Votre mot de passe"
          editable={!isLoading}
        />

        <PrimaryButton
          title="Se connecter"
          onPress={() => void handleLogin()}
          loading={isLoading}
        />

        <View style={styles.footer}>
          <Text style={styles.footerText}>Pas encore de compte ?</Text>
          <TouchableOpacity
            onPress={() => navigation.navigate('Register')}
            disabled={isLoading}
          >
            <Text style={styles.link}> Créer un compte</Text>
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
