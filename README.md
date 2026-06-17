# Shop

Full-stack e-commerce application built with **.NET 8** (Clean Architecture) and **Angular 20** (standalone components) + **Tailwind CSS** + **Angular Material**.

---

## Architecture

```
Shop.sln
├── API/              # ASP.NET Core Web API (entry point)
├── Core/             # Domain entities and business logic
└── Infrastructure/   # Data access (EF Core), Redis, Stripe, Identity
    └── Data/
        └── SeedData/ # Database seed data (preserved on build)
```

The Angular client lives in `client/` and builds into `API/wwwroot/` for production serving.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (see `client/package.json` for version, requires Angular CLI 20+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server and Redis)
- Angular CLI (install globally):

```bash
npm install -g @angular/cli
```

---

## How to Debug

### 1. Start the database infrastructure

```bash
docker compose up -d
```

Starts:
- **SQL Server 2022** on port `1433` (SA password: `Password@1`)
- **Redis** on port `6379`

To stop and remove volumes (also deletes data):

```bash
docker compose down -v
```

> `-v` also deletes the volumes.

### 2. Run the .NET backend

```bash
dotnet run --project API
```

By default, the API starts on `http://localhost:5284` (or the ports configured in `Properties/launchSettings.json`).
Swagger is available at the HTTP URL.

> ⚠️ **Port matching**: The Angular client is configured in `client/src/environments/environment.development.ts` to call the API at:
> ```
> baseUrl: 'https://localhost:44304/api/'
> ```
> This port (`44304`) matches the **IIS Express SSL port** from `API/Properties/launchSettings.json`. If you run the API via `dotnet run` (or using a different profile), the port will differ and the Angular client won't reach the backend.
>
> To fix this, either:
> - Run the API via **IIS Express** (in Visual Studio) so it uses port `44304`, or
> - Update `baseUrl` and `hubUrl` in `environment.development.ts` to match the port your API is actually running on (e.g. `https://localhost:7085/api/` for the "https" profile).

### 3. Run the Angular client

The project uses SSL for local development. First-time setup may require trusting the dev certificate:

```bash
cd client
npm install
ng serve
```

Opens at `https://localhost:4200/` (SSL enabled via `ssl/localhost.pem`).
The app automatically reloads on file changes.

---

## .NET Migrations (EF Core)

The `API` project is the startup project (`-s API`), and the `Infrastructure` project contains the `DbContext` (`-p Infrastructure`).

### Create a new migration

```bash
dotnet ef migrations add <MigrationName> -s API -p Infrastructure
```

### Remove the last migration

```bash
dotnet ef migrations remove -s API -p Infrastructure
```

### Apply migrations to the database

```bash
dotnet ef database update -s API -p Infrastructure
```

### Apply to a specific migration

```bash
dotnet ef database update <MigrationName> -s API -p Infrastructure
```

### View pending migrations

```bash
dotnet ef migrations list -s API -p Infrastructure
```

> All migration commands must be run from the solution root (where `Shop.sln` is located).

---

## Configuration

### Connection Strings (`API/appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=shop;User Id=SA;Password=Password@1;TrustServerCertificate=True",
    "Redis": "localhost"
  }
}
```

### Stripe

Stripe keys are configured in `appsettings.Development.json`:
- `MockStripe: true` — uses a mock Stripe client for local development (no real keys needed)

For production, set the actual keys via User Secrets:

```bash
dotnet user-secrets set "StripeSettings:SecretKey" "sk_live_..." --project API
dotnet user-secrets set "StripeSettings:PublishableKey" "pk_live_..." --project API
dotnet user-secrets set "StripeSettings:whSecret" "whsec_..." --project API
```

> The project's UserSecretsId is `29fd337c-45c1-4b20-9e44-d4501670aa5e`.

### Angular Environment

Development environment file: `client/src/environments/environment.development.ts`

---

## Client Scripts (`client/package.json`)

| Command | Description |
|---------|-------------|
| `ng serve` | Start development server (SSL, hot reload) |
| `ng build` | Production build (output goes to `API/wwwroot/`) |
| `ng test` | Run unit tests (Karma) |

---

## Project Structure

```
shop/
├── API/                      # ASP.NET Core API
│   ├── Controllers/          # API endpoints
│   ├── Extensions/           # Service registration extensions
│   ├── Middleware/            # Exception handling, etc.
│   ├── RequestHandlers/      # Mediator-like request handlers
│   ├── SignalR/              # Real-time hubs
│   ├── DTOs/                 # Data transfer objects
│   ├── Errors/               # API error response models
│   ├── Program.cs            # Entry point
│   └── appsettings*.json     # Configuration
├── Core/                     # Domain layer
├── Infrastructure/           # Data & services
│   ├── Config/               # Entity configuration
│   ├── Data/                 # DbContext, migrations, seed data
│   └── Services/             # Stripe, Redis, etc.
├── client/                   # Angular 20 frontend
│   ├── src/
│   │   └── app/
│   │       ├── core/         # Services, guards, interceptors
│   │       ├── features/     # Feature modules (lazy loaded)
│   │       └── shared/       # Shared components, models
│   └── angular.json
├── docker-compose.yml        # SQL Server + Redis
└── Shop.sln                  # Visual Studio solution
```

---

## Built With

| Layer | Technology |
|-------|-----------|
| Backend | .NET 8, ASP.NET Core, EF Core 8, SignalR |
| Identity | ASP.NET Core Identity (cookie-based auth) |
| Database | SQL Server 2022 (Docker) |
| Cache | Redis (Docker) |
| Payments | Stripe (mock mode in dev) |
| Frontend | Angular 20, Angular Material, Tailwind CSS 4 |
| Real-time | SignalR (`@microsoft/signalr`) |
