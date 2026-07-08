# Namira API

Backend foundation for the Namira project.

This project is an ASP.NET Core Web API prepared for:

- Customer registration
- Phone-number login
- JWT authentication
- Current user endpoint
- Customer profile read/update
- Owner and Customer roles

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication

## Project Structure

The project is organized by topic inside each main folder:

- `Controllers/Authentication`
- `Controllers/Customer`
- `Services/Authentication`
- `Services/Customer`
- `Services/Token`
- `DTOs/Authentication`
- `DTOs/Customer`
- `Models/Identity`
- `Data/Seed`
- `Extensions/ServiceCollection`

## Local Setup

Restore packages:

```powershell
dotnet restore
```

Create your local configuration files as needed. Sensitive configuration files are intentionally ignored by Git.

Required local settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:ExpirationMinutes`
- `Cors:AllowedOrigins`

Run the API:

```powershell
dotnet run --launch-profile http
```

## Database Migrations

Create and apply migrations locally:

```powershell
dotnet ef migrations add InitialIdentityAuth
dotnet ef database update
```

## Security Notes

Do not commit real database credentials, JWT secrets, certificates, private keys, or local environment files.

Ignored local files include:

- `Program.cs`
- `appsettings*.json`
- `.env`
- `.env.*`
- `*.pfx`
- `*.pem`
- `*.key`
- `*.crt`
- `*.cer`
