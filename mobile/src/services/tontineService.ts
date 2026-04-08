/**
 * Tontine API service.
 * Communicates with /api/v1/tontines/* endpoints.
 */

import apiClient from './apiClient';
import type {
  TontineSummary,
  TontineDetail,
  MembreRetardataire,
  RelancerSmsRequest,
  CloreTourResponse,
} from '../types/api';

export const tontineService = {
  /**
   * GET /tontines
   * Fetch user's active tontines.
   */
  async getMyTontines(): Promise<TontineSummary[]> {
    const response = await apiClient.get<TontineSummary[]>('/tontines');
    return response.data;
  },

  /**
   * GET /tontines/:id
   * Get detailed tontine information.
   */
  async getTontineById(id: string): Promise<TontineDetail> {
    const response = await apiClient.get<TontineDetail>(`/tontines/${id}`);
    return response.data;
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
} as const;
