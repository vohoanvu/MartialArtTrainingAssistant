# AI Coding Agent Instructions for Martial Art Training Assistant

## Project Overview
This represents a fullstack web application for martial arts training ("CodeJitsu").
- **Stack**: .NET 8.0 (Backend), React + Vite (Frontend), PostgreSQL (Database), Docker (Containerization).
- **Architecture**: Microservices-style backend with a shared data layer (`SharedEntities`), orchestrated via Docker Compose and accessible through an Nginx reverse proxy.

## Architecture & Components

### Backend (ASP.NET Core 8.0)
- **Services**:
  - `FighterManager.Server`: Core logic for user profiles, fighters, and training sessions.
  - `VideoSharing.Server`: Manages YouTube video sharing and GCS integration.
  - `MatchMaker.Server`: Handles logic for pairing or matching entities.
- **Data Layer**:
  - `SharedEntities`: Contains the `DatabaseContext` (EF Core), migrations, and shared domain models (`SharedEntities/Models`).
  - **Pattern**: Services share the same Postgres DB instance and `SharedEntities` library.
  - **Authentication**: JWT/Identity stored in `SharedEntities`.

### Frontend (React + Vite)
- **Location**: `SampleAspNetReactDockerApp.Client`
- **Tech**: React, Vite, Tailwind CSS, Shadcn UI (components), Zustand (state), React Router.
- **Styling**: Utility-first CSS using Tailwind. Component library based on Radix UI + Shadcn.

### Infrastructure
- **Docker**: Root `docker-compose.yml` orchestrates all services + Nginx + Postgres.
- **Nginx**: Routes traffic. `/` -> React Client, specific paths -> Backend APIs.
- **Config**: Environment variables managed via `.env` file (see `.env.example`).

## Developer Workflows

### 1. Running the Application
**Recommended (Docker)**:
```bash
docker compose --env-file ./.env up -d --build
```
**Hybrid (Local Debugging)**:
1. Start DB: `docker compose --env-file ./.env up -d app-db`
2. Run APIs (in separate terminals):
   - `cd FighterManager.Server && dotnet run --launch-profile http` (Port 5136)
   - `cd VideoSharing.Server && dotnet run --launch-profile http` (Port 5137)
   - `cd MatchMaker.Server && dotnet run --launch-profile http` (Port 5138)
3. Run Client: `cd SampleAspNetReactDockerApp.Client && npm run dev`

### 2. Database Migrations
Migrations logic resides in `SharedEntities`, but requires an executable startup project (e.g., `FighterManager`).
**Create Migration**:
```bash
dotnet ef migrations add <Name> --project SharedEntities --startup-project FighterManager.Server
```
**Apply Migration**:
```bash
dotnet ef database update --project SharedEntities --startup-project FighterManager.Server
```

### 3. Testing
- **Backend**: `dotnet test` (from root).
- **Frontend**: `npm test` (inside `SampleAspNetReactDockerApp.Client`).

## Coding Conventions

- **Frontend**:
  - Use **Shadcn** components for UI elements.
  - State management uses **Zustand**.
  - Icons: **Lucide React**.
  - Use `i18next` for all text strings.
- **Backend**:
  - Use **DTOs** for API contracts, mapping to Entities in `SharedEntities`.
  - **Swagger** is used for API documentation (enabled in Dev/Docker via settings).
  - Use `Serilog` for logging.
- **Configuration**:
  - **Secrets**: Do NOT commit `gcp-key.json` or sensitive `.env` values. Use local `appsettings.Development.json` override.
