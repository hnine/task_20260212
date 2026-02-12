using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.CQRS.Commands;

public record AddEmployeesCommand(List<Employee> Employees);

public record AddEmployeesResult(int AddedCount, List<string> ValidationErrors);
