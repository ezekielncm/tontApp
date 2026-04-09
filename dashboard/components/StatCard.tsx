interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  trend?: "up" | "down" | "neutral";
  trendValue?: string;
}

export default function StatCard({
  title,
  value,
  subtitle,
  trend,
  trendValue,
}: StatCardProps) {
  const trendColor =
    trend === "up"
      ? "text-green-600"
      : trend === "down"
        ? "text-red-600"
        : "text-gray-500";

  const trendIcon =
    trend === "up" ? "↑" : trend === "down" ? "↓" : "→";

  return (
    <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
      <p className="text-sm font-medium text-gray-500">{title}</p>
      <p className="mt-2 text-3xl font-bold text-gray-900">{value}</p>
      {(subtitle || trendValue) && (
        <div className="mt-2 flex items-center gap-2">
          {trendValue && (
            <span className={`text-sm font-medium ${trendColor}`}>
              {trendIcon} {trendValue}
            </span>
          )}
          {subtitle && (
            <span className="text-sm text-gray-500">{subtitle}</span>
          )}
        </div>
      )}
    </div>
  );
}
