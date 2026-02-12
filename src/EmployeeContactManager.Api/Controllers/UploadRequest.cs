namespace EmployeeContactManager.Api.Controllers;

/// <summary>
/// Request model for the Upload endpoint.
/// Enables Swagger/OpenAPI to generate correct schema for multipart/form-data.
/// </summary>
public class UploadRequest
{
    /// <summary>
    /// Optional CSV or JSON file to upload.
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Optional raw text content (CSV or JSON format).
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Format of the text content: "csv" or "json". Defaults to "csv".
    /// </summary>
    public string? Format { get; set; }
}
