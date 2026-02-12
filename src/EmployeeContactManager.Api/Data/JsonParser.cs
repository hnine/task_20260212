using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

public static class JsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // JSON uses yyyy-MM-dd as specified in CLAUDE.md
    private static readonly string[] DateFormats = { "yyyy-MM-dd", "yyyy.MM.dd" };

    public static List<Employee> Parse(string jsonContent)
    {
        var records = JsonSerializer.Deserialize<List<JsonEmployeeRecord>>(jsonContent, Options)
                      ?? new List<JsonEmployeeRecord>();

        return records.Select(r =>
        {
            var joinedDate = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(r.Joined))
            {
                DateTime.TryParseExact(r.Joined, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out joinedDate);
            }

            var birthDate = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(r.BirthDate))
            {
                DateTime.TryParseExact(r.BirthDate, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out birthDate);
            }

            return new Employee
            {
                Name = r.Name ?? string.Empty,
                Email = r.Email ?? string.Empty,
                TelNumber = r.Tel ?? string.Empty,
                JoinedDate = joinedDate,
                BirthDate = birthDate
            };
        }).ToList();
    }

    private class JsonEmployeeRecord
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("tel")]
        public string? Tel { get; set; }

        [JsonPropertyName("joined")]
        public string? Joined { get; set; }

        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }
    }
}
