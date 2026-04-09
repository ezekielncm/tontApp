"use client";

import { useState, useEffect, useCallback } from "react";
import { formatDate } from "@/lib/format";
import type { AuditEntriesResult, AuditEntryDto } from "@/lib/types";
import Pagination from "@/components/Pagination";

export default function AuditTrailPage() {
  const [entries, setEntries] = useState<AuditEntryDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const pageSize = 20;

  const fetchAudit = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const res = await fetch(
        `/api/audit?page=${p}&pageSize=${pageSize}`
      );
      if (res.ok) {
        const data: AuditEntriesResult = await res.json();
        setEntries(data.entries);
        setTotalPages(data.totalPages);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAudit(page);
  }, [page, fetchAudit]);

  return (
    <div className="max-w-6xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Audit Trail</h1>
      <p className="text-sm text-gray-500">
        Historique immuable de toutes les actions sur votre tontine (chaîne
        SHA-256).
      </p>

      <div className="bg-white rounded-lg shadow border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Date
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Action
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Acteur
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Hash
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-sm text-gray-400">
                    Chargement...
                  </td>
                </tr>
              ) : entries.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-sm text-gray-400">
                    Aucune entrée d&apos;audit.
                  </td>
                </tr>
              ) : (
                entries.map((entry) => (
                  <tr key={entry.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {formatDate(entry.timestamp)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {entry.action}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {entry.acteurNom ?? entry.acteurId.slice(0, 8)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-xs font-mono text-gray-400">
                      {entry.hash.slice(0, 16)}…
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
