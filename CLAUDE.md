# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Martial Art Training Assistant ("CodeJitsu") â€” a fullstack web app with .NET 10 microservices, React 19/Vite 7 frontend, Supabase PostgreSQL, and Docker orchestration.

## Build & Run Commands

```bash
# Full stack via Docker â€” mimics production VM environment (recommended)
docker compose --env-file ./.env up -d --build

# This starts all 4 containers: fighter-manager, video-analysis, match-maker, app-client (Nginx)
# Access the app at http://localhost:3000
# Nginx proxies: /api â†’ fighter-manager, /vid/api â†’ video-analysis, /pair/api â†’ match-maker

# Alternative: run .NET services and React client locally (without Docker)
cd FighterManager.Server && dotnet run --launch-profile http   # Port 5136
cd VideoAnalysis.Server && dotnet run --launch-profile http     # Port 5137
cd MatchMaker.Server && dotnet run --launch-profile http       # Port 5138
cd CodeJitsu.Client && npm run dev            # Port 5173

# Build all backend projects
dotnet build

# Run tests
dotnet test                                                    # Backend: 141 tests (135 pass, 6 skipped)
cd CodeJitsu.Client && npx vitest run         # Frontend: 99 tests
```

## Database

**No local PostgreSQL** â€” both local development and production connect to **Supabase** (hosted PostgreSQL). The connection string in `.env` (`ASPNETCORE_APP_DB`) must use the Supabase **session pooler** (port 5432) with `No Reset On Close=true`. The transaction pooler (port 6543) times out from Docker networking.

Migrations live in `SharedEntities` but require a startup project:

```bash
dotnet ef migrations add <Name> --project SharedEntities --startup-project FighterManager.Server
dotnet ef database update --project SharedEntities --startup-project FighterManager.Server
```

## Architecture

**Microservices** sharing a single Supabase PostgreSQL database via a common EF Core library:

- **FighterManager.Server** (port 5136/8081) â€” User profiles, fighters, training sessions, attendance, curriculum. Main service with Controllers â†’ Domain services â†’ Repository pattern.
- **VideoAnalysis.Server** (port 5137/8082) â€” YouTube video sharing & search, Google Cloud Storage uploads, Gemini Vision AI analysis. Uses Hangfire for background jobs.
- **MatchMaker.Server** (port 5138/8083) â€” Fighter pairing/matching logic.
- **SharedEntities** (class library) â€” `DatabaseContext`, domain models, EF Core migrations. All services reference this. Auth (JWT + ASP.NET Identity) is configured here.
- **CodeJitsu.Client** â€” React 19 + Vite 7 + TypeScript frontend served via Nginx in Docker.

**Nginx reverse proxy** routes: `/` â†’ React client, `/api/` â†’ fighter-manager, `/vid/api/` â†’ video-analysis, `/pair/api/` â†’ match-maker.

## Backend Patterns

- .NET 10.0 targeting `net10.0`
- Controllers â†’ Domain services â†’ Repositories (GenericRepository base in `FighterManager.Server/Helpers/`)
- DTOs in `Models/` folders for API contracts, mapped via AutoMapper 16 to SharedEntities domain models
- Serilog for structured logging
- Swagger/Swashbuckle v10 for API docs (enabled via config)
- API versioning via `Asp.Versioning`
- SignalR 10 for real-time notifications
- YouTube Data API v3 for video search (replaced Grok live search)
- Testing: xUnit + Moq + EF Core InMemory provider

## Frontend Patterns

- React 19 + TypeScript + Vite 7
- **UI**: Shadcn (Radix UI) + Tailwind CSS + Lucide React icons
- **State**: Zustand 5
- **Routing**: React Router DOM v6
- **i18n**: i18next â€” all user-facing strings must use translation keys
- **Validation**: Zod
- **Real-time**: @microsoft/signalr 10
- **Testing**: Vitest + React Testing Library + MSW (Mock Service Worker)

## Environment Configuration

Copy `.env.example` to `.env`. Key variables: database connection string (`ASPNETCORE_APP_DB`), service ports, YouTube/GCP API keys, JWT settings, Gemini Vision config. Never commit secrets or `gcp-key.json`.

The GCS service account key must be placed at `./secrets/gcs-key.json` for the video-analysis container volume mount.

## Docker Services

There is **no local PostgreSQL container** â€” all services connect directly to Supabase.

| Service | Container Port | Host Port |
|---------|---------------|-----------|
| fighter-manager | 8081 | 8081 |
| video-analysis | 8082 | 8082 |
| match-maker | 8083 | 8083 |
| app-client (Nginx) | 80 | 3000 |

The `app-client` container uses `default.local.conf` (HTTP-only, no SSL) for local testing. Production uses `default.conf` with Let's Encrypt SSL.

## Deployment

The app runs on a single GCP VM (`thecodejitsu-app-vm`, zone `us-central1-b`, project `project-afa815fe-26c6-40c3-a8b` / "MyCoach") with Nginx as reverse proxy. The site stays on `thecodejitsu.com`, fronted by Cloudflare (proxied, **Full (Strict)** SSL) terminating at a **Cloudflare Origin CA cert** on the VM (15-yr, valid to 2041). The VM authenticates to GCS + Vertex AI **keyless** via its attached service account `codejitsu-vm-runtime` (no mounted SA key — MyCoach enforces `constraints/iam.disableServiceAccountKeyCreation`). Migrated from the personal `codejitsu` project in June 2026; see `MIGRATION-PLAN-gcp-codejitsu-to-mycoach.md`.

### Current infrastructure (post-migration, June 2026)

- **Project:** `project-afa815fe-26c6-40c3-a8b` ("MyCoach"), company-billed. Region `us-central1`, zone `us-central1-b`.
- **VM:** `thecodejitsu-app-vm` (**e2-small**, 2 GB RAM, Ubuntu 24.04), static IP **35.232.12.173** (regional — can attach in any `us-central1` zone), app dir **`/home/vohoanvu/app`**.
  - **Resized e2-medium → e2-small 2026-08-16** for cost (~$12/mo vs ~$24.5/mo). A 2 GB swap file (`/swapfile`, fstab-persisted, swappiness 10) absorbs .NET spikes, and the compose services carry `mem_limit`s (fighter-manager 800m, video-analysis 1100m, app-client 128m). If video-analysis struggles under heavy Vertex pipeline load, bump its limit or resize back (stop → `set-machine-type` → start, same IP).
  - **Zone is `us-central1-b`** (was `us-central1-c`; moved 2026-07-01 after a region-wide `ZONE_RESOURCE_POOL_EXHAUSTED` stockout — recovery playbook in `HANDOFF.md`). Recovery point: global machine image `codejitsu-vm-recovery-20260816` (the July-era image + orphaned `-c` boot disk are deleted).
  - **Containers self-heal**: all compose services have `restart: unless-stopped` (verified after a VM stop/start 2026-08-16) — no manual `docker-compose up -d` needed after reboots.
- **Keyless auth:** the VM's attached SA `codejitsu-vm-runtime` is the app's ADC identity (Secret Manager accessor [per-secret], Artifact Registry reader, `aiplatform.user`, Storage `objectAdmin` on the bucket, and Token-Creator-on-self for V4 signed-URL `signBlob`). **No SA key files** — `GoogleCloudStorageService`/`GeminiVisionService` fall back to ADC.
- **docker-compose:** v2 standalone at `/usr/local/bin/docker-compose`. **Run it WITHOUT `sudo`** (the user is in the `docker` group; the gcloud credential helper is a snap at `/snap/bin` that `sudo`'s secure_path can't see — `sudo docker-compose pull` fails AR auth).
- **TLS:** Cloudflare proxied + Full (Strict); Origin CA cert at `~/app/letsencrypt/config/live/thecodejitsu.com/` (private key generated on-VM, never left it). DNS is at **Cloudflare** (not Cloud DNS); cutover = flip the origin A record there.
- **Secrets:** GCP Secret Manager in MyCoach, scoped per-secret to the VM SA. Refresh with `bash ~/app/refresh-env-secret-manager.sh` (keyless; no GCS key fetch).
- **CI/CD:** WIF pool `github-pool` / provider `codejitsu-provider` + SA `codejitsu-cicd-deployer`; GitHub secrets `GCP_PROJECT_ID`, `GCP_SERVICE_ACCOUNT`, `GCP_WORKLOAD_IDENTITY_PROVIDER`.
- See `HANDOFF.md` for outstanding work (old-project decommission, Vertex pipeline refactor).

**CI/CD is partial** â€” GitHub Actions (`.github/workflows/deploy-to-vm.yml`) only builds Docker images and pushes to GCP Artifact Registry (`us-central1-docker.pkg.dev/project-afa815fe-26c6-40c3-a8b/codejitsu-repo`). Actual deployment requires manual SSH into the VM.

**Manual deploy workflow:**
1. SSH: `gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-b --project=project-afa815fe-26c6-40c3-a8b`
   - On Windows, gcloud's bundled PuTTY `plink` rejects OpenSSH `-o` flags. Direct OpenSSH works: `ssh -i ~/.ssh/google_compute_engine vohoanvu@35.232.12.173` (user `vohoanvu`'s key is in project metadata; OS Login is off).
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
| `backend-developer` | sonnet | .NET 10 APIs, services, repositories, EF Core migrations |
| `devops-engineer` | sonnet | Docker, GCP, Nginx, CI/CD, deployment |
| `qa-tester` | sonnet | Manual/automated testing, verification, bug hunting |

### How It Works

1. **Default behavior**: When you assign a task, I act as the `project-manager` â€” analyzing scope, breaking it down, and delegating to the right agents.
2. **Parallel execution**: Independent subtasks are assigned to agents concurrently for speed.
3. **Quality gates**: After implementation, `qa-tester` verifies and `project-manager` reviews.
4. **Memory persists**: Project context and decisions carry across sessions via the memory system.

### Invoking Specific Agents

- Natural language: "Use the backend-developer agent to add a new endpoint"
- Direct: `@backend-developer add a GET endpoint for training session stats`
- Or let the project-manager decide which agent(s) to use

### MCP Servers

Configured in `.claude/.mcp.json`:
- **GitHub** â€” PR workflows, issue management, code review
- **Supabase** â€” Database management and queries

To authenticate MCP servers, use `/mcp` in a Claude Code session.

### Configuration Files

| File | Purpose |
|------|---------|
| `.claude/agents/*.md` | Agent role definitions with tools and instructions |
| `.claude/.mcp.json` | MCP server configurations |
| `.claude/settings.json` | Project-wide permissions and environment |
| `.claude/settings.local.json` | Local overrides (gitignored) |

