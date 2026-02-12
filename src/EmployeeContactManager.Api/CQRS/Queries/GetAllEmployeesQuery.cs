using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.CQRS.Queries;

public record GetAllEmployeesQuery(int Page, int PageSize);

public record GetAllEmployeesResult(
    IEnumerable<Employee> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
