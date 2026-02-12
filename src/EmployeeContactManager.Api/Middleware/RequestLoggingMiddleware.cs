using System.Text;
using ILogger = Serilog.ILogger;

namespace EmployeeContactManager.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ILogger Logger = AppLogger.ForContext<RequestLoggingMiddleware>();

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;

        Logger.Information("[{RequestId}] → {Method} {Path}{Query}",
            requestId, method, path, queryString);

        // Trace: log request headers
        if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
        {
            var headers = new StringBuilder();
            foreach (var header in context.Request.Headers)
            {
                headers.AppendLine($"  {header.Key}: {header.Value}");
            }
            Logger.Verbose("[{RequestId}] Headers:\n{Headers}", requestId, headers.ToString().TrimEnd());

            // Trace: log request body (for non-file requests)
            if (context.Request.ContentType != null &&
                !context.Request.ContentType.Contains("multipart/form-data"))
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true)
                    .ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    var truncated = body.Length > 2000 ? body[..2000] + "... (truncated)" : body;
                    Logger.Verbose("[{RequestId}] Body:\n{Body}", requestId, truncated);
                }
            }
            else if (context.Request.ContentType?.Contains("multipart/form-data") == true)
            {
                // For multipart, log form field names and file info (not the full body)
                context.Request.EnableBuffering();
                var formInfo = new StringBuilder();
                if (context.Request.HasFormContentType)
                {
                    var form = await context.Request.ReadFormAsync();
                    foreach (var field in form)
                    {
                        var val = field.Value.ToString();
                        var display = val.Length > 200 ? val[..200] + "..." : val;
                        formInfo.AppendLine($"  [field] {field.Key} = {display}");
                    }
                    foreach (var file in form.Files)
                    {
                        formInfo.AppendLine($"  [file] {file.Name}: {file.FileName} ({file.Length} bytes, {file.ContentType})");
                    }
                }
                Logger.Verbose("[{RequestId}] Form data:\n{FormInfo}", requestId, formInfo.ToString().TrimEnd());
            }
        }

        try
        {
            await _next(context);

            Logger.Information("[{RequestId}] ← {StatusCode} {Method} {Path}",
                requestId, context.Response.StatusCode, method, path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[{RequestId}] ✖ {Method} {Path} — Unhandled exception",
                requestId, method, path);
            throw;
        }
    }
}
