# Handmade Resin Gifts API

ASP.NET Core Web API for an Arabic handmade gifts store.

## Features

- Customer registration
- Login with phone number, email, or username
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

## Local Setup

Restore packages:

```powershell
dotnet restore
```

Required local settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:ExpirationMinutes`
- `Cors:AllowedOrigins`
- `Email:Smtp:Host`
- `Email:Smtp:Port`
- `Email:Smtp:EnableSsl`
- `Email:Smtp:UserName`
- `Email:Smtp:Password`
- `Email:Smtp:FromEmail`
- `Email:Smtp:FromName`
- `Email:Smtp:FrontendBaseUrl`

For Gmail SMTP, use `smtp.gmail.com`, port `587`, SSL enabled, the full Gmail address as the username, and a Google App Password as the password.

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
