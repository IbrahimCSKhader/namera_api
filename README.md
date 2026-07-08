# Namira API

ASP.NET Core Web API foundation for Namira authentication and customer profile.

## Run

```powershell
dotnet restore
dotnet run --launch-profile http
```

## Database

Create `appsettings.Development.json` locally, then set the SQL Server connection string and JWT secret. Configuration files are intentionally ignored by Git.

```powershell
dotnet ef migrations add InitialIdentityAuth
dotnet ef database update
```
