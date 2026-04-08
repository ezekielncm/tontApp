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
  /** Current tour info for badge display */
  tourActuel: TourInfo | null;
}

export type TontineStatus =
  | 'Draft'
  | 'Active'
  | 'Suspended'
  | 'Completed'
  | 'Cancelled';

/** Payment status for a member in a tour */
export type StatutPaiement = 'paye' | 'en_attente' | 'en_retard';

/** Tontine detail */
export interface TontineDetail extends TontineSummary {
  gestionnaireName: string;
  gestionnaireId: string;
  membres: TontineMember[];
}

export interface TontineMember {
  id: string;
  nom: string;
  telephone: string;
  dateAdhesion: string;
  /** Payment status in current tour */
  statutPaiement?: StatutPaiement;
}

export interface TourInfo {
  id: string;
  numero: number;
  beneficiaireNom: string;
  dateOuverture: string;
  dateCloture: string | null;
  estOuvert: boolean;
  /** Number of payments received for this tour */
  nombrePaiementsRecus: number;
  /** Total payments expected for this tour */
  nombrePaiementsAttendus: number;
}

/** User profile */
export interface UserProfile {
  id: string;
  telephone: string;
  nom: string;
  dateInscription: string;
}

// ─── Payment types ─────────────────────────────────────────────────────────────

/** POST /api/v1/versements/initier body */
export interface InitierVersementRequest {
  tontineId: string;
  tourId: string;
  montant: number;
}

/** Payment initiation response */
export interface InitierVersementResponse {
  versementId: string;
  statut: VersementStatut;
  /** External transaction reference from the payment provider */
  transactionRef: string;
}

/** Versement status */
export type VersementStatut = 'en_attente' | 'confirme' | 'rejete';

/** GET /api/v1/versements/:id status response */
export interface VersementStatusResponse {
  versementId: string;
  statut: VersementStatut;
  montant: number;
  dateCreation: string;
  dateConfirmation: string | null;
}

// ─── Gestionnaire types ────────────────────────────────────────────────────────

/** Late member info for the gestionnaire screen */
export interface MembreRetardataire {
  membreId: string;
  nom: string;
  telephone: string;
  joursRetard: number;
}

/** POST /api/v1/tontines/:id/tours/:tourId/relancer body */
export interface RelancerSmsRequest {
  membreIds: string[];
}

/** POST /api/v1/tontines/:id/tours/:tourId/clore response */
export interface CloreTourResponse {
  tourId: string;
  estCloture: boolean;
}

// ─── Credit Scoring types ──────────────────────────────────────────────────────

/** GET /api/v1/membres/:id/profil-credit response */
export interface ProfilCreditResponse {
  membreId: string;
  score: number;
  niveau: ProfilCreditNiveau;
  donneesInsuffisantes: boolean;
  composantes: ComposantesScore;
  calculeLe: string;
}

export type ProfilCreditNiveau = 'Excellent' | 'Bon' | 'Moyen' | 'Faible';

export interface ComposantesScore {
  cyclesCompletes: number;
  tauxPonctualite: number;
  ancienneteEnMois: number;
  contributionCycles: number;
  contributionPonctualite: number;
  contributionAnciennete: number;
}
