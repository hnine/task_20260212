using System.Globalization;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

public static class CsvParser
{
    // CSV uses yyyy.MM.dd as specified in CLAUDE.md
    private static readonly string[] DateFormats = {
        "yyyy.MM.dd", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy",
        "yyyy/MM/dd", "M/d/yyyy", "d/M/yyyy"
    };

    public static List<Employee> Parse(string csvContent)
    {
        var employees = new List<Employee>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Skip header row
            if (trimmed.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = trimmed.Split(',');
            if (parts.Length < 4)
                continue;

            var name = parts[0].Trim();
            var email = parts[1].Trim();
            var tel = parts[2].Trim();
            var dateStr = parts[3].Trim();

            if (!DateTime.TryParseExact(dateStr, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var joinedDate))
            {
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out joinedDate);
            }

            // BirthDate is optional (5th column)
            var birthDate = DateTime.MinValue;
            if (parts.Length >= 5)
            {
                var birthStr = parts[4].Trim();
                if (!DateTime.TryParseExact(birthStr, DateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out birthDate))
                {
                    DateTime.TryParse(birthStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out birthDate);
                }
            }

            employees.Add(new Employee
            {
                Name = name,
                Email = email,
                TelNumber = tel,
                JoinedDate = joinedDate,
                BirthDate = birthDate
            });
        }

        return employees;
    }
}
