using EmployeeContactManager.Api.CQRS.Commands;
using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Domain;
using ILogger = Serilog.ILogger;

namespace EmployeeContactManager.Api.CQRS.Handlers;

public class AddEmployeesHandler
{
    private readonly IDbProxy _db;
    private static readonly ILogger Logger = AppLogger.ForContext<AddEmployeesHandler>();

    public AddEmployeesHandler(IDbProxy db)
    {
        _db = db;
    }

    public AddEmployeesResult Handle(AddEmployeesCommand command)
    {
        var validationErrors = EmployeeValidator.ValidateBatch(command.Employees);
        if (validationErrors.Count > 0)
        {
            var errorMessages = validationErrors
                .SelectMany(kvp => kvp.Value.Select(e => $"Row {kvp.Key + 1}: [{e.Field}] {e.Message}"))
                .ToList();

            Logger.Warning("Validation failed for {ErrorCount} employee(s): {Errors}",
                validationErrors.Count, string.Join("; ", errorMessages));

            return new AddEmployeesResult(0, errorMessages);
        }

        // Check for exact duplicates (same name, email, tel number) against existing DB
        var duplicateErrors = CheckForDuplicates(command.Employees);
        if (duplicateErrors.Count > 0)
        {
            Logger.Warning("Duplicate employee(s) detected: {Errors}", string.Join("; ", duplicateErrors));
            return new AddEmployeesResult(0, duplicateErrors);
        }

        // Deduplicate names: if only name matches (but different data), rename to "{name} {counter}"
        var deduplicatedEmployees = DeduplicateNames(command.Employees);

        Logger.Information("Adding {Count} employees", deduplicatedEmployees.Count);
        _db.AddRange(deduplicatedEmployees);
        Logger.Information("Successfully added {Count} employees", deduplicatedEmployees.Count);
        return new AddEmployeesResult(deduplicatedEmployees.Count, new List<string>());
    }

    /// <summary>
    /// Checks if any employee in the batch is an exact duplicate of an existing record
    /// (same name, email, and tel number).
    /// Also checks for exact duplicates within the batch itself.
    /// </summary>
    private List<string> CheckForDuplicates(IReadOnlyList<Employee> employees)
    {
        var errors = new List<string>();
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < employees.Count; i++)
        {
            var emp = employees[i];
            var key = $"{emp.Name}|{emp.Email}|{emp.TelNumber}";

            // Check duplicate within the same batch
            if (!seenInBatch.Add(key))
            {
                errors.Add($"Row {i + 1}: Duplicate employee data — '{emp.Name}' with email '{emp.Email}' and tel '{emp.TelNumber}' appears multiple times in the upload.");
                continue;
            }

            // Check duplicate against existing DB records
            var existing = _db.GetByName(emp.Name);
            if (existing is not null &&
                string.Equals(existing.Email, emp.Email, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.TelNumber, emp.TelNumber, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Row {i + 1}: Employee '{emp.Name}' with email '{emp.Email}' and tel '{emp.TelNumber}' already exists.");
            }
        }

        return errors;
    }

    private List<Employee> DeduplicateNames(IReadOnlyList<Employee> employees)
    {
        var result = new List<Employee>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var emp in employees)
        {
            var finalName = emp.Name;

            // Check if name already exists in DB or in this batch
            if (_db.Exists(finalName) || usedNames.Contains(finalName))
            {
                var counter = 2;
                do
                {
                    finalName = $"{emp.Name} {counter}";
                    counter++;
                } while (_db.Exists(finalName) || usedNames.Contains(finalName));

                Logger.Debug("Duplicate name '{OriginalName}' renamed to '{NewName}'",
                    emp.Name, finalName);
            }

            usedNames.Add(finalName);
            result.Add(new Employee
            {
                Name = finalName,
                Email = emp.Email,
                TelNumber = emp.TelNumber,
                JoinedDate = emp.JoinedDate,
                BirthDate = emp.BirthDate
            });
        }

        return result;
    }
}
