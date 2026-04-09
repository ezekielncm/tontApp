/**
 * Hook for fetching the credit profile of a member.
 * Uses React Query for caching and automatic refresh.
 */

import { useQuery } from '@tanstack/react-query';
import apiClient from '../services/apiClient';
import type { ProfilCreditResponse } from '../types/api';
import { QUERY_STALE_TIME_MS, QUERY_CACHE_TIME_MS } from '../config/constants';

async function fetchProfilCredit(
  membreId: string,
): Promise<ProfilCreditResponse> {
  const response = await apiClient.get<ProfilCreditResponse>(
    `/membres/${membreId}/profil-credit`,
  );
  return response.data;
}

export function useProfilCredit(membreId: string | undefined) {
  return useQuery<ProfilCreditResponse>({
    queryKey: ['profil-credit', membreId],
    queryFn: () => fetchProfilCredit(membreId!),
    enabled: !!membreId,
    staleTime: QUERY_STALE_TIME_MS,
    gcTime: QUERY_CACHE_TIME_MS,
    retry: 1,
  });
}
