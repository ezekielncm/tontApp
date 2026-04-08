/**
 * Payment API service.
 * Communicates with /api/v1/versements/* endpoints.
 */

import apiClient from './apiClient';
import type {
  InitierVersementRequest,
  InitierVersementResponse,
  VersementStatusResponse,
} from '../types/api';

export const paymentService = {
  /**
   * POST /versements/initier
   * Initiate a Mobile Money payment (Orange Money).
   */
  async initierVersement(
    data: InitierVersementRequest,
  ): Promise<InitierVersementResponse> {
    const response = await apiClient.post<InitierVersementResponse>(
      '/versements/initier',
      data,
    );
    return response.data;
  },

  /**
   * GET /versements/:id/statut
   * Poll payment status.
   */
  async getVersementStatus(
    versementId: string,
  ): Promise<VersementStatusResponse> {
    const response = await apiClient.get<VersementStatusResponse>(
      `/versements/${versementId}/statut`,
    );
    return response.data;
  },
} as const;
