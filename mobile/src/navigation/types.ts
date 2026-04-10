/**
 * Navigation type definitions for React Navigation v7.
 * Defines the param lists for AuthStack and AppStack.
 */

export type AuthStackParamList = {
  Onboarding: undefined;
  Login: undefined;
  Register: undefined;
  VerifierOtp: { telephone: string; nom: string };
};

export type AppStackParamList = {
  Home: undefined;
  TontineDetail: { tontineId: string };
  Paiement: { tontineId: string; tourId: string; montant: number };
  Gestionnaire: { tontineId: string; tourId: string };
  CreateTontine: undefined;
  Profil: undefined;
  RejoindreParCode: undefined;
  Invitation: { tontineId: string; tontineNom: string; montant: number };
  HistoriqueVersements: { tontineId?: string } | undefined;
  Notifications: undefined;
};
