using System.Text;
using System.Text.RegularExpressions;

// ────────────────────────────────────────────────────────────────────────────
// SqlGenerator: Parses Employee.cs as text to extract properties, then
// generates StoredProcedures.sql for both MySQL and MSSQL.
// No dependency on the API project — avoids circular build references.
// ────────────────────────────────────────────────────────────────────────────

var apiProjectDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "EmployeeContactManager.Api");

var employeeCsPath = Path.Combine(apiProjectDir, "Domain", "Employee.cs");
var storedProcDir = Path.Combine(apiProjectDir, "StoredProcedures");

if (!File.Exists(employeeCsPath))
{
    Console.Error.WriteLine($"❌ Employee.cs not found: {employeeCsPath}");
    return 1;
}

Console.WriteLine($"🔄 Parsing {Path.GetFileName(employeeCsPath)} to generate SQL...");

// Parse properties from Employee.cs
var columns = ParseEmployeeProperties(File.ReadAllText(employeeCsPath));
if (columns.Count == 0)
{
    Console.Error.WriteLine("❌ No properties found in Employee.cs");
    return 1;
}

Console.WriteLine($"   Found {columns.Count} columns: {string.Join(", ", columns.Select(c => c.Name))}");

// Generate SQL files
Directory.CreateDirectory(Path.Combine(storedProcDir, "MySQL"));
Directory.CreateDirectory(Path.Combine(storedProcDir, "MSSQL"));

File.WriteAllText(Path.Combine(storedProcDir, "MySQL", "StoredProcedures.sql"), GenerateMySql(columns));
File.WriteAllText(Path.Combine(storedProcDir, "MSSQL", "StoredProcedures.sql"), GenerateMsSql(columns));

Console.WriteLine("✔ Generated MySQL/StoredProcedures.sql");
Console.WriteLine("✔ Generated MSSQL/StoredProcedures.sql");
return 0;

// ── Helpers ──────────────────────────────────────────────────────────────────

static List<ColumnDef> ParseEmployeeProperties(string csContent)
{
    var columns = new List<ColumnDef>();
    // Match property declarations: public <type> <name> { get; set; }
    // Excludes class/record declarations by requiring { get; set; } pattern
    var regex = new Regex(@"public\s+(string|DateTime|int|Int32|long|Int64|double|decimal|bool|Guid)\??\s+(\w+)\s*\{\s*get;\s*set;\s*\}", RegexOptions.Multiline);

    foreach (Match match in regex.Matches(csContent))
    {
        var clrType = match.Groups[1].Value;
        var name = match.Groups[2].Value;

        columns.Add(new ColumnDef
        {
            Name = name,
            ClrType = clrType,
            MySqlType = MapToMySqlType(clrType, name),
            MsSqlType = MapToMsSqlType(clrType, name),
            IsPrimaryKey = name == "Name" // Convention: Name is PK
        });
    }

    return columns;
}

static string MapToMySqlType(string clrType, string name)
{
    return clrType switch
    {
        "string" => name == "TelNumber" ? "VARCHAR(50)" : "VARCHAR(200)",
        "DateTime" => "DATETIME",
        "int" or "Int32" => "INT",
        _ => "VARCHAR(200)"
    };
}

static string MapToMsSqlType(string clrType, string name)
{
    return clrType switch
    {
        "string" => name == "TelNumber" ? "NVARCHAR(50)" : "NVARCHAR(200)",
        "DateTime" => "DATETIME2",
        "int" or "Int32" => "INT",
        _ => "NVARCHAR(200)"
    };
}

static string GenerateMySql(List<ColumnDef> columns)
{
    const string tableName = "Employees";
    var sb = new StringBuilder();
    sb.AppendLine("-- =============================================");
    sb.AppendLine($"-- MySQL Schema and Stored Procedures for {tableName} table");
    sb.AppendLine("-- Auto-generated from Employee model");
    sb.AppendLine($"-- Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    sb.AppendLine("-- =============================================");
    sb.AppendLine();

    // Create table
    sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");
    for (var i = 0; i < columns.Count; i++)
    {
        var col = columns[i];
        var line = $"    {col.Name} {col.MySqlType} NOT NULL";
        if (col.IsPrimaryKey) line += " PRIMARY KEY";
        if (i < columns.Count - 1) line += ",";
        sb.AppendLine(line);
    }
    sb.AppendLine(");");
    sb.AppendLine();

    // Drop existing SPs
    foreach (var sp in new[] { "sp_GetAllEmployees", "sp_GetPagedEmployees", "sp_GetEmployeeByName", "sp_EmployeeExists", "sp_InsertEmployee" })
        sb.AppendLine($"DROP PROCEDURE IF EXISTS {sp};");
    sb.AppendLine();

    var colList = string.Join(", ", columns.Select(c => c.Name));
    var pk = columns.First(c => c.IsPrimaryKey);

    sb.AppendLine("DELIMITER //");
    sb.AppendLine();

    // sp_GetAllEmployees
    sb.AppendLine("CREATE PROCEDURE sp_GetAllEmployees()");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    SELECT {colList}");
    sb.AppendLine($"    FROM {tableName}");
    sb.AppendLine("    ORDER BY Name;");
    sb.AppendLine("END //");
    sb.AppendLine();

    // sp_GetPagedEmployees
    sb.AppendLine("CREATE PROCEDURE sp_GetPagedEmployees(IN p_Offset INT, IN p_Limit INT)");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    SELECT COUNT(*) AS TotalCount FROM {tableName};");
    sb.AppendLine($"    SELECT {colList}");
    sb.AppendLine($"    FROM {tableName}");
    sb.AppendLine("    ORDER BY Name LIMIT p_Limit OFFSET p_Offset;");
    sb.AppendLine("END //");
    sb.AppendLine();

    // sp_GetEmployeeByName
    sb.AppendLine($"CREATE PROCEDURE sp_GetEmployeeByName(IN p_{pk.Name} {pk.MySqlType})");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    SELECT {colList}");
    sb.AppendLine($"    FROM {tableName}");
    sb.AppendLine($"    WHERE {pk.Name} = p_{pk.Name};");
    sb.AppendLine("END //");
    sb.AppendLine();

    // sp_EmployeeExists
    sb.AppendLine($"CREATE PROCEDURE sp_EmployeeExists(IN p_{pk.Name} {pk.MySqlType})");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    SELECT COUNT(*) AS ExistsCount FROM {tableName} WHERE {pk.Name} = p_{pk.Name};");
    sb.AppendLine("END //");
    sb.AppendLine();

    // sp_InsertEmployee
    var paramList = string.Join(", ", columns.Select(c => $"IN p_{c.Name} {c.MySqlType}"));
    var valueList = string.Join(", ", columns.Select(c => $"p_{c.Name}"));
    sb.AppendLine($"CREATE PROCEDURE sp_InsertEmployee({paramList})");
    sb.AppendLine("BEGIN");
    sb.AppendLine($"    INSERT INTO {tableName} ({colList})");
    sb.AppendLine($"    VALUES ({valueList});");
    sb.AppendLine("END //");
    sb.AppendLine();
    sb.AppendLine("DELIMITER ;");

    return sb.ToString();
}

static string GenerateMsSql(List<ColumnDef> columns)
{
    const string tableName = "Employees";
    var sb = new StringBuilder();
    sb.AppendLine("-- =============================================");
    sb.AppendLine($"-- MSSQL Schema and Stored Procedures for {tableName} table");
    sb.AppendLine("-- Auto-generated from Employee model");
    sb.AppendLine($"-- Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    sb.AppendLine("-- =============================================");
    sb.AppendLine();

    // Create table
    sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')");
    sb.AppendLine($"CREATE TABLE {tableName} (");
    for (var i = 0; i < columns.Count; i++)
    {
        var col = columns[i];
        var line = $"    {col.Name} {col.MsSqlType} NOT NULL";
        if (col.IsPrimaryKey) line += " PRIMARY KEY";
        if (i < columns.Count - 1) line += ",";
        sb.AppendLine(line);
    }
    sb.AppendLine(");");
    sb.AppendLine("GO");
    sb.AppendLine();

    // Drop existing SPs
    foreach (var sp in new[] { "sp_GetAllEmployees", "sp_GetPagedEmployees", "sp_GetEmployeeByName", "sp_EmployeeExists", "sp_InsertEmployee" })
        sb.AppendLine($"IF OBJECT_ID('{sp}', 'P') IS NOT NULL DROP PROCEDURE {sp};");
    sb.AppendLine("GO");
    sb.AppendLine();

    var colList = string.Join(", ", columns.Select(c => c.Name));
    var pk = columns.First(c => c.IsPrimaryKey);

    // sp_GetAllEmployees
    sb.AppendLine("CREATE PROCEDURE sp_GetAllEmployees");
    sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
    sb.AppendLine($"    SELECT {colList} FROM {tableName} ORDER BY Name;");
    sb.AppendLine("END");
    sb.AppendLine("GO");
    sb.AppendLine();

    // sp_GetPagedEmployees
    sb.AppendLine("CREATE PROCEDURE sp_GetPagedEmployees @Offset INT, @Limit INT");
    sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
    sb.AppendLine($"    SELECT COUNT(*) AS TotalCount FROM {tableName};");
    sb.AppendLine($"    SELECT {colList} FROM {tableName} ORDER BY Name");
    sb.AppendLine("    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;");
    sb.AppendLine("END");
    sb.AppendLine("GO");
    sb.AppendLine();

    // sp_GetEmployeeByName
    sb.AppendLine($"CREATE PROCEDURE sp_GetEmployeeByName @{pk.Name} {pk.MsSqlType}");
    sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
    sb.AppendLine($"    SELECT {colList} FROM {tableName} WHERE {pk.Name} = @{pk.Name};");
    sb.AppendLine("END");
    sb.AppendLine("GO");
    sb.AppendLine();

    // sp_EmployeeExists
    sb.AppendLine($"CREATE PROCEDURE sp_EmployeeExists @{pk.Name} {pk.MsSqlType}");
    sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
    sb.AppendLine($"    SELECT COUNT(*) AS ExistsCount FROM {tableName} WHERE {pk.Name} = @{pk.Name};");
    sb.AppendLine("END");
    sb.AppendLine("GO");
    sb.AppendLine();

    // sp_InsertEmployee
    var paramList = string.Join(", ", columns.Select(c => $"@{c.Name} {c.MsSqlType}"));
    var valueList = string.Join(", ", columns.Select(c => $"@{c.Name}"));
    sb.AppendLine($"CREATE PROCEDURE sp_InsertEmployee {paramList}");
    sb.AppendLine("AS BEGIN SET NOCOUNT ON;");
    sb.AppendLine($"    INSERT INTO {tableName} ({colList}) VALUES ({valueList});");
    sb.AppendLine("END");
    sb.AppendLine("GO");

    return sb.ToString();
}

record ColumnDef
{
    public string Name { get; init; } = "";
    public string ClrType { get; init; } = "";
    public string MySqlType { get; init; } = "";
    public string MsSqlType { get; init; } = "";
    public bool IsPrimaryKey { get; init; }
}
