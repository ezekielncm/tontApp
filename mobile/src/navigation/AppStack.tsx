/**
 * Main application navigation stack.
 * Shown when the user is authenticated.
 * Contains Home, TontineDetail, Paiement, Gestionnaire, and Profil screens.
 */

import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { HomeScreen } from '../screens/app/HomeScreen';
import { TontineDetailScreen } from '../screens/app/TontineDetailScreen';
import { PaiementScreen } from '../screens/app/PaiementScreen';
import { GestionnaireScreen } from '../screens/app/GestionnaireScreen';
import { ProfilScreen } from '../screens/app/ProfilScreen';
import type { AppStackParamList } from './types';
import { colors } from '../config/theme';

const Stack = createNativeStackNavigator<AppStackParamList>();

export function AppStack(): React.JSX.Element {
  return (
    <Stack.Navigator
      initialRouteName="Home"
      screenOptions={{
        headerStyle: { backgroundColor: colors.primary },
        headerTintColor: colors.textOnPrimary,
        headerTitleStyle: { fontWeight: '600' },
        contentStyle: { backgroundColor: colors.background },
        animation: 'slide_from_right',
      }}
    >
      <Stack.Screen
        name="Home"
        component={HomeScreen}
        options={{ title: 'Mes Tontines' }}
      />
      <Stack.Screen
        name="TontineDetail"
        component={TontineDetailScreen}
        options={{ title: 'Détail Tontine' }}
      />
      <Stack.Screen
        name="Paiement"
        component={PaiementScreen}
        options={{ title: 'Paiement' }}
      />
      <Stack.Screen
        name="Gestionnaire"
        component={GestionnaireScreen}
        options={{ title: 'Gestion du tour' }}
      />
      <Stack.Screen
        name="Profil"
        component={ProfilScreen}
        options={{ title: 'Mon Profil' }}
      />
    </Stack.Navigator>
  );
}
