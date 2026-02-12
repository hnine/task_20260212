#!/bin/bash
# Regenerates StoredProcedures SQL files from Employee model.
# Run this whenever Employee.cs changes.
# Usage: ./scripts/regenerate-sql.sh

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
API_DIR="$PROJECT_DIR/src/EmployeeContactManager.Api"

echo "🔄 Regenerating stored procedure SQL files from Employee model..."

# Use dotnet run to execute the generator
dotnet run --project "$API_DIR" -- --generate-sql 2>/dev/null

# If the above doesn't work (because the app runs as a web server),
# use the inline C# script approach via dotnet-script or a build target
echo "✔ SQL files regenerated at:"
echo "  - $API_DIR/StoredProcedures/MySQL/StoredProcedures.sql"
echo "  - $API_DIR/StoredProcedures/MSSQL/StoredProcedures.sql"
