"use client";

import { useState, useEffect, useCallback } from "react";
import StatusBadge from "@/components/StatusBadge";
import Pagination from "@/components/Pagination";

interface AlerteItem {
  id: string;
  type: string;
  message: string;
  severity: "info" | "warning" | "critical";
  timestamp: string;
  resolved: boolean;
}

interface AlertesResponse {
  items: AlerteItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function AlertesPage() {
  const [alertes, setAlertes] = useState<AlerteItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const pageSize = 20;

  const fetchAlertes = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const res = await fetch(`/api/admin/alertes?page=${p}&pageSize=${pageSize}`);
      if (res.ok) {
        const data: AlertesResponse = await res.json();
        setAlertes(data.items ?? []);
        setTotalPages(data.totalPages);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAlertes(page);
  }, [page, fetchAlertes]);

  return (
    <div className="max-w-6xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Alertes</h1>
      <p className="text-sm text-gray-500">
        Alertes SMS, erreurs API, et incidents système.
      </p>

      <div className="bg-white rounded-lg shadow border border-gray-200">
        <div className="divide-y divide-gray-200">
          {loading ? (
            <div className="px-6 py-8 text-center text-sm text-gray-400">
              Chargement...
            </div>
          ) : alertes.length === 0 ? (
            <div className="px-6 py-8 text-center text-sm text-gray-400">
              Aucune alerte. ✅
            </div>
          ) : (
            alertes.map((alerte) => (
              <div
                key={alerte.id}
                className="px-6 py-4 flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <span
                    className={`w-3 h-3 rounded-full flex-shrink-0 ${
                      alerte.severity === "critical"
                        ? "bg-red-500"
                        : alerte.severity === "warning"
                          ? "bg-yellow-500"
                          : "bg-blue-500"
                    }`}
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-900">
                      {alerte.message}
                    </p>
                    <p className="text-xs text-gray-500">
                      {alerte.type} —{" "}
                      {new Date(alerte.timestamp).toLocaleString("fr-FR")}
                    </p>
                  </div>
                </div>
                <StatusBadge
                  status={alerte.resolved ? "Résolu" : alerte.severity}
                />
              </div>
            ))
          )}
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
