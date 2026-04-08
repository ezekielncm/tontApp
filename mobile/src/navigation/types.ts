/**
 * Navigation type definitions for React Navigation v7.
 * Defines the param lists for AuthStack and AppStack.
 */

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

export type AppStackParamList = {
  Home: undefined;
  TontineDetail: { tontineId: string };
  Paiement: { tontineId: string; tourId: string; montant: number };
  Gestionnaire: { tontineId: string; tourId: string };
  Profil: undefined;
};
