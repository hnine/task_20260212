using EmployeeContactManager.Api.CQRS.Commands;
using EmployeeContactManager.Api.CQRS.Handlers;
using EmployeeContactManager.Api.CQRS.Queries;
using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Tests;

public class HandlerTests
{
    private readonly IDbProxy _db;
    private readonly GetAllEmployeesHandler _getAllHandler;
    private readonly GetEmployeeByNameHandler _getByNameHandler;
    private readonly AddEmployeesHandler _addHandler;

    public HandlerTests()
    {
        _db = new InMemoryDbProxy();
        _getAllHandler = new GetAllEmployeesHandler(_db);
        _getByNameHandler = new GetEmployeeByNameHandler(_db);
        _addHandler = new AddEmployeesHandler(_db);

        // Seed test data
        _db.AddRange(new[]
        {
            new Employee { Name = "Alice", Email = "alice@test.com", TelNumber = "010-0001", JoinedDate = new DateTime(2022, 1, 1) },
            new Employee { Name = "Bob", Email = "bob@test.com", TelNumber = "010-0002", JoinedDate = new DateTime(2022, 2, 1) },
            new Employee { Name = "Charlie", Email = "charlie@test.com", TelNumber = "010-0003", JoinedDate = new DateTime(2022, 3, 1) },
        });
    }

    [Fact]
    public void GetAll_WithPagination_ReturnsCorrectPage()
    {
        var result = _getAllHandler.Handle(new GetAllEmployeesQuery(1, 2));
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void GetAll_SecondPage_ReturnsRemainingItems()
    {
        var result = _getAllHandler.Handle(new GetAllEmployeesQuery(2, 2));
        Assert.Single(result.Items);
        Assert.Equal("Charlie", result.Items.First().Name);
    }

    [Fact]
    public void GetAll_InvalidPage_DefaultsToPageOne()
    {
        var result = _getAllHandler.Handle(new GetAllEmployeesQuery(0, 10));
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public void GetByName_ExistingEmployee_ReturnsEmployee()
    {
        var employee = _getByNameHandler.Handle(new GetEmployeeByNameQuery("Alice"));
        Assert.NotNull(employee);
        Assert.Equal("alice@test.com", employee.Email);
    }

    [Fact]
    public void GetByName_CaseInsensitive_ReturnsEmployee()
    {
        var employee = _getByNameHandler.Handle(new GetEmployeeByNameQuery("alice"));
        Assert.NotNull(employee);
        Assert.Equal("Alice", employee.Name);
    }

    [Fact]
    public void GetByName_NonExistent_ReturnsNull()
    {
        var employee = _getByNameHandler.Handle(new GetEmployeeByNameQuery("Nobody"));
        Assert.Null(employee);
    }

    [Fact]
    public void AddEmployees_ValidData_AddsToRepository()
    {
        var newEmployees = new List<Employee>
        {
            new() { Name = "Diana", Email = "diana@test.com", TelNumber = "010-0004", JoinedDate = DateTime.Now }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(newEmployees));
        Assert.Equal(1, result.AddedCount);
        Assert.Empty(result.ValidationErrors);
        Assert.NotNull(_getByNameHandler.Handle(new GetEmployeeByNameQuery("Diana")));
    }

    [Fact]
    public void AddEmployees_InvalidData_ReturnsValidationErrors()
    {
        var bad = new List<Employee>
        {
            new() { Name = "", Email = "bad-email", TelNumber = "010-000", JoinedDate = default }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(bad));
        Assert.Equal(0, result.AddedCount);
        Assert.NotEmpty(result.ValidationErrors);
    }

    [Fact]
    public void AddEmployees_MissingEmail_ReturnsValidationError()
    {
        var bad = new List<Employee>
        {
            new() { Name = "Test", Email = "", TelNumber = "010-000", JoinedDate = new DateTime(2024, 1, 1) }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(bad));
        Assert.Equal(0, result.AddedCount);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Email"));
    }

    [Fact]
    public void AddEmployees_ExactDuplicate_ReturnsError()
    {
        // Alice already exists with same name, email, and tel — should be rejected
        var duplicate = new List<Employee>
        {
            new() { Name = "Alice", Email = "alice@test.com", TelNumber = "010-0001", JoinedDate = DateTime.Now }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(duplicate));
        Assert.Equal(0, result.AddedCount);
        Assert.NotEmpty(result.ValidationErrors);
        Assert.Contains(result.ValidationErrors, e => e.Contains("already exists"));
    }

    [Fact]
    public void AddEmployees_SameNameDifferentData_RenamesSuccessfully()
    {
        // Same name but different email/tel — should be renamed, not rejected
        var sameNameDifferentData = new List<Employee>
        {
            new() { Name = "Alice", Email = "newalice@test.com", TelNumber = "010-9999", JoinedDate = DateTime.Now }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(sameNameDifferentData));
        Assert.Equal(1, result.AddedCount);
        Assert.Empty(result.ValidationErrors);
        // Should have been renamed to "Alice 2"
        Assert.NotNull(_getByNameHandler.Handle(new GetEmployeeByNameQuery("Alice 2")));
    }

    [Fact]
    public void AddEmployees_BatchExactDuplicates_ReturnsError()
    {
        // Two identical employees in the same batch — should be rejected
        var batchDuplicates = new List<Employee>
        {
            new() { Name = "NewPerson", Email = "new@test.com", TelNumber = "010-5555", JoinedDate = DateTime.Now },
            new() { Name = "NewPerson", Email = "new@test.com", TelNumber = "010-5555", JoinedDate = DateTime.Now }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(batchDuplicates));
        Assert.Equal(0, result.AddedCount);
        Assert.NotEmpty(result.ValidationErrors);
        Assert.Contains(result.ValidationErrors, e => e.Contains("multiple times"));
    }

    [Fact]
    public void AddEmployees_BatchSameNameDifferentData_BothAdded()
    {
        // Two employees with same name but different data in same batch — both added with rename
        var batch = new List<Employee>
        {
            new() { Name = "Zara", Email = "zara1@test.com", TelNumber = "010-1111", JoinedDate = DateTime.Now },
            new() { Name = "Zara", Email = "zara2@test.com", TelNumber = "010-2222", JoinedDate = DateTime.Now }
        };
        var result = _addHandler.Handle(new AddEmployeesCommand(batch));
        Assert.Equal(2, result.AddedCount);
        Assert.Empty(result.ValidationErrors);
        Assert.NotNull(_getByNameHandler.Handle(new GetEmployeeByNameQuery("Zara")));
        Assert.NotNull(_getByNameHandler.Handle(new GetEmployeeByNameQuery("Zara 2")));
    }
}
