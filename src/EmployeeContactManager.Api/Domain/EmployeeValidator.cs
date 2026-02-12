using System.Globalization;
using System.Text.RegularExpressions;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Domain;

public record ValidationError(string Field, string Message);

public static class EmployeeValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] ValidDateFormats = {
        "yyyy.MM.dd", "yyyy-MM-dd"
    };

    /// <summary>
    /// Validates a single employee. Returns list of errors (empty = valid).
    /// </summary>
    public static List<ValidationError> Validate(Employee employee)
    {
        var errors = new List<ValidationError>();

        // Name — required
        if (string.IsNullOrWhiteSpace(employee.Name))
            errors.Add(new ValidationError("Name", "Name is required."));

        // Email — required + format
        if (string.IsNullOrWhiteSpace(employee.Email))
            errors.Add(new ValidationError("Email", "Email is required."));
        else if (!EmailRegex.IsMatch(employee.Email))
            errors.Add(new ValidationError("Email", $"Invalid email format: '{employee.Email}'."));

        // Joined date — must not be default
        if (employee.JoinedDate == default)
            errors.Add(new ValidationError("JoinedDate", "Joined date is required and must be a valid date (yyyy.MM.dd or yyyy-MM-dd)."));

        return errors;
    }

    /// <summary>
    /// Validates a batch. Returns per-employee errors keyed by index.
    /// </summary>
    public static Dictionary<int, List<ValidationError>> ValidateBatch(List<Employee> employees)
    {
        var allErrors = new Dictionary<int, List<ValidationError>>();
        for (int i = 0; i < employees.Count; i++)
        {
            var errors = Validate(employees[i]);
            if (errors.Count > 0)
                allErrors[i] = errors;
        }
        return allErrors;
    }
}
