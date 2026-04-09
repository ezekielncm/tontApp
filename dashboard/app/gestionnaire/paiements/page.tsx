"use client";

import { useState, useEffect, useCallback } from "react";
import { formatMontant, formatDate } from "@/lib/format";
import type { VersementDto } from "@/lib/types";
import StatusBadge from "@/components/StatusBadge";
import Pagination from "@/components/Pagination";

interface PaiementsResponse {
  items: VersementDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function PaiementsPage() {
  const [paiements, setPaiements] = useState<VersementDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const pageSize = 20;

  const fetchPaiements = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const res = await fetch(
        `/api/paiements?page=${p}&pageSize=${pageSize}`
      );
      if (res.ok) {
        const data: PaiementsResponse = await res.json();
        setPaiements(data.items ?? []);
        setTotalPages(data.totalPages);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPaiements(page);
  }, [page, fetchPaiements]);

  return (
    <div className="max-w-6xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Paiements</h1>
      <p className="text-sm text-gray-500">
        Historique complet des versements pour votre tontine.
      </p>

      <div className="bg-white rounded-lg shadow border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Payeur
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Montant
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Statut
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Date
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td
                    colSpan={4}
                    className="px-6 py-8 text-center text-sm text-gray-400"
                  >
                    Chargement...
                  </td>
                </tr>
              ) : paiements.length === 0 ? (
                <tr>
                  <td
                    colSpan={4}
                    className="px-6 py-8 text-center text-sm text-gray-400"
                  >
                    Aucun paiement enregistré.
                  </td>
                </tr>
              ) : (
                paiements.map((p) => (
                  <tr key={p.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {p.payeurNom}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatMontant(p.montant)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <StatusBadge status={p.status} />
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {formatDate(p.dateCreation)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        <div className="px-6 border-t border-gray-200">
          <Pagination
            page={page}
            totalPages={totalPages}
            onPageChange={setPage}
          />
        </div>
      </div>
    </div>
  );
}
