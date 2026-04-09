"use client";

import { useState, useEffect, useCallback } from "react";
import { formatMontant } from "@/lib/format";
import StatusBadge from "@/components/StatusBadge";
import Pagination from "@/components/Pagination";

interface TontineListItem {
  id: string;
  nom: string;
  status: string;
  membresCount: number;
  montantCotisation: number;
  periodicite: string;
  createdAt: string;
}

interface TontinesResponse {
  items: TontineListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function AdminTontinesPage() {
  const [tontines, setTontines] = useState<TontineListItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const pageSize = 20;

  const fetchTontines = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const res = await fetch(
        `/api/admin/tontines?page=${p}&pageSize=${pageSize}`
      );
      if (res.ok) {
        const data: TontinesResponse = await res.json();
        setTontines(data.items ?? []);
        setTotalPages(data.totalPages);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTontines(page);
  }, [page, fetchTontines]);

  return (
    <div className="max-w-6xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">
        Toutes les tontines
      </h1>

      <div className="bg-white rounded-lg shadow border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Nom
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Statut
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Membres
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Cotisation
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Périodicité
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Créée le
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td
                    colSpan={6}
                    className="px-6 py-8 text-center text-sm text-gray-400"
                  >
                    Chargement...
                  </td>
                </tr>
              ) : tontines.length === 0 ? (
                <tr>
                  <td
                    colSpan={6}
                    className="px-6 py-8 text-center text-sm text-gray-400"
                  >
                    Aucune tontine trouvée.
                  </td>
                </tr>
              ) : (
                tontines.map((t) => (
                  <tr key={t.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {t.nom}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <StatusBadge status={t.status} />
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {t.membresCount}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {formatMontant(t.montantCotisation)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {t.periodicite}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(t.createdAt).toLocaleDateString("fr-FR")}
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
