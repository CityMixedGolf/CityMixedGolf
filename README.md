# City Mixed Golf

ASP.NET Core 8.0 web application for the City of Newcastle Golf Club Mixed Section.

## Features
- Public homepage: latest results, order of merit, upcoming competitions
- Member dashboard: sign-up, amend/cancel entries, partner preference, competition history
- Admin panel: competition management, auto draw algorithm, manual overrides, singles pool
- Email notifications via SendGrid (WhatsApp via Twilio — Phase 2)

## Tech stack
- ASP.NET Core 8.0 MVC with Areas (Public / Member / Admin)
- Entity Framework Core 8 + Azure SQL Server
- ASP.NET Identity for authentication
- SendGrid for transactional email
- Bootstrap 5 + custom golf theme CSS

## Getting started

### Prerequisites
- .NET 8 SDK
- SQL Server (local or Azure)
- SendGrid account (for email notifications)

### Setup
1. Clone the repo
2. Update `appsettings.json` with your SQL Server connection string
3. Add your SendGrid API key to user secrets:
   ```
   dotnet user-secrets set "SendGrid:ApiKey" "YOUR_KEY" --project CityMixedGolf.Web
   ```
4. Run migrations:
   ```
   dotnet ef database update --project CityMixedGolf.Web
   ```
5. Run the app:
   ```
   dotnet run --project CityMixedGolf.Web
   ```

### Seeding an admin user
After first run, use the ASP.NET Identity scaffolded registration, then update the `IsAdmin` flag and add the user to the `Admin` role directly in the database, or add a seed method to `Program.cs`.

## Project structure
```
CityMixedGolf.Web/
├── Areas/
│   ├── Admin/         Controllers + Views for admin functions
│   ├── Member/        Controllers + Views for logged-in members
│   └── Public/        Controllers + Views for public pages
├── Data/              EF Core DbContext
├── Models/            Entity models + enums
├── Services/          DrawService, NotificationService
└── wwwroot/css/       golf-theme.css
```