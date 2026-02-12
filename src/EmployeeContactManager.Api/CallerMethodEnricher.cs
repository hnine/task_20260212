using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog.Core;
using Serilog.Events;

namespace EmployeeContactManager.Api;

/// <summary>
/// Serilog enricher that automatically captures the calling method name
/// from the stack trace and adds it as a "CallerMethod" property.
/// Resolves async state machine MoveNext() to the original method name.
/// </summary>
public class CallerMethodEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SkipPrefixes = new()
    {
        "Serilog",
        "EmployeeContactManager.Api.CallerMethodEnricher",
        "EmployeeContactManager.Api.AppLogger"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var stackTrace = new StackTrace(true);
        var frames = stackTrace.GetFrames();
        if (frames is null) return;

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method is null) continue;

            var declaringType = method.DeclaringType;
            if (declaringType is null) continue;

            var fullName = declaringType.FullName ?? "";

            // Skip Serilog internals, enricher, and AppLogger
            if (SkipPrefixes.Any(p => fullName.StartsWith(p, StringComparison.Ordinal)))
                continue;

            // Skip system/runtime/Microsoft frames
            if (fullName.StartsWith("System.", StringComparison.Ordinal) ||
                fullName.StartsWith("Microsoft.", StringComparison.Ordinal))
                continue;

            var methodName = method.Name;

            // Handle async state machine: MoveNext on a compiler-generated type
            if (methodName == "MoveNext" &&
                declaringType.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine)))
            {
                // The declaring type name is like: <InvokeAsync>d__2
                // Extract "InvokeAsync" from between < and >
                var typeName = declaringType.Name;
                var ltIndex = typeName.IndexOf('<');
                var gtIndex = typeName.IndexOf('>');
                if (ltIndex >= 0 && gtIndex > ltIndex)
                {
                    methodName = typeName[(ltIndex + 1)..gtIndex];
                }
            }
            // Handle compiler-generated local functions: <<Main>$>g__SeedData|0_0
            else if (methodName.Contains('<') || methodName.Contains('>'))
            {
                var gIndex = methodName.IndexOf("__", StringComparison.Ordinal);
                var pipeIndex = methodName.IndexOf('|', gIndex >= 0 ? gIndex : 0);
                if (gIndex >= 0 && pipeIndex > gIndex + 2)
                {
                    methodName = methodName[(gIndex + 2)..pipeIndex];
                }
                else
                {
                    continue; // Skip unresolvable compiler-generated methods
                }
            }

            var property = propertyFactory.CreateProperty("CallerMethod", methodName);
            logEvent.AddPropertyIfAbsent(property);
            return;
        }
    }
}
