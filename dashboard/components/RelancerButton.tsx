"use client";

import { useState } from "react";

interface RelancerButtonProps {
  membreId: string;
  membreNom: string;
  tontineId: string;
}

export default function RelancerButton({
  membreId,
  membreNom,
  tontineId,
}: RelancerButtonProps) {
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  async function handleRelancer() {
    setLoading(true);
    try {
      const res = await fetch("/api/relancer", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ membreId, tontineId }),
      });
      if (res.ok) {
        setSent(true);
      }
    } finally {
      setLoading(false);
    }
  }

  if (sent) {
    return (
      <span className="text-sm text-green-600 font-medium">
        ✓ Relance envoyée à {membreNom}
      </span>
    );
  }

  return (
    <button
      onClick={handleRelancer}
      disabled={loading}
      className="px-3 py-1 text-sm bg-orange-500 text-white rounded-md hover:bg-orange-600 disabled:opacity-50 transition-colors"
    >
      {loading ? "Envoi..." : `Relancer ${membreNom}`}
    </button>
  );
}
