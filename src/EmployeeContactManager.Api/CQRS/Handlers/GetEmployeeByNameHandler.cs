using EmployeeContactManager.Api.CQRS.Queries;
using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.CQRS.Handlers;

public class GetEmployeeByNameHandler
{
    private readonly IDbProxy _db;

    public GetEmployeeByNameHandler(IDbProxy db)
    {
        _db = db;
    }

    public Employee? Handle(GetEmployeeByNameQuery query)
    {
        return _db.GetByName(query.Name);
    }
}
