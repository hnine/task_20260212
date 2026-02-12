using EmployeeContactManager.Api.CQRS.Queries;
using EmployeeContactManager.Api.Data;

namespace EmployeeContactManager.Api.CQRS.Handlers;

public class GetAllEmployeesHandler
{
    private readonly IDbProxy _db;

    public GetAllEmployeesHandler(IDbProxy db)
    {
        _db = db;
    }

    public GetAllEmployeesResult Handle(GetAllEmployeesQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var (items, totalCount) = _db.GetPaged(page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new GetAllEmployeesResult(items, totalCount, page, pageSize, totalPages);
    }
}
