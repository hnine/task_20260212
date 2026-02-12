using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace EmployeeContactManager.Api;

/// <summary>
/// Singleton application logger. All logging goes through AppLogger.Log.
/// No DI injection needed — access via AppLogger.Log anywhere.
/// Format is configurable via Serilog:WriteTo:Console:Args:outputTemplate in appsettings.json.
/// </summary>
public static class AppLogger
{
    /// <summary>
    /// The singleton Serilog logger instance.
    /// Configured once at startup from appsettings.json.
    /// </summary>
    public static ILogger Log => Serilog.Log.Logger;

    /// <summary>
    /// Get a logger with a specific context (namespace/class name).
    /// Usage: private static readonly ILogger Logger = AppLogger.ForContext<MyClass>();
    /// </summary>
    public static ILogger ForContext<T>() => Log.ForContext<T>();

    /// <summary>
    /// Get a logger with a specific source context string.
    /// </summary>
    public static ILogger ForContext(string sourceContext) =>
        Log.ForContext("SourceContext", sourceContext);

    // ── Convenience methods with caller info ────────────────────────

    public static void Info(string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var source = Path.GetFileNameWithoutExtension(filePath);
        Log.ForContext("SourceContext", source)
           .ForContext("CallerMethod", caller)
           .Information("[{CallerMethod}] {Message}", caller, message);
    }

    public static void Debug(string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var source = Path.GetFileNameWithoutExtension(filePath);
        Log.ForContext("SourceContext", source)
           .ForContext("CallerMethod", caller)
           .Debug("[{CallerMethod}] {Message}", caller, message);
    }

    public static void Warn(string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var source = Path.GetFileNameWithoutExtension(filePath);
        Log.ForContext("SourceContext", source)
           .ForContext("CallerMethod", caller)
           .Warning("[{CallerMethod}] {Message}", caller, message);
    }

    public static void Error(Exception? ex, string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var source = Path.GetFileNameWithoutExtension(filePath);
        Log.ForContext("SourceContext", source)
           .ForContext("CallerMethod", caller)
           .Error(ex, "[{CallerMethod}] {Message}", caller, message);
    }
}
