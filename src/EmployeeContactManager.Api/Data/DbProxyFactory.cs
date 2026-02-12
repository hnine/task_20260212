namespace EmployeeContactManager.Api.Data;

/// <summary>
/// Factory that creates the appropriate IDbProxy based on configuration.
/// Reads "Database:Type" and "Database:ConnectionString" from appsettings.json.
/// Supported types: InMemory, FileDB, MySQL, MSSQL
/// </summary>
public static class DbProxyFactory
{
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
