# Shared Database Setup

The database itself is not shared through Git. Each teammate should create a local database from the same Entity Framework Core migrations. This produces the same tables and columns for everyone.

## Requirements

- .NET 10 SDK
- SQL Server LocalDB (`MSSQLLocalDB`) or another SQL Server instance
- Repository restored with `dotnet restore`

## Create or update the local database

From the repository root, run:

```powershell
dotnet restore .\TravelPlanningSystem\TravelPlanningSystem.csproj
dotnet ef database update --project .\TravelPlanningSystem\TravelPlanningSystem.csproj --startup-project .\TravelPlanningSystem\TravelPlanningSystem.csproj
```

The default connection string creates or updates this database on the local machine:

```text
TravelPlanningSystemDb
```

The connection string is in `TravelPlanningSystem/appsettings.json`. Do not commit personal connection strings, passwords, or database files.

## Verify the schema

List all migrations:

```powershell
dotnet ef migrations list --project .\TravelPlanningSystem\TravelPlanningSystem.csproj --startup-project .\TravelPlanningSystem\TravelPlanningSystem.csproj
```

The latest migration currently includes the staff phone-number column:

```text
20260913110453_AddPhoneNumberToStaffUsers
```

If a database is out of sync, run `dotnet ef database update` again. The application also contains startup compatibility checks for selected missing columns.

## Alternative: run the SQL schema script

For a new SQL Server database, open `TravelPlanningSystem/DatabaseSchema.sql` in SQL Server Management Studio or Azure Data Studio and execute it against the intended database. The script is generated from the EF migrations and is idempotent.

EF migrations remain the source of truth. When a model changes, create and commit a new migration rather than editing the SQL script manually:

```powershell
dotnet ef migrations add DescribeYourChange --project .\TravelPlanningSystem\TravelPlanningSystem.csproj --startup-project .\TravelPlanningSystem\TravelPlanningSystem.csproj
dotnet ef database update --project .\TravelPlanningSystem\TravelPlanningSystem.csproj --startup-project .\TravelPlanningSystem\TravelPlanningSystem.csproj
```

## Important: local versus shared database

The current LocalDB connection is local to each Windows computer. Your teammates will have their own `TravelPlanningSystemDb` database and will not automatically see your data. They will have the same table structure after applying the migrations.

If the team must use the same data, configure a shared SQL Server database and put the connection string in user secrets or environment-specific configuration instead of committing it to `appsettings.json`.
