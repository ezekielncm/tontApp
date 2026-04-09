namespace Infrastructure.Monitoring;

using Microsoft.AspNetCore.Http;

/// <summary>
/// ASP.NET Core middleware that records HTTP request metrics for Prometheus.
/// </summary>
public sealed class PrometheusMiddleware
{
    private readonly RequestDelegate _next;

    public PrometheusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var endpoint = context.Request.Path.Value ?? "/";

        using var timer = TontAppMetrics.StartHttpRequestTimer(method, endpoint);

        await _next(context);

        TontAppMetrics.RecordHttpRequest(method, endpoint, context.Response.StatusCode);
    }
}
