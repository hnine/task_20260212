using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.CQRS.Queries;

public record GetEmployeeByNameQuery(string Name);
