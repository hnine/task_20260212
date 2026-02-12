using EmployeeContactManager.Api.Data;

namespace EmployeeContactManager.Tests;

public class JsonParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsEmployees()
    {
        var json = """
            [
              { "name": "Fiona Wang", "email": "fiona@example.com", "tel": "010-6789-0123", "joined": "2023-05-20" },
              { "name": "George Han", "email": "george@example.com", "tel": "010-7890-1234", "joined": "2021-12-01" }
            ]
            """;

        var result = JsonParser.Parse(json);

        Assert.Equal(2, result.Count);
        Assert.Equal("Fiona Wang", result[0].Name);
        Assert.Equal("fiona@example.com", result[0].Email);
        Assert.Equal("010-6789-0123", result[0].TelNumber);
        Assert.Equal(new DateTime(2023, 5, 20), result[0].JoinedDate);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyList()
    {
        var json = "[]";

        var result = JsonParser.Parse(json);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SingleEmployee_ReturnsSingleItem()
    {
        var json = """
            [{ "name": "Test", "email": "test@test.com", "tel": "000-0000-0000", "joined": "2024-01-01" }]
            """;

        var result = JsonParser.Parse(json);

        Assert.Single(result);
        Assert.Equal("Test", result[0].Name);
    }

    [Fact]
    public void Parse_InvalidDateFormat_ReturnsDefaultDate()
    {
        var json = """
            [{ "name": "Bad Date", "email": "bad@test.com", "tel": "000", "joined": "not-a-date" }]
            """;

        var result = JsonParser.Parse(json);

        Assert.Single(result);
        Assert.Equal(default(DateTime), result[0].JoinedDate);
    }
}
