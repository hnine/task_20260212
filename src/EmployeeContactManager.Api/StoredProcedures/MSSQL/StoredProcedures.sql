-- =============================================
-- MSSQL Schema and Stored Procedures for Employees table
-- Auto-generated from Employee model
-- Generated at: 2026-02-12 06:58:27 UTC
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employees' AND xtype='U')
CREATE TABLE Employees (
    Name NVARCHAR(200) NOT NULL PRIMARY KEY,
    Email NVARCHAR(200) NOT NULL,
    TelNumber NVARCHAR(50) NOT NULL,
    JoinedDate DATETIME2 NOT NULL,
    BirthDate DATETIME2 NOT NULL
);
GO

IF OBJECT_ID('sp_GetAllEmployees', 'P') IS NOT NULL DROP PROCEDURE sp_GetAllEmployees;
IF OBJECT_ID('sp_GetPagedEmployees', 'P') IS NOT NULL DROP PROCEDURE sp_GetPagedEmployees;
IF OBJECT_ID('sp_GetEmployeeByName', 'P') IS NOT NULL DROP PROCEDURE sp_GetEmployeeByName;
IF OBJECT_ID('sp_EmployeeExists', 'P') IS NOT NULL DROP PROCEDURE sp_EmployeeExists;
IF OBJECT_ID('sp_InsertEmployee', 'P') IS NOT NULL DROP PROCEDURE sp_InsertEmployee;
GO

CREATE PROCEDURE sp_GetAllEmployees
AS BEGIN SET NOCOUNT ON;
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate FROM Employees ORDER BY Name;
END
GO

CREATE PROCEDURE sp_GetPagedEmployees @Offset INT, @Limit INT
AS BEGIN SET NOCOUNT ON;
    SELECT COUNT(*) AS TotalCount FROM Employees;
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate FROM Employees ORDER BY Name
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

CREATE PROCEDURE sp_GetEmployeeByName @Name NVARCHAR(200)
AS BEGIN SET NOCOUNT ON;
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate FROM Employees WHERE Name = @Name;
END
GO

CREATE PROCEDURE sp_EmployeeExists @Name NVARCHAR(200)
AS BEGIN SET NOCOUNT ON;
    SELECT COUNT(*) AS ExistsCount FROM Employees WHERE Name = @Name;
END
GO

CREATE PROCEDURE sp_InsertEmployee @Name NVARCHAR(200), @Email NVARCHAR(200), @TelNumber NVARCHAR(50), @JoinedDate DATETIME2, @BirthDate DATETIME2
AS BEGIN SET NOCOUNT ON;
    INSERT INTO Employees (Name, Email, TelNumber, JoinedDate, BirthDate) VALUES (@Name, @Email, @TelNumber, @JoinedDate, @BirthDate);
END
GO
