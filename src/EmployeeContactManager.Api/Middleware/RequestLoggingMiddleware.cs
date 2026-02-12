using System.Text;

namespace EmployeeContactManager.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;

        _logger.LogInformation("[{RequestId}] → {Method} {Path}{Query}",
            requestId, method, path, queryString);

        // Trace: log request headers
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var headers = new StringBuilder();
            foreach (var header in context.Request.Headers)
            {
                headers.AppendLine($"  {header.Key}: {header.Value}");
            }
            _logger.LogTrace("[{RequestId}] Headers:\n{Headers}", requestId, headers.ToString().TrimEnd());

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
                    _logger.LogTrace("[{RequestId}] Body:\n{Body}", requestId, truncated);
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
                _logger.LogTrace("[{RequestId}] Form data:\n{FormInfo}", requestId, formInfo.ToString().TrimEnd());
            }
        }

        try
        {
            await _next(context);

            _logger.LogInformation("[{RequestId}] ← {StatusCode} {Method} {Path}",
                requestId, context.Response.StatusCode, method, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] ✖ {Method} {Path} — Unhandled exception",
                requestId, method, path);
            throw;
        }
    }
}
