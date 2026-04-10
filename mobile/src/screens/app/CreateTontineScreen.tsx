/**
 * Screen to create a new tontine.
 * The creator automatically becomes gestionnaire.
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  ScrollView,
  Alert,
  TouchableOpacity,
} from 'react-native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { tontineService } from '../../services/tontineService';
import { PrimaryButton } from '../../components/PrimaryButton';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';

interface Props {
  navigation: NativeStackNavigationProp<AppStackParamList, 'CreateTontine'>;
}

const PERIODICITIES = [
  { label: 'Hebdomadaire', value: 'Weekly' },
  { label: 'Bimensuelle', value: 'Biweekly' },
  { label: 'Mensuelle', value: 'Monthly' },
];

export function CreateTontineScreen({ navigation }: Props): React.JSX.Element {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [periodicity, setPeriodicity] = useState('Monthly');
  const [maxMembers, setMaxMembers] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleCreate = async () => {
    if (!name.trim()) {
      Alert.alert('Erreur', 'Le nom de la tontine est requis.');
      return;
    }
    const parsedAmount = Number(amount);
    if (!parsedAmount || parsedAmount <= 0) {
      Alert.alert('Erreur', 'Le montant de cotisation doit être supérieur à 0.');
      return;
    }
    const parsedMaxMembers = Number(maxMembers);
    if (!parsedMaxMembers || parsedMaxMembers < 2) {
      Alert.alert('Erreur', 'Le nombre maximum de membres doit être au moins 2.');
      return;
    }

    setIsLoading(true);
    try {
      const result = await tontineService.createTontine({
        name: name.trim(),
        description: description.trim(),
        contributionAmount: parsedAmount,
        periodicity,
        maxMembers: parsedMaxMembers,
      });
      Alert.alert(
        'Tontine créée !',
        'Vous êtes maintenant gestionnaire de cette tontine.',
        [
          {
            text: 'Voir la tontine',
            onPress: () =>
              navigation.replace('TontineDetail', { tontineId: result.id }),
          },
        ],
      );
    } catch (error: any) {
      const message =
        error?.response?.data?.error ?? 'Impossible de créer la tontine.';
      Alert.alert('Erreur', message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.contentContainer}
      keyboardShouldPersistTaps="handled"
    >
      <Text style={styles.title}>Créer une tontine</Text>
      <Text style={styles.subtitle}>
        En créant une tontine, vous devenez automatiquement son gestionnaire.
      </Text>

      <View style={styles.field}>
        <Text style={styles.label}>Nom *</Text>
        <TextInput
          style={styles.input}
          value={name}
          onChangeText={setName}
          placeholder="Ex : Tontine du Quartier"
          placeholderTextColor={colors.textSecondary}
          maxLength={100}
        />
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Description</Text>
        <TextInput
          style={[styles.input, styles.textArea]}
          value={description}
          onChangeText={setDescription}
          placeholder="Description de la tontine..."
          placeholderTextColor={colors.textSecondary}
          multiline
          numberOfLines={3}
          maxLength={500}
        />
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Cotisation (FCFA) *</Text>
        <TextInput
          style={styles.input}
          value={amount}
          onChangeText={setAmount}
          placeholder="Ex : 10000"
          placeholderTextColor={colors.textSecondary}
          keyboardType="numeric"
        />
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Périodicité</Text>
        <View style={styles.periodicityContainer}>
          {PERIODICITIES.map((p) => (
            <TouchableOpacity
              key={p.value}
              style={[
                styles.periodicityOption,
                periodicity === p.value && styles.periodicityOptionActive,
              ]}
              onPress={() => setPeriodicity(p.value)}
              activeOpacity={0.7}
            >
              <Text
                style={[
                  styles.periodicityText,
                  periodicity === p.value && styles.periodicityTextActive,
                ]}
              >
                {p.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Nombre maximum de membres *</Text>
        <TextInput
          style={styles.input}
          value={maxMembers}
          onChangeText={setMaxMembers}
          placeholder="Ex : 12"
          placeholderTextColor={colors.textSecondary}
          keyboardType="numeric"
        />
      </View>

      <PrimaryButton
        title="Créer la tontine"
        onPress={handleCreate}
        loading={isLoading}
        style={styles.createButton}
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
    paddingBottom: spacing.xl * 2,
  },
  title: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    marginBottom: spacing.xl,
    lineHeight: 20,
  },
  field: {
    marginBottom: spacing.lg,
  },
  label: {
    fontSize: fontSizes.sm,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  input: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    fontSize: fontSizes.md,
    color: colors.textPrimary,
  },
  textArea: {
    minHeight: 80,
    textAlignVertical: 'top',
  },
  periodicityContainer: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  periodicityOption: {
    flex: 1,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
  },
  periodicityOptionActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  periodicityText: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
  },
  periodicityTextActive: {
    color: colors.textOnPrimary,
    fontWeight: '600',
  },
  createButton: {
    marginTop: spacing.md,
  },
});
