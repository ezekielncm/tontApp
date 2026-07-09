"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

interface NavItem {
  label: string;
  href: string;
}

interface SidebarProps {
  role: string;
}

const gestionnaireNav: NavItem[] = [
  { label: "Tableau de bord", href: "/gestionnaire" },
  { label: "Paiements", href: "/gestionnaire/paiements" },
  { label: "Audit Trail", href: "/gestionnaire/audit" },
];

const adminNav: NavItem[] = [
  { label: "Vue d'ensemble", href: "/admin" },
  { label: "Tontines", href: "/admin/tontines" },
  { label: "Alertes", href: "/admin/alertes" },
];

export default function Sidebar({ role }: SidebarProps) {
  const pathname = usePathname();
  const navItems = role === "Admin" ? adminNav : gestionnaireNav;

  return (
    <aside className="w-64 min-h-screen bg-gray-900 text-white p-4">
      <div className="mb-8">
        <h1 className="text-xl font-bold">TontinesApp</h1>
        <p className="text-sm text-gray-400 mt-1">
          {role === "Admin" ? "Admin SaaS" : "Gestionnaire"}
        </p>
      </div>
      <nav className="space-y-1" aria-label="Navigation principale">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActive ? "page" : undefined}
              className={`block px-3 py-2 rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900 ${
                isActive
                  ? "bg-gray-700 text-white"
                  : "text-gray-300 hover:bg-gray-800 hover:text-white"
              }`}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
      <div className="mt-auto pt-8">
        <button
          type="button"
          onClick={async () => {
            await fetch("/api/auth/logout", { method: "POST" });
            window.location.href = "/login";
          }}
          className="w-full px-3 py-2 text-sm text-gray-400 hover:text-white hover:bg-gray-800 rounded-md transition-colors text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900"
        >
          Déconnexion
        </button>
      </div>
    </aside>
  );
}
