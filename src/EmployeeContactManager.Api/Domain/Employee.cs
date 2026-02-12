namespace EmployeeContactManager.Api.Domain;

public class Employee
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TelNumber { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }
    public DateTime BirthDate { get; set;}
}
