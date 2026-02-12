using Microsoft.EntityFrameworkCore;
using EmployeeContactManager.Api.Domain;

namespace EmployeeContactManager.Api.Data;

/// <summary>
/// EF Core DbContext for schema management (migrations).
/// CRUD operations use stored procedures via IDbProxy implementations.
/// </summary>
public class EmployeeDbContext : DbContext
{
    public DbSet<Employee> Employees { get; set; } = null!;

    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Name);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TelNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.JoinedDate).IsRequired();
            entity.Property(e => e.BirthDate).IsRequired();
        });
    }
}
