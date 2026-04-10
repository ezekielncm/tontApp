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
import { CreateTontineScreen } from '../screens/app/CreateTontineScreen';
import { ProfilScreen } from '../screens/app/ProfilScreen';
import { RejoindreParCodeScreen } from '../screens/app/RejoindreParCodeScreen';
import { InvitationScreen } from '../screens/app/InvitationScreen';
import { HistoriqueVersementsScreen } from '../screens/app/HistoriqueVersementsScreen';
import { NotificationsScreen } from '../screens/app/NotificationsScreen';
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
        name="CreateTontine"
        component={CreateTontineScreen}
        options={{ title: 'Nouvelle Tontine' }}
      />
      <Stack.Screen
        name="Profil"
        component={ProfilScreen}
        options={{ title: 'Mon Profil' }}
      />
      <Stack.Screen
        name="RejoindreParCode"
        component={RejoindreParCodeScreen}
        options={{ title: 'Rejoindre une tontine' }}
      />
      <Stack.Screen
        name="Invitation"
        component={InvitationScreen}
        options={{ title: 'Inviter des membres' }}
      />
      <Stack.Screen
        name="HistoriqueVersements"
        component={HistoriqueVersementsScreen}
        options={{ title: 'Mes Versements' }}
      />
      <Stack.Screen
        name="Notifications"
        component={NotificationsScreen}
        options={{ title: 'Notifications' }}
      />
    </Stack.Navigator>
  );
}
