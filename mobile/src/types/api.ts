/**
 * API response types matching the ASP.NET Core backend DTOs.
 */

/** Mirrors Application.IdentityManagement.DTOs.AuthResult */
export interface AuthResult {
  utilisateurId: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

/** POST /api/v1/auth/register body */
export interface RegisterRequest {
  telephone: string;
  nom: string;
  motDePasse: string;
}

/** POST /api/v1/auth/login body */
export interface LoginRequest {
  telephone: string;
  motDePasse: string;
}

/** POST /api/v1/auth/refresh body */
export interface RefreshRequest {
  refreshToken: string;
}

/** Generic API error shape returned by the backend */
export interface ApiError {
  error: string;
}

/** Tontine summary for list views */
export interface TontineSummary {
  id: string;
  nom: string;
  description: string;
  status: TontineStatus;
  montantCotisation: number;
  devise: string;
  frequence: string;
  nombreMembres: number;
  dateCreation: string;
}

export type TontineStatus =
  | 'Draft'
  | 'Active'
  | 'Suspended'
  | 'Completed'
  | 'Cancelled';

/** Tontine detail */
export interface TontineDetail extends TontineSummary {
  gestionnaireName: string;
  membres: TontineMember[];
  tourActuel: TourInfo | null;
}

export interface TontineMember {
  id: string;
  nom: string;
  telephone: string;
  dateAdhesion: string;
}

export interface TourInfo {
  id: string;
  numero: number;
  beneficiaireNom: string;
  dateOuverture: string;
  dateCloture: string | null;
  estOuvert: boolean;
}

/** User profile */
export interface UserProfile {
  id: string;
  telephone: string;
  nom: string;
  dateInscription: string;
}
