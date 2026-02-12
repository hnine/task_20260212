using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// Unified database proxy interface for all storage backends.
/// Implementations: InMemoryDbProxy, FileDbProxy, (MySQL/MSSQL stubs).
/// </summary>
public interface IDbProxy
{
    IEnumerable<Employee> GetAll();
    (IEnumerable<Employee> Items, int TotalCount) GetPaged(int page, int pageSize);
    Employee? GetByName(string name);
    bool Exists(string name);
    void AddRange(IEnumerable<Employee> employees);
}
