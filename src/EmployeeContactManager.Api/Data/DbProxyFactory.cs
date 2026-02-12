using Serilog;
using ILogger = Serilog.ILogger;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// Factory that creates the appropriate IDbProxy based on configuration.
/// Reads "Database:Type" and "Database:ConnectionString" from appsettings.json.
/// Supported types: InMemory, FileDB, MySQL, MSSQL
/// Validates connection strings before creating proxies for MySQL/MSSQL.
/// </summary>
public static class DbProxyFactory
{
    private static readonly ILogger Logger = AppLogger.ForContext("DbProxyFactory");

    public static IDbProxy Create(IConfiguration configuration)
    {
        var dbType = configuration.GetValue<string>("Database:Type") ?? "InMemory";
        var connectionString = configuration.GetValue<string>("Database:ConnectionString") ?? "";

        // Validate connection string for database types that require it
        if (RequiresConnectionString(dbType) && string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Database type '{dbType}' requires a valid ConnectionString in appsettings.json. " +
                $"Set Database:ConnectionString to a valid value.");
        }

        // Validate connection string format and connectivity for MySQL/MSSQL
        if (RequiresConnectionString(dbType))
        {
            ValidateConnectionString(dbType, connectionString);
            TestConnection(dbType, connectionString);
        }

        return dbType.ToLowerInvariant() switch
        {
            "inmemory" => new InMemoryDbProxy(),
            "filedb" => new FileDbProxy(connectionString),
            "mysql" => new MySqlDbProxy(connectionString),
            "mssql" => new MsSqlDbProxy(connectionString),
            _ => throw new InvalidOperationException(
                $"Unsupported database type: '{dbType}'. Supported: InMemory, FileDB, MySQL, MSSQL")
        };
    }

    /// <summary>
    /// Validates the connection string format by checking for required keys.
    /// </summary>
    private static void ValidateConnectionString(string dbType, string connectionString)
    {
        Logger.Information("Validating {DbType} connection string format...", dbType);

        var normalized = dbType.ToLowerInvariant();
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Contains('='))
            .ToDictionary(
                p => p[..p.IndexOf('=')].Trim(),
                p => p[(p.IndexOf('=') + 1)..].Trim(),
                StringComparer.OrdinalIgnoreCase
            );

        // Check required keys
        var requiredKeys = normalized switch
        {
            "mysql" => new[] { "Server", "Database" },
            "mssql" => new[] { "Server", "Database" },
            _ => Array.Empty<string>()
        };

        // MSSQL also accepts "Data Source" for Server, "Initial Catalog" for Database
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Server", new[] { "Server", "Data Source", "Host" } },
            { "Database", new[] { "Database", "Initial Catalog" } }
        };

        var missingKeys = new List<string>();
        foreach (var key in requiredKeys)
        {
            var possibleNames = aliases.ContainsKey(key) ? aliases[key] : new[] { key };
            if (!possibleNames.Any(name => parts.ContainsKey(name)))
            {
                missingKeys.Add($"'{key}' (or: {string.Join(", ", possibleNames.Skip(1).Select(n => $"'{n}'"))})");
            }
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid {dbType} connection string — missing required keys: {string.Join(", ", missingKeys)}. " +
                $"Connection string: \"{MaskPassword(connectionString)}\"");
        }

        Logger.Information("✔ {DbType} connection string format is valid", dbType);
    }

    /// <summary>
    /// Tests actual connectivity to the database before server starts.
    /// </summary>
    private static void TestConnection(string dbType, string connectionString)
    {
        Logger.Information("Testing {DbType} connection...", dbType);

        try
        {
            switch (dbType.ToLowerInvariant())
            {
                case "mysql":
                    using (var conn = new MySqlConnector.MySqlConnection(connectionString))
                    {
                        conn.Open();
                        Logger.Information("✔ {DbType} connection successful (server version: {Version})",
                            dbType, conn.ServerVersion);
                    }
                    break;

                case "mssql":
                    using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                    {
                        conn.Open();
                        Logger.Information("✔ {DbType} connection successful (server version: {Version})",
                            dbType, conn.ServerVersion);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to {dbType} database. " +
                $"Connection string: \"{MaskPassword(connectionString)}\". " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Masks the password in a connection string for safe logging.
    /// </summary>
    private static string MaskPassword(string connectionString)
    {
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            var trimmed = parts[i].Trim();
            if (trimmed.StartsWith("Password", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Pwd", StringComparison.OrdinalIgnoreCase))
            {
                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx >= 0)
                    parts[i] = trimmed[..(eqIdx + 1)] + "****";
            }
        }
        return string.Join(";", parts);
    }

    private static bool RequiresConnectionString(string dbType)
    {
        return dbType.ToLowerInvariant() switch
        {
            "mysql" => true,
            "mssql" => true,
            _ => false
        };
    }
}
