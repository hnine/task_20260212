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

    // ── Server port (configurable via appsettings.json) ─────────────
    var port = builder.Configuration.GetValue<int>("Server:Port", 5086);
    builder.WebHost.UseUrls($"http://localhost:{port}");

    // ── Serilog ──────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<CallerMethodEnricher>());

    builder.Services.AddControllers();

    // ── OpenAPI / Swagger (configurable via appsettings.json) ────────
    var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled", true);
    var swaggerTitle = builder.Configuration.GetValue<string>("Swagger:Title") ?? "Employee Contact Manager API";
    var swaggerVersion = builder.Configuration.GetValue<string>("Swagger:Version") ?? "v1";

    if (swaggerEnabled)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(swaggerVersion, new() { Title = swaggerTitle, Version = swaggerVersion });
        });
    }

    // ── CORS (configurable via appsettings.json) ─────────────────────
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:5173", "http://localhost:3000" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
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

    // ── Swagger ─────────────────────────────────────────────────────
    if (swaggerEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"{swaggerTitle} {swaggerVersion}"));
    }

    app.UseCors("AllowFrontend");
    app.MapControllers();

    var dbType = builder.Configuration.GetValue<string>("Database:Type") ?? "InMemory";
    Log.Information("✔ Database provider: {DbType}", dbType);
    Log.Information("✔ CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));
    Log.Information("✔ Swagger: {SwaggerStatus}", swaggerEnabled ? "enabled" : "disabled");
    Log.Information("🚀 Employee Contact Manager API started on port {Port}", port);

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
