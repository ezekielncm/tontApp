/**
 * Format a number as FCFA currency (thousands separator + FCFA suffix).
 * Example: 1500000 → "1 500 000 FCFA"
 */
export function formatMontant(amount: number): string {
  return (
    new Intl.NumberFormat("fr-FR", {
      maximumFractionDigits: 0,
    }).format(amount) + " FCFA"
  );
}

/**
 * Mask a monetary value for unauthorized roles.
 * Shows "*** FCFA" instead of the real amount.
 */
export function maskMontant(amount: number, authorized: boolean): string {
  return authorized ? formatMontant(amount) : "*** FCFA";
}

/**
 * Format a date to a locale-friendly French string.
 */
export function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat("fr-FR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(dateStr));
}

/**
 * Return a CSS class for payment status badges.
 */
export function statusBadgeClass(status: string): string {
  switch (status.toLowerCase()) {
    case "confirme":
    case "paye":
      return "bg-green-100 text-green-800";
    case "en_attente":
    case "pending":
      return "bg-yellow-100 text-yellow-800";
    case "en_retard":
    case "rejete":
    case "failed":
      return "bg-red-100 text-red-800";
    default:
      return "bg-gray-100 text-gray-800";
  }
}
