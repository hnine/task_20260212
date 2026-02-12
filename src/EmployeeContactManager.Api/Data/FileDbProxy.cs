using System.Text.Json;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// File-based database proxy. Persists employees as a JSON file on disk.
/// </summary>
public class FileDbProxy : IDbProxy
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<Employee> _cache;

    public FileDbProxy(string connectionString)
    {
        _filePath = string.IsNullOrWhiteSpace(connectionString)
            ? Path.Combine(AppContext.BaseDirectory, "data", "employees.db.json")
            : connectionString;

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _cache = LoadFromFile();
    }

    public IEnumerable<Employee> GetAll()
    {
        lock (_lock) { return _cache.OrderBy(e => e.Name).ToList(); }
    }

    public (IEnumerable<Employee> Items, int TotalCount) GetPaged(int page, int pageSize)
    {
        lock (_lock)
        {
            var all = _cache.OrderBy(e => e.Name).ToList();
            var items = all.Skip((page - 1) * pageSize).Take(pageSize);
            return (items, all.Count);
        }
    }

    public Employee? GetByName(string name)
    {
        lock (_lock)
        {
            return _cache.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool Exists(string name)
    {
        lock (_lock)
        {
            return _cache.Any(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void AddRange(IEnumerable<Employee> employees)
    {
        lock (_lock)
        {
            foreach (var emp in employees)
            {
                _cache.Add(emp);
            }
            SaveToFile();
        }
    }

    private List<Employee> LoadFromFile()
    {
        if (!File.Exists(_filePath))
            return new List<Employee>();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<Employee>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Employee>();
    }

    private void SaveToFile()
    {
        var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
