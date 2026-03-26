# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Martial Art Training Assistant ("CodeJitsu") — a fullstack web app with .NET 8.0 microservices, React/Vite frontend, PostgreSQL, and Docker orchestration.

## Build & Run Commands

```bash
# Full stack via Docker (recommended)
docker compose --env-file ./.env up -d --build

# Hybrid: start only DB, run services locally
docker compose --env-file ./.env up -d app-db
cd FighterManager.Server && dotnet run --launch-profile http   # Port 5136
cd VideoSharing.Server && dotnet run --launch-profile http     # Port 5137
cd MatchMaker.Server && dotnet run --launch-profile http       # Port 5138
cd SampleAspNetReactDockerApp.Client && npm run dev            # Port 5173

# Build all backend projects
dotnet build

# NOTE: Do NOT run tests (dotnet test / npm test) — the test suite is outdated and broken.
```

## Database Migrations

Migrations live in `SharedEntities` but require a startup project:

```bash
dotnet ef migrations add <Name> --project SharedEntities --startup-project FighterManager.Server
dotnet ef database update --project SharedEntities --startup-project FighterManager.Server
```

## Architecture

**Microservices** sharing a single PostgreSQL database via a common EF Core library:

- **FighterManager.Server** (port 5136/8081) — User profiles, fighters, training sessions, attendance, curriculum. Main service with Controllers → Domain services → Repository pattern.
- **VideoSharing.Server** (port 5137/8082) — YouTube video sharing, Google Cloud Storage uploads, Gemini Vision AI analysis. Uses Hangfire for background jobs.
- **MatchMaker.Server** (port 5138/8083) — Fighter pairing/matching logic.
- **SharedEntities** (class library) — `DatabaseContext`, domain models, EF Core migrations. All services reference this. Auth (JWT + ASP.NET Identity) is configured here.
- **SampleAspNetReactDockerApp.Client** — React 18 + Vite + TypeScript frontend served via Nginx in Docker.

**Nginx reverse proxy** routes: `/` → React client, `/api/` → backend microservices.

## Backend Patterns

- .NET 8.0 targeting `net8.0`
- Controllers → Domain services → Repositories (GenericRepository base in `FighterManager.Server/Helpers/`)
- DTOs in `Models/` folders for API contracts, mapped via AutoMapper to SharedEntities domain models
- Serilog for structured logging
- Swagger/Swashbuckle for API docs (enabled via config)
- API versioning via `Asp.Versioning`
- SignalR for real-time notifications
- Testing: xUnit + Moq + EF Core InMemory provider (currently outdated/broken — do not run)

## Frontend Patterns

- React 18 + TypeScript + Vite
- **UI**: Shadcn (Radix UI) + Tailwind CSS + Lucide React icons
- **State**: Zustand
- **Routing**: React Router DOM v6
- **i18n**: i18next — all user-facing strings must use translation keys
- **Validation**: Zod
- **Real-time**: @microsoft/signalr
- **Testing**: Jest + React Testing Library (currently outdated/broken — do not run)

## Environment Configuration

Copy `.env.example` to `.env`. Key variables: database connection string (`ASPNETCORE_APP_DB`), service ports, YouTube/GCP API keys, JWT settings, Gemini Vision config. Never commit secrets or `gcp-key.json`.

## Docker Services

| Service | Container Port | Host Port |
|---------|---------------|-----------|
| app-db (PostgreSQL) | 5432 | 5430 |
| fighter-manager | 8081 | 8081 |
| video-sharing | 8082 | 8082 |
| match-maker | 8083 | 8083 |
| app-client (Nginx) | 80 | 3000 |

## Deployment

The app runs on a single GCP VM (`thecodejitsu-app-vm`, zone `us-central1-c`, project `codejitsu`) with Nginx as reverse proxy and Let's Encrypt SSL.

**CI/CD is partial** — GitHub Actions (`.github/workflows/deploy-to-vm.yml`) only builds Docker images and pushes to GCP Artifact Registry (`us-central1-docker.pkg.dev/codejitsu/codejitsu-repo`). Actual deployment requires manual SSH into the VM.

**Manual deploy workflow:**
1. SSH: `gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-c --project=codejitsu`
2. Run the all-in-one deploy script: `bash ~/deploy-app.sh`
   - This handles: docker auth, pull, secret refresh, container restart, verification, and image cleanup
   - Use `bash` to invoke it (avoids `chmod` permission issues on the VM)

If you need to run steps individually:
1. `cd ~/app`
2. Auth Docker: `gcloud auth configure-docker us-central1-docker.pkg.dev`
3. Pull images: `sudo docker-compose pull`
4. Refresh secrets: `bash ./refresh-env-secret-manager.sh` (fetches from GCP Secret Manager into `.env` + GCS key)
5. Restart: `sudo docker-compose down --remove-orphans && sudo docker container prune -f && sudo docker-compose up -d`
6. Verify: `sudo docker ps`
7. Cleanup: `sudo docker image prune -a -f`

**Secrets** are stored in GCP Secret Manager and pulled to the VM's `.env` file via `refresh-env-secret-manager.sh`. The GCS service account key is also fetched to `./secrets/gcs-key.json`.

See `manual-vm-deployment-steps.md` for full troubleshooting guide (container crashes, connection errors, Nginx config issues, auth failures).

## Agentic Development Team

This project uses a multi-agent development workflow. Five specialized agents are defined in `.claude/agents/`:

| Agent | Model | Role |
|-------|-------|------|
| `project-manager` | opus | Task decomposition, coordination, code review, quality gates |
| `frontend-developer` | sonnet | React/TypeScript UI, components, state, i18n, styling |
| `backend-developer` | sonnet | .NET 8 APIs, services, repositories, EF Core migrations |
| `devops-engineer` | sonnet | Docker, GCP, Nginx, CI/CD, deployment |
| `qa-tester` | sonnet | Manual/automated testing, verification, bug hunting |

### How It Works

1. **Default behavior**: When you assign a task, I act as the `project-manager` — analyzing scope, breaking it down, and delegating to the right agents.
2. **Parallel execution**: Independent subtasks are assigned to agents concurrently for speed.
3. **Quality gates**: After implementation, `qa-tester` verifies and `project-manager` reviews.
4. **Memory persists**: Project context and decisions carry across sessions via the memory system.

### Invoking Specific Agents

- Natural language: "Use the backend-developer agent to add a new endpoint"
- Direct: `@backend-developer add a GET endpoint for training session stats`
- Or let the project-manager decide which agent(s) to use

### MCP Servers

Configured in `.claude/.mcp.json`:
- **GitHub** — PR workflows, issue management, code review
- **Supabase** — Database management and queries
- **PostgreSQL** (@bytebase/dbhub) — Direct database inspection

To authenticate MCP servers, use `/mcp` in a Claude Code session.

### Configuration Files

| File | Purpose |
|------|---------|
| `.claude/agents/*.md` | Agent role definitions with tools and instructions |
| `.claude/.mcp.json` | MCP server configurations |
| `.claude/settings.json` | Project-wide permissions and environment |
| `.claude/settings.local.json` | Local overrides (gitignored) |
