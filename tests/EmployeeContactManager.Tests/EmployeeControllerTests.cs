using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EmployeeContactManager.Tests;

public class EmployeeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EmployeeControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/employee ────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResults()
    {
        var response = await _client.GetAsync("/api/employee?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("items", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("totalCount", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAll_DefaultPagination_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/employee");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_InvalidPageParameter_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/employee?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/employee/{name} ─────────────────────────────────────

    [Fact]
    public async Task GetByName_ExistingEmployee_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/employee/Alice Johnson");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice Johnson", content);
    }

    [Fact]
    public async Task GetByName_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/employee/NonExistentPerson");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/employee — Success cases ───────────────────────────

    [Fact]
    public async Task Post_WithCsvFile_ReturnsCreated()
    {
        var csv = "name, email, tel number, joined date\nTest Person, test@test.com, 010-9999-9999, 2024.01.01";
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithJsonFile_ReturnsCreated()
    {
        var json = """[{"name":"JSON Person","email":"json@test.com","tel":"010-0000-0000","joined":"2024-06-15"}]""";
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", "test.json");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithTextContent_ReturnsCreated()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Test Text, text@test.com, 010-1111-1111, 2024.03.01"), "textContent");
        content.Add(new StringContent("csv"), "format");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── POST /api/employee — Failure cases ───────────────────────────

    [Fact]
    public async Task Post_EmptyData_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(""), "textContent");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEmployeeData_ReturnsBadRequestWithValidationErrors()
    {
        // CSV with invalid data: missing email, bad date
        var csv = "name, email, tel number, joined date\nBadEmp, , 010-0000, not-a-date";
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "bad.csv");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Validation failed", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_UnsupportedFileFormat_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("data"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/employee", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
