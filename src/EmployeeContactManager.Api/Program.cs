using Serilog;
using EmployeeContactManager.Api;
using EmployeeContactManager.Api.CQRS.Handlers;
using EmployeeContactManager.Api.Data;
using EmployeeContactManager.Api.Middleware;

// ── Serilog bootstrap ───────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("🚀 Starting Employee Contact Manager API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<CallerMethodEnricher>());

    builder.Services.AddControllers();

    // ── OpenAPI / Swagger ────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Employee Contact Manager API", Version = "v1" });
    });

    // ── CORS (allow React frontend) ─────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // ── Database Proxy (configurable via appsettings.json) ──────────
    var dbProxy = DbProxyFactory.Create(builder.Configuration);
    builder.Services.AddSingleton<IDbProxy>(dbProxy);

    // ── CQRS Handlers ───────────────────────────────────────────────
    builder.Services.AddTransient<GetAllEmployeesHandler>();
    builder.Services.AddTransient<GetEmployeeByNameHandler>();
    builder.Services.AddTransient<AddEmployeesHandler>();

    var app = builder.Build();

    // ── Seed data from CSV & JSON files ─────────────────────────────
    SeedData(app);

    // ── Middleware pipeline ─────────────────────────────────────────
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<PerformanceMonitoringMiddleware>();

    // ── Swagger (dev only) ──────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Contact Manager v1"));
    }

    app.UseCors("AllowFrontend");
    app.MapControllers();

    var dbType = builder.Configuration.GetValue<string>("Database:Type") ?? "InMemory";
    Log.Information("✔ Database provider: {DbType}", dbType);
    Log.Information("🚀 Employee Contact Manager API started");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "✖ Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ── Seed helper ─────────────────────────────────────────────────────
void SeedData(WebApplication application)
{
    var db = application.Services.GetRequiredService<IDbProxy>();

    try
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "SeedData");

        var csvPath = Path.Combine(basePath, "employees.csv");
        if (File.Exists(csvPath))
        {
            var csvContent = File.ReadAllText(csvPath);
            var csvEmployees = CsvParser.Parse(csvContent);
            db.AddRange(csvEmployees);
            Log.Information("✔ Loaded {Count} employees from CSV seed data", csvEmployees.Count);
        }

        var jsonPath = Path.Combine(basePath, "employees.json");
        if (File.Exists(jsonPath))
        {
            var jsonContent = File.ReadAllText(jsonPath);
            var jsonEmployees = JsonParser.Parse(jsonContent);
            db.AddRange(jsonEmployees);
            Log.Information("✔ Loaded {Count} employees from JSON seed data", jsonEmployees.Count);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "✖ Failed to load seed data");
    }
}

// Make Program accessible for integration tests
public partial class Program { }
