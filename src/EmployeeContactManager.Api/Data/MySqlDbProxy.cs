using System.Data;
using MySqlConnector;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// MySQL database proxy using stored procedures via MySqlConnector (ADO.NET).
/// Table schema and stored procedures are managed from external .sql files.
/// </summary>
public class MySqlDbProxy : IDbProxy
{
    private readonly string _connectionString;

    public MySqlDbProxy(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchemaExists();
    }

    private MySqlConnection CreateConnection()
    {
        var conn = new MySqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>
    /// Reads and executes the StoredProcedures/MySQL/StoredProcedures.sql file
    /// to create the table and stored procedures.
    /// </summary>
    private void EnsureSchemaExists()
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "StoredProcedures", "MySQL", "StoredProcedures.sql");
        if (!File.Exists(sqlPath))
            throw new FileNotFoundException($"MySQL schema file not found: {sqlPath}");

        var fullSql = File.ReadAllText(sqlPath);

        // MySQL requires splitting on DELIMITER and handling separately
        // Split into individual statements: before DELIMITER, between //, and after DELIMITER ;
        using var conn = CreateConnection();

        // 1. Execute statements before DELIMITER (CREATE TABLE, DROP PROCEDURE)
        var delimiterIdx = fullSql.IndexOf("DELIMITER //", StringComparison.OrdinalIgnoreCase);
        if (delimiterIdx >= 0)
        {
            var preDelimiter = fullSql[..delimiterIdx];
            ExecuteStatements(conn, preDelimiter, ";");

            // 2. Execute stored procedure CREATE statements (split on //)
            var endDelimiterIdx = fullSql.IndexOf("DELIMITER ;", delimiterIdx + 12, StringComparison.OrdinalIgnoreCase);
            var spBlock = endDelimiterIdx >= 0
                ? fullSql[(delimiterIdx + 12)..endDelimiterIdx]
                : fullSql[(delimiterIdx + 12)..];

            var spStatements = spBlock.Split("//", StringSplitOptions.RemoveEmptyEntries);
            foreach (var sp in spStatements)
            {
                var trimmed = sp.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = trimmed;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        else
        {
            // No DELIMITER block, execute all as regular statements
            ExecuteStatements(conn, fullSql, ";");
        }
    }

    private static void ExecuteStatements(MySqlConnection conn, string sql, string delimiter)
    {
        var statements = sql.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        foreach (var stmt in statements)
        {
            var trimmed = stmt.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) &&
                !trimmed.StartsWith("--") &&
                !trimmed.Equals("DELIMITER", StringComparison.OrdinalIgnoreCase))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = trimmed;
                cmd.ExecuteNonQuery();
            }
        }
    }

    public IEnumerable<Employee> GetAll()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_GetAllEmployees";
        cmd.CommandType = CommandType.StoredProcedure;

        using var reader = cmd.ExecuteReader();
        return ReadEmployees(reader);
    }

    public (IEnumerable<Employee> Items, int TotalCount) GetPaged(int page, int pageSize)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_GetPagedEmployees";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("p_Limit", pageSize);

        using var reader = cmd.ExecuteReader();

        var totalCount = 0;
        if (reader.Read())
            totalCount = reader.GetInt32("TotalCount");

        reader.NextResult();
        var items = ReadEmployees(reader);

        return (items, totalCount);
    }

    public Employee? GetByName(string name)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_GetEmployeeByName";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_Name", name);

        using var reader = cmd.ExecuteReader();
        var employees = ReadEmployees(reader);
        return employees.FirstOrDefault();
    }

    public bool Exists(string name)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_EmployeeExists";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_Name", name);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public void AddRange(IEnumerable<Employee> employees)
    {
        using var conn = CreateConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            foreach (var emp in employees)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = "sp_InsertEmployee";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Name", emp.Name);
                cmd.Parameters.AddWithValue("p_Email", emp.Email);
                cmd.Parameters.AddWithValue("p_TelNumber", emp.TelNumber);
                cmd.Parameters.AddWithValue("p_JoinedDate", emp.JoinedDate);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static List<Employee> ReadEmployees(MySqlDataReader reader)
    {
        var list = new List<Employee>();
        while (reader.Read())
        {
            list.Add(new Employee
            {
                Name = reader.GetString("Name"),
                Email = reader.GetString("Email"),
                TelNumber = reader.GetString("TelNumber"),
                JoinedDate = reader.GetDateTime("JoinedDate")
            });
        }
        return list;
    }
}
