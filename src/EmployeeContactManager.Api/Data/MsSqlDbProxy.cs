using System.Data;
using Microsoft.Data.SqlClient;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// MSSQL database proxy using stored procedures via Microsoft.Data.SqlClient (ADO.NET).
/// Table schema and stored procedures are managed from external .sql files.
/// </summary>
public class MsSqlDbProxy : IDbProxy
{
    private readonly string _connectionString;

    public MsSqlDbProxy(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchemaExists();
    }

    private SqlConnection CreateConnection()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>
    /// Reads and executes the StoredProcedures/MSSQL/StoredProcedures.sql file
    /// to create the table and stored procedures.
    /// </summary>
    private void EnsureSchemaExists()
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "StoredProcedures", "MSSQL", "StoredProcedures.sql");
        if (!File.Exists(sqlPath))
            throw new FileNotFoundException($"MSSQL schema file not found: {sqlPath}");

        var fullSql = File.ReadAllText(sqlPath);

        // MSSQL uses GO as batch separator — split and execute each batch
        using var conn = CreateConnection();
        var batches = fullSql.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("--"))
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
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@Limit", pageSize);

        using var reader = cmd.ExecuteReader();

        var totalCount = 0;
        if (reader.Read())
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

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
        cmd.Parameters.AddWithValue("@Name", name);

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
        cmd.Parameters.AddWithValue("@Name", name);

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
                cmd.Parameters.AddWithValue("@Name", emp.Name);
                cmd.Parameters.AddWithValue("@Email", emp.Email);
                cmd.Parameters.AddWithValue("@TelNumber", emp.TelNumber);
                cmd.Parameters.AddWithValue("@JoinedDate", emp.JoinedDate);
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

    private static List<Employee> ReadEmployees(SqlDataReader reader)
    {
        var list = new List<Employee>();
        while (reader.Read())
        {
            list.Add(new Employee
            {
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                TelNumber = reader.GetString(reader.GetOrdinal("TelNumber")),
                JoinedDate = reader.GetDateTime(reader.GetOrdinal("JoinedDate"))
            });
        }
        return list;
    }
}
