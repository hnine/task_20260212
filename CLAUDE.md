# Project: Employees Contact list manager

## Code styles

- Using .NET 8.0
- Using DB
    - Select DB types from configuration
    - File DB, MySQL, MSSQL
    - Create DB Proxy for using same interface

## Backend

### Requires

- Use .NET 8
- Using CQRS pattern
- Load data from CVS and Json files
- CVS file format
    name, email, tel number, joined date(date format yyyy.MM.dd)
- Json file format
    name, email, tel, joined (date format yyyy-MM-dd)
- API Endpoints
    GET /api/employee?page={page}&pageSize={pageSize}
        Response (200 OK, application/json)
        return all employees data list (using page and page size)
    GET /api/employee/{name}
        Response (200 OK, application/json)
        requested name, information defails
    POST /api/employee
        Response (201 Created)
        upload input file (csv, json)
        input text from text area
        frontend send context to backend when using <input type="file"> csv, json file upload
        frontend send context to backend when using <textarea> text </textarea> input text formatted csv, json is working same as file upload
- Create test code for api tests
    More details on TDD Workflow
- Create log system
    Using Serilog
    Log schema
        API call histories
        System initialize and fail point
        Performance monitoring
- API Spec (using OpenAPI)
- Employee data storage
    Using database (MySQL, MSSQL, File DB, InMemory DB)
    Using DB Proxy for using same interface
    Check Connectstring is valid
    Choose Database type from configuration (appsettings.json)
        Database type, Connection string
    MySQL and MSSQL using stored procedure
        Create stored procedure for using same interfaces
    When Employee data Scheme(Employee.cs file) is updated(saved), generate new sql files and update table and stored procedure
        Using Entity Framework Core
        Using T4 template
        Update StoredProcedures/MSSQL/StoredProcedures.sql, StoredProcedures/MySQL/StoredProcedures.sql files
    Table scheme and stored procedure managing from external files (.sql)
        EnsureSchemaExists method using generated files

#### TDD Workflow
- 1. Failure case
    API Call parameter check
    Add new employee data file (CSV, json) format check
    Check new employee data (invalid data, not nullable data, email, date format)
- 2. Success case
    Employee data list request (using mock data)
    Add new employee data (using mock data CSV and json)

## Frontend

### Requires

- Using React
- View employees list
    Employee infomation using card UI
- Add new employee
    Upload file (CSV, json)
    Input text from text area