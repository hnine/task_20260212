using System.Collections.Concurrent;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// In-memory database proxy using ConcurrentDictionary.
/// </summary>
public class InMemoryDbProxy : IDbProxy
{
    private readonly ConcurrentDictionary<string, Employee> _employees = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Employee> GetAll()
    {
        return _employees.Values.OrderBy(e => e.Name);
    }

    public (IEnumerable<Employee> Items, int TotalCount) GetPaged(int page, int pageSize)
    {
        var all = _employees.Values.OrderBy(e => e.Name).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize);
        return (items, all.Count);
    }

    public Employee? GetByName(string name)
    {
        _employees.TryGetValue(name, out var employee);
        return employee;
    }

    public bool Exists(string name)
    {
        return _employees.ContainsKey(name);
    }

    public void AddRange(IEnumerable<Employee> employees)
    {
        foreach (var emp in employees)
        {
            _employees.TryAdd(emp.Name, emp);
        }
    }
}
