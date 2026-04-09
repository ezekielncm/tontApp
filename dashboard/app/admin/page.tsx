import { cookies } from "next/headers";
import { apiFetch } from "@/lib/api";
import { formatMontant } from "@/lib/format";
import type { AdminMetrics } from "@/lib/types";
import StatCard from "@/components/StatCard";
import StatusBadge from "@/components/StatusBadge";

async function getAdminMetrics(token: string): Promise<AdminMetrics | null> {
  try {
    return await apiFetch<AdminMetrics>("/api/v1/dashboard/admin/metrics", {
      token,
    });
  } catch {
    // Return mock data for development
    return {
      tontinesActives: 0,
      totalMembres: 0,
      volumeFcfaSemaine: 0,
      smsEnvoyesSemaine: 0,
      smsEchecsSemaine: 0,
      tauxErreurApi: 0,
      alertes: [],
    };
  }
}

export default async function DashboardAdminPage() {
  const cookieStore = await cookies();
  const token = cookieStore.get("accessToken")?.value ?? "";

  const metrics = await getAdminMetrics(token);

  if (!metrics) {
    return (
      <div className="max-w-4xl">
        <h1 className="text-2xl font-bold text-gray-900 mb-4">
          Dashboard Admin SaaS
        </h1>
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md">
          Impossible de charger les métriques. Vérifiez la connexion API.
        </div>
      </div>
    );
  }

  const tauxSmsFail =
    metrics.smsEnvoyesSemaine > 0
      ? ((metrics.smsEchecsSemaine / metrics.smsEnvoyesSemaine) * 100).toFixed(
          1
        )
      : "0";

  return (
    <div className="max-w-7xl space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Dashboard Admin SaaS
        </h1>
        <p className="text-gray-500 mt-1">
          Métriques globales de la plateforme TontinesApp
        </p>
      </div>

      {/* ── KPI Cards ──────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
        <StatCard
          title="Tontines actives"
          value={metrics.tontinesActives}
          trend="up"
        />
        <StatCard
          title="Membres inscrits"
          value={metrics.totalMembres}
          trend="up"
        />
        <StatCard
          title="Volume FCFA (semaine)"
          value={formatMontant(metrics.volumeFcfaSemaine)}
        />
        <StatCard
          title="SMS envoyés (semaine)"
          value={metrics.smsEnvoyesSemaine}
          subtitle={`${metrics.smsEchecsSemaine} échecs`}
          trend={Number(tauxSmsFail) > 10 ? "down" : "up"}
          trendValue={`${tauxSmsFail}% échec`}
        />
        <StatCard
          title="Taux erreur API"
          value={`${metrics.tauxErreurApi.toFixed(2)}%`}
          trend={metrics.tauxErreurApi > 5 ? "down" : "up"}
          trendValue={metrics.tauxErreurApi > 5 ? "Élevé" : "Normal"}
        />
      </div>

      {/* ── Alertes SMS / Système ──────────────────────────────────── */}
      <div className="bg-white rounded-lg shadow border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">
            Alertes système
          </h2>
        </div>
        <div className="divide-y divide-gray-200">
          {metrics.alertes.length === 0 ? (
            <div className="px-6 py-8 text-center text-sm text-gray-400">
              Aucune alerte active. ✅
            </div>
          ) : (
            metrics.alertes.map((alerte) => (
              <div
                key={alerte.id}
                className="px-6 py-4 flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <span
                    className={`w-2.5 h-2.5 rounded-full ${
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
      </div>

      {/* ── Monitoring Links ───────────────────────────────────────── */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            Grafana
          </h3>
          <p className="text-sm text-gray-500 mb-4">
            Dashboards temps réel : latence API, taux SMS, connexions DB
          </p>
          <a
            href={process.env.NEXT_PUBLIC_GRAFANA_URL ?? "http://localhost:3001"}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center px-4 py-2 bg-purple-600 text-white text-sm font-medium rounded-md hover:bg-purple-700 transition-colors"
          >
            Ouvrir Grafana ↗
          </a>
        </div>
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            Prometheus
          </h3>
          <p className="text-sm text-gray-500 mb-4">
            Métriques brutes et requêtes PromQL
          </p>
          <a
            href={
              process.env.NEXT_PUBLIC_PROMETHEUS_URL ??
              "http://localhost:9090"
            }
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center px-4 py-2 bg-orange-600 text-white text-sm font-medium rounded-md hover:bg-orange-700 transition-colors"
          >
            Ouvrir Prometheus ↗
          </a>
        </div>
      </div>
    </div>
  );
}
