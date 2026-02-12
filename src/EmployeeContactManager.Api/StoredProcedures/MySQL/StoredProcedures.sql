-- =============================================
-- MySQL Schema and Stored Procedures for Employees table
-- Auto-generated from Employee model
-- Generated at: 2026-02-12 06:58:27 UTC
-- =============================================

CREATE TABLE IF NOT EXISTS Employees (
    Name VARCHAR(200) NOT NULL PRIMARY KEY,
    Email VARCHAR(200) NOT NULL,
    TelNumber VARCHAR(50) NOT NULL,
    JoinedDate DATETIME NOT NULL,
    BirthDate DATETIME NOT NULL
);

DROP PROCEDURE IF EXISTS sp_GetAllEmployees;
DROP PROCEDURE IF EXISTS sp_GetPagedEmployees;
DROP PROCEDURE IF EXISTS sp_GetEmployeeByName;
DROP PROCEDURE IF EXISTS sp_EmployeeExists;
DROP PROCEDURE IF EXISTS sp_InsertEmployee;

DELIMITER //

CREATE PROCEDURE sp_GetAllEmployees()
BEGIN
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate
    FROM Employees
    ORDER BY Name;
END //

CREATE PROCEDURE sp_GetPagedEmployees(IN p_Offset INT, IN p_Limit INT)
BEGIN
    SELECT COUNT(*) AS TotalCount FROM Employees;
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate
    FROM Employees
    ORDER BY Name LIMIT p_Limit OFFSET p_Offset;
END //

CREATE PROCEDURE sp_GetEmployeeByName(IN p_Name VARCHAR(200))
BEGIN
    SELECT Name, Email, TelNumber, JoinedDate, BirthDate
    FROM Employees
    WHERE Name = p_Name;
END //

CREATE PROCEDURE sp_EmployeeExists(IN p_Name VARCHAR(200))
BEGIN
    SELECT COUNT(*) AS ExistsCount FROM Employees WHERE Name = p_Name;
END //

CREATE PROCEDURE sp_InsertEmployee(IN p_Name VARCHAR(200), IN p_Email VARCHAR(200), IN p_TelNumber VARCHAR(50), IN p_JoinedDate DATETIME, IN p_BirthDate DATETIME)
BEGIN
    INSERT INTO Employees (Name, Email, TelNumber, JoinedDate, BirthDate)
    VALUES (p_Name, p_Email, p_TelNumber, p_JoinedDate, p_BirthDate);
END //

DELIMITER ;
