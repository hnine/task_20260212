using Microsoft.AspNetCore.Mvc;
using EmployeeContactManager.Api.CQRS.Commands;
using EmployeeContactManager.Api.CQRS.Handlers;
using EmployeeContactManager.Api.CQRS.Queries;
using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeeController : ControllerBase
{
    private readonly GetAllEmployeesHandler _getAllHandler;
    private readonly GetEmployeeByNameHandler _getByNameHandler;
    private readonly AddEmployeesHandler _addHandler;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        GetAllEmployeesHandler getAllHandler,
        GetEmployeeByNameHandler getByNameHandler,
        AddEmployeesHandler addHandler,
        ILogger<EmployeeController> logger)
    {
        _getAllHandler = getAllHandler;
        _getByNameHandler = getByNameHandler;
        _addHandler = addHandler;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/employee?page={page}&amp;pageSize={pageSize}
    /// Returns paginated employee list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllEmployeesResult), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { message = "Page and pageSize must be positive integers." });

        _logger.LogInformation("GET /api/employee?page={Page}&pageSize={PageSize}", page, pageSize);
        var query = new GetAllEmployeesQuery(page, pageSize);
        var result = _getAllHandler.Handle(query);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/employee/{name}
    /// Returns employee details by name.
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(Employee), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Employee name is required." });

        _logger.LogInformation("GET /api/employee/{Name}", name);
        var query = new GetEmployeeByNameQuery(name);
        var employee = _getByNameHandler.Handle(query);

        if (employee is null)
            return NotFound(new { message = $"Employee '{name}' not found." });

        return Ok(employee);
    }

    /// <summary>
    /// POST /api/employee
    /// Upload a CSV or JSON file, or submit text content directly.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest request)
    {
        var file = request.File;
        var textContent = request.TextContent;
        var format = request.Format;

        _logger.LogInformation("POST /api/employee — file: {FileName}, textContent length: {Len}, format: {Format}",
            file?.FileName ?? "none", textContent?.Length ?? 0, format ?? "none");

        var employees = new List<Employee>();

        // Handle file upload
        if (file is not null && file.Length > 0)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            try
            {
                employees.AddRange(extension switch
                {
                    ".csv" => CsvParser.Parse(content),
                    ".json" => JsonParser.Parse(content),
                    _ => throw new ArgumentException($"Unsupported file format: {extension}. Use .csv or .json")
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse uploaded file {FileName}", file.FileName);
                return BadRequest(new { message = $"Failed to parse file: {ex.Message}" });
            }
        }

        // Handle text content
        if (!string.IsNullOrWhiteSpace(textContent))
        {
            var parsedFormat = format?.ToLowerInvariant() ?? "csv";
            try
            {
                employees.AddRange(parsedFormat switch
                {
                    "csv" => CsvParser.Parse(textContent),
                    "json" => JsonParser.Parse(textContent),
                    _ => throw new ArgumentException($"Unsupported format: {parsedFormat}. Use 'csv' or 'json'")
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse text content");
                return BadRequest(new { message = $"Failed to parse text: {ex.Message}" });
            }
        }

        if (employees.Count == 0)
            return BadRequest(new { message = "No employees found in the provided data. Please upload a file or enter text content." });

        var command = new AddEmployeesCommand(employees);
        var result = _addHandler.Handle(command);

        // Return validation errors if any
        if (result.ValidationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Validation failed for one or more employees.",
                errors = result.ValidationErrors
            });
        }

        return CreatedAtAction(nameof(GetAll), new { page = 1, pageSize = 10 },
            new { message = $"{result.AddedCount} employee(s) added successfully.", count = result.AddedCount });
    }
}
