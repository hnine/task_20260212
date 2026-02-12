using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Tests;

public class EmployeeValidatorTests
{
    // ── Failure cases ────────────────────────────────────────────────

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var emp = new Employee { Name = "", Email = "test@test.com", TelNumber = "010-0000", JoinedDate = DateTime.Now };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Contains(errors, e => e.Field == "Name");
    }

    [Fact]
    public void Validate_MissingEmail_ReturnsError()
    {
        var emp = new Employee { Name = "Test", Email = "", TelNumber = "010-0000", JoinedDate = DateTime.Now };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Contains(errors, e => e.Field == "Email");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsError()
    {
        var emp = new Employee { Name = "Test", Email = "not-an-email", TelNumber = "010-0000", JoinedDate = DateTime.Now };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Contains(errors, e => e.Field == "Email" && e.Message.Contains("Invalid email"));
    }

    [Fact]
    public void Validate_InvalidEmailNoAt_ReturnsError()
    {
        var emp = new Employee { Name = "Test", Email = "bademail.com", TelNumber = "010-0000", JoinedDate = DateTime.Now };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Contains(errors, e => e.Field == "Email");
    }

    [Fact]
    public void Validate_DefaultDate_ReturnsError()
    {
        var emp = new Employee { Name = "Test", Email = "test@test.com", TelNumber = "010-0000", JoinedDate = default };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Contains(errors, e => e.Field == "JoinedDate");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var emp = new Employee { Name = "", Email = "bad", TelNumber = "", JoinedDate = default };

        var errors = EmployeeValidator.Validate(emp);

        Assert.True(errors.Count >= 3); // Name, Email, JoinedDate
    }

    // ── Success cases ────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidEmployee_ReturnsNoErrors()
    {
        var emp = new Employee
        {
            Name = "Valid User",
            Email = "valid@example.com",
            TelNumber = "010-1234-5678",
            JoinedDate = new DateTime(2023, 6, 15)
        };

        var errors = EmployeeValidator.Validate(emp);

        Assert.Empty(errors);
    }

    // ── Batch validation ─────────────────────────────────────────────

    [Fact]
    public void ValidateBatch_MixedData_ReturnsErrorsOnlyForInvalid()
    {
        var employees = new List<Employee>
        {
            new() { Name = "Good", Email = "good@test.com", TelNumber = "010-0001", JoinedDate = DateTime.Now },
            new() { Name = "", Email = "bad", TelNumber = "010-0002", JoinedDate = default },
        };

        var errors = EmployeeValidator.ValidateBatch(employees);

        Assert.DoesNotContain(0, errors.Keys); // First is valid
        Assert.Contains(1, errors.Keys);      // Second has errors
    }
}
