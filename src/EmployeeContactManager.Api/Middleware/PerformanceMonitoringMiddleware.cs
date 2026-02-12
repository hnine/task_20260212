using System.Diagnostics;

namespace EmployeeContactManager.Api.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (elapsed > 500)
        {
            _logger.LogWarning("⚠ Slow request: {Method} {Path} took {Elapsed}ms",
                context.Request.Method, context.Request.Path, elapsed);
        }
        else
        {
            _logger.LogDebug("⏱ {Method} {Path} completed in {Elapsed}ms",
                context.Request.Method, context.Request.Path, elapsed);
        }
    }
}
