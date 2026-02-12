using System.Reflection;
using System.Text;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Templates;

/// <summary>
/// T4-style template that auto-generates stored procedure SQL scripts
/// from the Employee model. Run GenerateAll() when the Employee model changes
/// to regenerate StoredProcedures/MySQL/ and StoredProcedures/MSSQL/ scripts.
///
/// Usage in code or CLI:
///   var generator = new StoredProcedureGenerator();
///   generator.GenerateAll("path/to/StoredProcedures");
/// </summary>
public class StoredProcedureGenerator
{
    private const string TableName = "Employees";

    // Column definitions derived from Employee model
    private static readonly List<ColumnDef> Columns = GetColumnsFromModel();

    private static List<ColumnDef> GetColumnsFromModel()
    {
        var props = typeof(Employee).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<ColumnDef>();

        foreach (var prop in props)
        {
            columns.Add(new ColumnDef
            {
                PropertyName = prop.Name,
                ClrType = prop.PropertyType,
                MySqlType = MapToMySqlType(prop.PropertyType, prop.Name),
                MsSqlType = MapToMsSqlType(prop.PropertyType, prop.Name),
                IsPrimaryKey = prop.Name == "Name" // Convention: Name is PK
            });
        }

        return columns;
    }

    private static string MapToMySqlType(Type type, string name)
    {
        if (type == typeof(string))
            return name == "TelNumber" ? "VARCHAR(50)" : "VARCHAR(200)";
        if (type == typeof(DateTime))
            return "DATETIME";
        if (type == typeof(int))
            return "INT";
        return "VARCHAR(200)";
    }

    private static string MapToMsSqlType(Type type, string name)
    {
        if (type == typeof(string))
            return name == "TelNumber" ? "NVARCHAR(50)" : "NVARCHAR(200)";
        if (type == typeof(DateTime))
            return "DATETIME2";
        if (type == typeof(int))
            return "INT";
        return "NVARCHAR(200)";
    }

    /// <summary>
    /// Generates all SQL scripts for both MySQL and MSSQL.
    /// </summary>
    public void GenerateAll(string basePath)
    {
        var mysqlDir = Path.Combine(basePath, "MySQL");
        var mssqlDir = Path.Combine(basePath, "MSSQL");
        Directory.CreateDirectory(mysqlDir);
        Directory.CreateDirectory(mssqlDir);

        File.WriteAllText(Path.Combine(mysqlDir, "StoredProcedures.sql"), GenerateMySql());
        File.WriteAllText(Path.Combine(mssqlDir, "StoredProcedures.sql"), GenerateMsSql());
    }

    public string GenerateMySql()
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- =============================================");
        sb.AppendLine($"-- MySQL Stored Procedures for {TableName} table");
        sb.AppendLine("-- Auto-generated from Employee model");
        sb.AppendLine($"-- Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- =============================================");
        sb.AppendLine();

        // Drop existing
        foreach (var sp in new[] { "sp_GetAllEmployees", "sp_GetPagedEmployees", "sp_GetEmployeeByName", "sp_EmployeeExists", "sp_InsertEmployee" })
            sb.AppendLine($"DROP PROCEDURE IF EXISTS {sp};");
        sb.AppendLine();

        // Create table
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {TableName} (");
        for (var i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            var line = $"    {col.PropertyName} {col.MySqlType} NOT NULL";
            if (col.IsPrimaryKey) line += " PRIMARY KEY";
            if (i < Columns.Count - 1) line += ",";
            sb.AppendLine(line);
        }
        sb.AppendLine(");");
        sb.AppendLine();

        var colList = string.Join(", ", Columns.Select(c => c.PropertyName));

        // sp_GetAllEmployees
        sb.AppendLine("DELIMITER //");
        sb.AppendLine();
        sb.AppendLine("CREATE PROCEDURE sp_GetAllEmployees()");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    SELECT {colList}");
        sb.AppendLine($"    FROM {TableName}");
        sb.AppendLine("    ORDER BY Name;");
        sb.AppendLine("END //");
        sb.AppendLine();

        // sp_GetPagedEmployees
        sb.AppendLine("CREATE PROCEDURE sp_GetPagedEmployees(IN p_Offset INT, IN p_Limit INT)");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    SELECT COUNT(*) AS TotalCount FROM {TableName};");
        sb.AppendLine($"    SELECT {colList}");
        sb.AppendLine($"    FROM {TableName}");
        sb.AppendLine("    ORDER BY Name LIMIT p_Limit OFFSET p_Offset;");
        sb.AppendLine("END //");
        sb.AppendLine();

        // sp_GetEmployeeByName
        var pk = Columns.First(c => c.IsPrimaryKey);
        sb.AppendLine($"CREATE PROCEDURE sp_GetEmployeeByName(IN p_{pk.PropertyName} {pk.MySqlType})");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    SELECT {colList}");
        sb.AppendLine($"    FROM {TableName}");
        sb.AppendLine($"    WHERE {pk.PropertyName} = p_{pk.PropertyName};");
        sb.AppendLine("END //");
        sb.AppendLine();

        // sp_EmployeeExists
        sb.AppendLine($"CREATE PROCEDURE sp_EmployeeExists(IN p_{pk.PropertyName} {pk.MySqlType})");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    SELECT COUNT(*) AS ExistsCount FROM {TableName} WHERE {pk.PropertyName} = p_{pk.PropertyName};");
        sb.AppendLine("END //");
        sb.AppendLine();

        // sp_InsertEmployee
        var paramList = string.Join(", ", Columns.Select(c => $"IN p_{c.PropertyName} {c.MySqlType}"));
        var valueList = string.Join(", ", Columns.Select(c => $"p_{c.PropertyName}"));
        sb.AppendLine($"CREATE PROCEDURE sp_InsertEmployee({paramList})");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    INSERT INTO {TableName} ({colList})");
        sb.AppendLine($"    VALUES ({valueList});");
        sb.AppendLine("END //");
        sb.AppendLine();
        sb.AppendLine("DELIMITER ;");

        return sb.ToString();
    }

    public string GenerateMsSql()
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- =============================================");
        sb.AppendLine($"-- MSSQL Stored Procedures for {TableName} table");
        sb.AppendLine("-- Auto-generated from Employee model");
        sb.AppendLine($"-- Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- =============================================");
        sb.AppendLine();

        // Drop existing
        foreach (var sp in new[] { "sp_GetAllEmployees", "sp_GetPagedEmployees", "sp_GetEmployeeByName", "sp_EmployeeExists", "sp_InsertEmployee" })
            sb.AppendLine($"IF OBJECT_ID('{sp}', 'P') IS NOT NULL DROP PROCEDURE {sp};");
        sb.AppendLine("GO");
        sb.AppendLine();

        // Create table
        sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{TableName}' AND xtype='U')");
        sb.AppendLine($"CREATE TABLE {TableName} (");
        for (var i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            var line = $"    {col.PropertyName} {col.MsSqlType} NOT NULL";
            if (col.IsPrimaryKey) line += " PRIMARY KEY";
            if (i < Columns.Count - 1) line += ",";
            sb.AppendLine(line);
        }
        sb.AppendLine(");");
        sb.AppendLine("GO");
        sb.AppendLine();

        var colList = string.Join(", ", Columns.Select(c => c.PropertyName));

        // sp_GetAllEmployees
        sb.AppendLine("CREATE PROCEDURE sp_GetAllEmployees");
        sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
        sb.AppendLine($"    SELECT {colList} FROM {TableName} ORDER BY Name;");
        sb.AppendLine("END");
        sb.AppendLine("GO");
        sb.AppendLine();

        // sp_GetPagedEmployees
        sb.AppendLine("CREATE PROCEDURE sp_GetPagedEmployees @Offset INT, @Limit INT");
        sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
        sb.AppendLine($"    SELECT COUNT(*) AS TotalCount FROM {TableName};");
        sb.AppendLine($"    SELECT {colList} FROM {TableName} ORDER BY Name");
        sb.AppendLine("    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;");
        sb.AppendLine("END");
        sb.AppendLine("GO");
        sb.AppendLine();

        // sp_GetEmployeeByName
        var pk = Columns.First(c => c.IsPrimaryKey);
        sb.AppendLine($"CREATE PROCEDURE sp_GetEmployeeByName @{pk.PropertyName} {pk.MsSqlType}");
        sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
        sb.AppendLine($"    SELECT {colList} FROM {TableName} WHERE {pk.PropertyName} = @{pk.PropertyName};");
        sb.AppendLine("END");
        sb.AppendLine("GO");
        sb.AppendLine();

        // sp_EmployeeExists
        sb.AppendLine($"CREATE PROCEDURE sp_EmployeeExists @{pk.PropertyName} {pk.MsSqlType}");
        sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
        sb.AppendLine($"    SELECT COUNT(*) AS ExistsCount FROM {TableName} WHERE {pk.PropertyName} = @{pk.PropertyName};");
        sb.AppendLine("END");
        sb.AppendLine("GO");
        sb.AppendLine();

        // sp_InsertEmployee
        var paramList = string.Join(", ", Columns.Select(c => $"@{c.PropertyName} {c.MsSqlType}"));
        var valueList = string.Join(", ", Columns.Select(c => $"@{c.PropertyName}"));
        sb.AppendLine($"CREATE PROCEDURE sp_InsertEmployee {paramList}");
        sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
        sb.AppendLine($"    INSERT INTO {TableName} ({colList}) VALUES ({valueList});");
        sb.AppendLine("END");
        sb.AppendLine("GO");

        return sb.ToString();
    }

    private class ColumnDef
    {
        public string PropertyName { get; set; } = "";
        public Type ClrType { get; set; } = typeof(string);
        public string MySqlType { get; set; } = "";
        public string MsSqlType { get; set; } = "";
        public bool IsPrimaryKey { get; set; }
    }
}
