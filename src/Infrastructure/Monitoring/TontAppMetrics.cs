namespace Infrastructure.Monitoring;

using Prometheus;

/// <summary>
/// Custom Prometheus metrics for TontinesApp monitoring.
/// Thread-safe singleton counters and histograms.
/// </summary>
public static class TontAppMetrics
{
    // ── Payments ────────────────────────────────────────────────────────
    private static readonly Counter PaiementsTotal = Metrics.CreateCounter(
        "tontapp_paiements_total",
        "Total number of payment attempts",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    private static readonly Histogram PaiementDuration = Metrics.CreateHistogram(
        "tontapp_paiement_duration_seconds",
        "Duration of payment processing",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 8) // 0.1s to ~25s
        });

    // ── SMS ─────────────────────────────────────────────────────────────
    private static readonly Counter SmsTotal = Metrics.CreateCounter(
        "tontapp_sms_total",
        "Total SMS sent",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    private static readonly Counter SmsFailures = Metrics.CreateCounter(
        "tontapp_sms_failures_total",
        "Total SMS delivery failures");

    // ── API ─────────────────────────────────────────────────────────────
    private static readonly Counter HttpRequestsTotal = Metrics.CreateCounter(
        "tontapp_http_requests_total",
        "Total HTTP requests",
        new CounterConfiguration { LabelNames = new[] { "method", "endpoint", "status_code" } });

    private static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
        "tontapp_http_request_duration_seconds",
        "HTTP request duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint" },
            Buckets = new[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
        });

    // ── Tontines ────────────────────────────────────────────────────────
    private static readonly Gauge TontinesActives = Metrics.CreateGauge(
        "tontapp_tontines_actives",
        "Number of currently active tontines");

    private static readonly Gauge MembresTotal = Metrics.CreateGauge(
        "tontapp_membres_total",
        "Total number of registered members");

    // ── Public helpers ──────────────────────────────────────────────────

    public static void RecordPayment(string status) =>
        PaiementsTotal.WithLabels(status).Inc();

    public static ITimer StartPaymentTimer() =>
        PaiementDuration.NewTimer();

    public static void RecordSms(string status) =>
        SmsTotal.WithLabels(status).Inc();

    public static void RecordSmsFailure() =>
        SmsFailures.Inc();

    public static void RecordHttpRequest(string method, string endpoint, int statusCode) =>
        HttpRequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();

    public static ITimer StartHttpRequestTimer(string method, string endpoint) =>
        HttpRequestDuration.WithLabels(method, endpoint).NewTimer();

    public static void SetTontinesActives(double count) =>
        TontinesActives.Set(count);

    public static void SetMembresTotal(double count) =>
        MembresTotal.Set(count);
}
