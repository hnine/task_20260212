using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Tests;

public class CsvParserTests
{
    [Fact]
    public void Parse_ValidCsvWithHeader_ReturnsEmployees()
    {
        var csv = """
            name, email, tel number, joined date
            Alice Johnson, alice@example.com, 010-1234-5678, 2022.03.15
            Bob Smith, bob@example.com, 010-2345-6789, 2021.07.22
            """;

        var result = CsvParser.Parse(csv);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice Johnson", result[0].Name);
        Assert.Equal("alice@example.com", result[0].Email);
        Assert.Equal("010-1234-5678", result[0].TelNumber);
        Assert.Equal(new DateTime(2022, 3, 15), result[0].JoinedDate);
    }

    [Fact]
    public void Parse_CsvWithoutHeader_ReturnsEmployees()
    {
        var csv = "Charlie Lee, charlie@example.com, 010-3456-7890, 2023.01.10";

        var result = CsvParser.Parse(csv);

        Assert.Single(result);
        Assert.Equal("Charlie Lee", result[0].Name);
    }

    [Fact]
    public void Parse_EmptyCsv_ReturnsEmptyList()
    {
        var csv = "";

        var result = CsvParser.Parse(csv);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_CsvWithMalformedLines_SkipsThem()
    {
        var csv = """
            name, email, tel number, joined date
            Alice, alice@example.com
            Bob, bob@example.com, 010-1111-2222, 2022.01.01
            """;

        var result = CsvParser.Parse(csv);

        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    [Fact]
    public void Parse_CsvWithYearDotMonthDotDay_ParsesCorrectly()
    {
        var csv = "Test User, test@example.com, 010-0000-0000, 2024.06.15";

        var result = CsvParser.Parse(csv);

        Assert.Single(result);
        Assert.Equal(new DateTime(2024, 6, 15), result[0].JoinedDate);
    }

    [Fact]
    public void Parse_CsvWithInvalidDate_StillParsesRow()
    {
        var csv = "Test User, test@example.com, 010-0000-0000, not-a-date";

        var result = CsvParser.Parse(csv);

        Assert.Single(result);
        Assert.Equal(default(DateTime), result[0].JoinedDate);
    }
}
