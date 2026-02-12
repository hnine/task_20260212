# Employee Contact Manager

A full-stack Employee Contact Manager built with **.NET 8 Web API** and **React (Vite)**.

---

## Prerequisites

| Tool                                              | Version |
| ------------------------------------------------- | ------- |
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+    |
| [Node.js](https://nodejs.org/)                    | 18+     |

---

## Project Structure

```
exam/
├── src/EmployeeContactManager.Api/
│   ├── AppLogger.cs                    # Singleton logger (no DI injection)
│   ├── CallerMethodEnricher.cs         # Auto-captures caller method name
│   ├── Controllers/                    # API endpoints + UploadRequest model
│   ├── CQRS/                           # Commands, Queries, Handlers
│   ├── Data/                           # DB proxies, parsers, EF Core context
│   ├── Domain/                         # Employee model, Validator
│   ├── Middleware/                      # Request logging, Performance monitoring
│   ├── StoredProcedures/               # Auto-generated SQL scripts
│   │   ├── MySQL/StoredProcedures.sql
│   │   └── MSSQL/StoredProcedures.sql
│   ├── Templates/                      # T4-style SQL generator (reflection-based)
│   ├── SeedData/                       # Sample CSV & JSON data
│   ├── Program.cs
│   └── appsettings.json                # All config: logging, CORS, port, DB, Swagger
├── tools/SqlGenerator/                 # Pre-build tool for auto SQL generation
├── tests/EmployeeContactManager.Tests/ # xUnit tests (43 tests)
├── frontend/                           # React + Vite
└── EmployeeContactManager.sln
```

---

## Quick Start

```bash
# 1. Run the backend
dotnet run --project src/EmployeeContactManager.Api

# 2. In another terminal, run the frontend
cd frontend && npm install && npm run dev
```

- **Backend**: http://localhost:5086 (configurable via `Server:Port` in `appsettings.json`)
- **Frontend**: http://localhost:5173
- **OpenAPI / Swagger**: http://localhost:5086/swagger

---

## OpenAPI Documentation

The API is fully documented with **OpenAPI (Swagger)** using Swashbuckle. Swagger is configurable in `appsettings.json`:

```json
{
  "Swagger": {
    "Enabled": true,
    "Title": "Employee Contact Manager API",
    "Version": "v1"
  }
}
```

- **Swagger UI**: http://localhost:5086/swagger
- **OpenAPI JSON**: http://localhost:5086/swagger/v1/swagger.json
- Set `"Enabled": false` to disable in production

---

## API Endpoints

| Method | Endpoint                           | Description                  |
| ------ | ---------------------------------- | ---------------------------- |
| `GET`  | `/api/employee?page=1&pageSize=10` | Paginated employee list      |
| `GET`  | `/api/employee/{name}`             | Employee detail by name      |
| `POST` | `/api/employee`                    | Upload CSV/JSON file or text |

### POST `/api/employee` — Upload Formats

**File upload** (multipart/form-data):

- Field: `file` — a `.csv` or `.json` file

**Text input** (multipart/form-data):

- Field: `textContent` — raw CSV or JSON text
- Field: `format` — `csv` or `json`

> **Duplicate handling**:
>
> - **Exact duplicate** (same name + email + tel number): **rejected** with an error message
> - **Name-only duplicate** (same name but different email/tel): **renamed** to `{name} 2`, `{name} 3`, etc.
> - **Batch duplicates**: exact duplicates within the same upload are also rejected

---

## Configuration (`appsettings.json`)

All settings are configurable via `appsettings.json` — no code changes needed:

```json
{
  "Server": { "Port": 5086 },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  },
  "Swagger": {
    "Enabled": true,
    "Title": "Employee Contact Manager API",
    "Version": "v1"
  },
  "Database": { "Type": "InMemory", "ConnectionString": "" }
}
```

| Setting               | Default                 | Description                  |
| --------------------- | ----------------------- | ---------------------------- |
| `Server:Port`         | `5086`                  | HTTP listen port             |
| `Cors:AllowedOrigins` | `localhost:5173, :3000` | Allowed CORS origins (array) |
| `Swagger:Enabled`     | `true`                  | Enable/disable Swagger UI    |
| `Database:Type`       | `InMemory`              | Database backend             |

---

## Database Configuration

### Supported Database Types

| Type       | ConnectionString                                                                            | Description                                                        |
| ---------- | ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `InMemory` | _(empty)_                                                                                   | In-memory dictionary (default, no persistence)                     |
| `FileDB`   | `path/to/file.json`                                                                         | JSON file on disk (optional, defaults to `data/employees.db.json`) |
| `MySQL`    | `Server=localhost;Database=employees;User=root;Password=pass;`                              | MySQL with stored procedures                                       |
| `MSSQL`    | `Server=localhost;Database=employees;User Id=sa;Password=pass;TrustServerCertificate=true;` | SQL Server with stored procedures                                  |

### Stored Procedures (MySQL / MSSQL)

When using MySQL or MSSQL, the proxy automatically creates the table and 5 stored procedures on startup by reading from external SQL files:

| Stored Procedure       | Purpose                           |
| ---------------------- | --------------------------------- |
| `sp_GetAllEmployees`   | Get all employees ordered by name |
| `sp_GetPagedEmployees` | Paginated query with total count  |
| `sp_GetEmployeeByName` | Lookup by name                    |
| `sp_EmployeeExists`    | Check if name exists              |
| `sp_InsertEmployee`    | Insert a new employee             |

SQL files are located at:

- `StoredProcedures/MySQL/StoredProcedures.sql`
- `StoredProcedures/MSSQL/StoredProcedures.sql`

### Auto-Generating SQL When Schema Changes

SQL files are **automatically regenerated** during build when `Employee.cs` changes. This is powered by:

1. **MSBuild pre-build target** in the `.csproj` that detects `Employee.cs` modifications
2. **`tools/SqlGenerator`** — a standalone tool that parses `Employee.cs` as text and generates SQL

```
Employee.cs saved → dotnet build → MSBuild detects change →
runs SqlGenerator → regenerates .sql files → build completes
```

You can also run the generator manually:

```bash
dotnet run --project tools/SqlGenerator -- src/EmployeeContactManager.Api
```

The API project also contains a reflection-based `StoredProcedureGenerator` in `Templates/` for programmatic use.

---

## Data Formats

### CSV (`yyyy.MM.dd`)

```
name, email, tel number, joined date, birth date
Alice Johnson, alice@example.com, 010-1234-5678, 2022.03.15, 1990.05.20
```

> `birth date` column is optional.

### JSON (`yyyy-MM-dd`)

```json
[
  {
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "tel": "010-1234-5678",
    "joined": "2022-03-15",
    "birthDate": "1990-05-20"
  }
]
```

> `birthDate` field is optional.

---

## Logging

Uses **Serilog** with a **singleton pattern** (`AppLogger`) — no DI injection needed. Access via `AppLogger.ForContext<T>()` or the static convenience methods.

### Log Format

Configurable via `outputTemplate` in `appsettings.json`:

```
[2026-02-12 19:22:45.476] [INF] - GET /api/employee?page=1&pageSize=5 [GetAll]
[2026-02-12 19:22:45.480] [DBG] - ⏱ GET /api/employee completed in 3ms [InvokeAsync]
[2026-02-12 19:22:45.500] [WRN] - ⚠ Slow request: POST took 600ms [InvokeAsync]
```

Format: `[Timestamp] [Level] - {Message} [CallerMethod]`

| Level       | Abbreviation | What's Logged                           |
| ----------- | ------------ | --------------------------------------- |
| Information | `INF`        | API calls, employee operations, startup |
| Debug       | `DBG`        | Performance timing, duplicate renames   |
| Warning     | `WRN`        | Validation failures, slow requests      |
| Verbose     | `VRB`        | Request headers, bodies, form data      |

### Customizing Format

Change the `outputTemplate` in `appsettings.json` → `Serilog:WriteTo:Args`:

```json
"outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] - {Message:lj} [{CallerMethod}]{NewLine}{Exception}"
```

Log files: `logs/employee-api-{date}.log` (30-day retention)

---

## Testing

```bash
dotnet test EmployeeContactManager.sln
```

**43 tests** covering:

- CSV / JSON parsers (date format validation)
- CQRS handlers (pagination, lookup, add with validation)
- **Duplicate detection** (exact duplicates rejected, name-only duplicates renamed)
- Employee data validation (required fields, email format, date format)
- API integration tests (success + failure cases)

---

## Frontend

### Features

- **Employee List** — responsive card grid with pagination
- **Employee Detail** — click a card to view full details
- **Upload** — file upload (CSV/JSON) or paste text directly

### Architecture

- **React** with Vite
- API proxy configured in `vite.config.js` → forwards `/api/*` to backend
- Components: `EmployeeList`, `EmployeeDetail`, `UploadForm`
