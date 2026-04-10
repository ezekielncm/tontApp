/**
 * OTP Verification Screen.
 * After registration, the user enters the 6-digit OTP code
 * sent via SMS to validate their phone number and obtain JWT tokens.
 */

import React, { useState, useRef, useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  Alert,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AuthStackParamList } from '../../navigation/types';
import { useAuth } from '../../hooks/useAuth';
import { PrimaryButton } from '../../components/PrimaryButton';
import { ErrorBanner } from '../../components/ErrorBanner';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';

type Props = NativeStackScreenProps<AuthStackParamList, 'VerifierOtp'>;

const OTP_LENGTH = 6;

export function VerifierOtpScreen({ route }: Props): React.JSX.Element {
  const { telephone, nom } = route.params;
  const [code, setCode] = useState<string[]>(Array(OTP_LENGTH).fill(''));
  const [localError, setLocalError] = useState<string | null>(null);
  const inputRefs = useRef<(TextInput | null)[]>([]);
  const { verifierOtp, isLoading, error: authError } = useAuth();

  const handleChange = useCallback(
    (text: string, index: number) => {
      // Only allow digits
      const digit = text.replace(/[^0-9]/g, '').slice(-1);
      const newCode = [...code];
      newCode[index] = digit;
      setCode(newCode);

      // Auto-advance to next input
      if (digit && index < OTP_LENGTH - 1) {
        inputRefs.current[index + 1]?.focus();
      }
    },
    [code],
  );

  const handleKeyPress = useCallback(
    (key: string, index: number) => {
      if (key === 'Backspace' && !code[index] && index > 0) {
        inputRefs.current[index - 1]?.focus();
      }
    },
    [code],
  );

  const handleVerify = useCallback(async () => {
    const otpCode = code.join('');
    if (otpCode.length !== OTP_LENGTH) {
      setLocalError('Veuillez entrer le code complet à 6 chiffres.');
      return;
    }

    setLocalError(null);
    await verifierOtp(telephone, nom, otpCode);
    // If successful, auth state changes and navigation switches to AppStack automatically
  }, [code, telephone, nom, verifierOtp]);

  const handleResend = useCallback(async () => {
    Alert.alert(
      'Code renvoyé',
      'Un nouveau code OTP a été envoyé par SMS à ' + telephone,
    );
  }, [telephone]);

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <View style={styles.content}>
        <Text style={styles.emoji}>🔐</Text>
        <Text style={styles.title}>Vérification OTP</Text>
        <Text style={styles.subtitle}>
          Entrez le code à 6 chiffres envoyé au{'\n'}
          <Text style={styles.phone}>{telephone}</Text>
        </Text>

        {(localError ?? authError) && <ErrorBanner message={(localError ?? authError)!} />}

        <View style={styles.otpContainer}>
          {code.map((digit, index) => (
            <TextInput
              key={index}
              ref={(ref) => {
                inputRefs.current[index] = ref;
              }}
              style={[
                styles.otpInput,
                digit ? styles.otpInputFilled : null,
              ]}
              value={digit}
              onChangeText={(text) => handleChange(text, index)}
              onKeyPress={({ nativeEvent }) =>
                handleKeyPress(nativeEvent.key, index)
              }
              keyboardType="number-pad"
              maxLength={1}
              textContentType="oneTimeCode"
              autoComplete="sms-otp"
            />
          ))}
        </View>

        <PrimaryButton
          title="Vérifier"
          onPress={handleVerify}
          loading={isLoading}
          disabled={code.join('').length !== OTP_LENGTH}
        />

        <View style={styles.resendContainer}>
          <Text style={styles.resendText}>Vous n'avez pas reçu le code ?</Text>
          <Text style={styles.resendLink} onPress={handleResend}>
            Renvoyer le code
          </Text>
        </View>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  content: {
    flex: 1,
    padding: spacing.lg,
    justifyContent: 'center',
    alignItems: 'center',
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
    marginBottom: spacing.xl,
    lineHeight: 22,
  },
  phone: {
    fontWeight: '700',
    color: colors.primary,
  },
  otpContainer: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  otpInput: {
    width: 48,
    height: 56,
    borderWidth: 2,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    textAlign: 'center',
    fontSize: fontSizes.xl,
    fontWeight: '700',
    color: colors.textPrimary,
    backgroundColor: colors.surface,
  },
  otpInputFilled: {
    borderColor: colors.primary,
    backgroundColor: colors.primaryLight,
  },
  resendContainer: {
    marginTop: spacing.xl,
    alignItems: 'center',
  },
  resendText: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  resendLink: {
    fontSize: fontSizes.sm,
    color: colors.primary,
    fontWeight: '600',
    marginTop: spacing.xs,
  },
});
