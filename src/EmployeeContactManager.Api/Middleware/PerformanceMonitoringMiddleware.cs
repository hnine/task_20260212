using System.Diagnostics;
using ILogger = Serilog.ILogger;

namespace EmployeeContactManager.Api.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ILogger Logger = AppLogger.ForContext<PerformanceMonitoringMiddleware>();

    public PerformanceMonitoringMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (elapsed > 500)
        {
            Logger.Warning("⚠ Slow request: {Method} {Path} took {Elapsed}ms",
                context.Request.Method, context.Request.Path, elapsed);
        }
        else
        {
            Logger.Debug("⏱ {Method} {Path} completed in {Elapsed}ms",
                context.Request.Method, context.Request.Path, elapsed);
        }
    }
}
