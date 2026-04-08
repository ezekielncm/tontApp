/**
 * Tontine API service.
 * Communicates with /api/v1/tontines/* endpoints.
 */

import apiClient from './apiClient';
import type { TontineSummary, TontineDetail } from '../types/api';

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
} as const;
