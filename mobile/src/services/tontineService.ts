/**
 * Tontine API service.
 * Communicates with /api/v1/tontines/* endpoints.
 * Maps backend DTOs (English camelCase) to mobile types (French camelCase).
 */

import apiClient from './apiClient';
import type {
  TontineSummary,
  TontineDetail,
  TontineStatus,
  MembreRetardataire,
  RelancerSmsRequest,
  CloreTourResponse,
  CreateTontineRequest,
  CreateTontineResponse,
  GenererCodeInvitationResponse,
  RejoindreParCodeRequest,
  VersementDto,
  UpdateSmsPreferencesRequest,
  TourActuelDto,
} from '../types/api';

// ─── Backend DTO shapes (actual JSON from ASP.NET Core) ────────────────────────

/** Matches Application.TontineManagement.Queries.GetTontineById.TontineDto */
interface BackendTontineDto {
  id: string;
  name: string;
  description: string | null;
  contributionAmount: number;
  currency: string;
  periodicity: string;
  status: string;
  maxMembers: number;
  currentMemberCount: number;
  gestionnaireId: string;
  createdAt: string;
}

// ─── Mappers ───────────────────────────────────────────────────────────────────

function mapToTontineSummary(dto: BackendTontineDto): TontineSummary {
  return {
    id: dto.id,
    nom: dto.name,
    description: dto.description ?? '',
    status: dto.status as TontineStatus,
    montantCotisation: dto.contributionAmount,
    devise: dto.currency,
    frequence: dto.periodicity,
    nombreMembres: dto.currentMemberCount,
    dateCreation: dto.createdAt,
    tourActuel: null, // Tour info not included in list endpoint yet
  };
}

function mapToTontineDetail(dto: BackendTontineDto): TontineDetail {
  return {
    ...mapToTontineSummary(dto),
    gestionnaireName: '',
    gestionnaireId: dto.gestionnaireId,
    membres: [], // Member list not included in detail endpoint yet
  };
}

// ─── Service ───────────────────────────────────────────────────────────────────

export const tontineService = {
  /**
   * GET /tontines/mes-tontines
   * Fetch user's active tontines.
   */
  async getMyTontines(): Promise<TontineSummary[]> {
    const response = await apiClient.get<BackendTontineDto[]>('/tontines/mes-tontines');
    return response.data.map(mapToTontineSummary);
  },

  /**
   * GET /tontines/:id
   * Get detailed tontine information.
   */
  async getTontineById(id: string): Promise<TontineDetail> {
    const response = await apiClient.get<BackendTontineDto>(`/tontines/${id}`);
    return mapToTontineDetail(response.data);
  },

  /**
   * GET /tontines/:id/tours/:tourId/retardataires
   * Get late members for the current tour.
   */
  async getRetardataires(
    tontineId: string,
    tourId: string,
  ): Promise<MembreRetardataire[]> {
    const response = await apiClient.get<MembreRetardataire[]>(
      `/tontines/${tontineId}/tours/${tourId}/retardataires`,
    );
    return response.data;
  },

  /**
   * POST /tontines/:id/tours/:tourId/relancer
   * Send SMS reminders to late members.
   */
  async relancerSms(
    tontineId: string,
    tourId: string,
    data: RelancerSmsRequest,
  ): Promise<void> {
    await apiClient.post(
      `/tontines/${tontineId}/tours/${tourId}/relancer`,
      data,
    );
  },

  /**
   * POST /tontines/:id/tours/:tourId/clore
   * Close the current tour (gestionnaire only).
   */
  async cloreTour(
    tontineId: string,
    tourId: string,
  ): Promise<CloreTourResponse> {
    const response = await apiClient.post<CloreTourResponse>(
      `/tontines/${tontineId}/tours/${tourId}/clore`,
    );
    return response.data;
  },

  /**
   * POST /tontines
   * Create a new tontine. The creator becomes gestionnaire.
   */
  async createTontine(data: CreateTontineRequest): Promise<CreateTontineResponse> {
    const response = await apiClient.post<CreateTontineResponse>('/tontines', data);
    return response.data;
  },

  /**
   * GET /tontines/:id/invitation/generer
   * Generate an invitation code (gestionnaire only).
   */
  async genererCodeInvitation(tontineId: string): Promise<GenererCodeInvitationResponse> {
    const response = await apiClient.get<GenererCodeInvitationResponse>(
      `/tontines/${tontineId}/invitation/generer`,
    );
    return response.data;
  },

  /**
   * POST /tontines/rejoindre
   * Join a tontine using an invitation code.
   */
  async rejoindreParCode(data: RejoindreParCodeRequest): Promise<void> {
    await apiClient.post('/tontines/rejoindre', data);
  },

  /**
   * GET /membres/moi/versements
   * Get all payments made by the authenticated user.
   */
  async getMesVersements(tontineId?: string): Promise<VersementDto[]> {
    const params = tontineId ? { tontineId } : {};
    const response = await apiClient.get<VersementDto[]>('/membres/moi/versements', { params });
    return response.data;
  },

  /**
   * PUT /membres/moi/sms-preferences
   * Update SMS opt-out preference.
   */
  async updateSmsPreferences(data: UpdateSmsPreferencesRequest): Promise<void> {
    await apiClient.put('/membres/moi/sms-preferences', data);
  },

  /**
   * GET /tontines/:id/tour-actuel
   * Get the current tour info for a tontine.
   */
  async getTourActuel(tontineId: string): Promise<TourActuelDto | null> {
    try {
      const response = await apiClient.get<TourActuelDto>(`/tontines/${tontineId}/tour-actuel`);
      return response.data;
    } catch {
      return null;
    }
  },
} as const;
