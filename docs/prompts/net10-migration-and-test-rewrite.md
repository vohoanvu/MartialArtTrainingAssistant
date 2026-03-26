# Prompt: .NET 10 Migration + Full Test Rewrite

> Paste everything below the line into a fresh Claude Code chat session.

---

Act as the **project-manager** agent. I need you to coordinate a major refactoring effort across the entire codebase with two parallel workstreams:

## Workstream 1: Migrate .NET from 8.0 to 10.0

### Scope — Backend (.NET)

**All 5 .csproj files** must be updated:
- `FighterManager.Server/FighterManager.Server.csproj`
- `VideoSharing.Server/VideoSharing.Server.csproj`
- `MatchMaker.Server/MatchMaker.Server.csproj`
- `SharedEntities/SharedEntities.csproj`
- `SampleAspNetReactDockerApp.Tests/SampleAspNetReactDockerApp.Tests.csproj`

For each .csproj:
1. Change `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`
2. Upgrade all NuGet packages to their latest .NET 10-compatible versions. Key packages to check:
   - `Microsoft.AspNetCore.Authentication.JwtBearer` (currently 8.0.8)
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (currently 8.0.2)
   - `Microsoft.EntityFrameworkCore.Design` (currently 8.0.2)
   - `Npgsql.EntityFrameworkCore.PostgreSQL` (currently 8.0.2)
   - `Microsoft.AspNetCore.Authentication.Google` (currently 8.0.5)
   - `Microsoft.AspNetCore.SpaProxy` (currently 8.*)
   - `Serilog.AspNetCore` (currently 8.0.1)
   - `Asp.Versioning.Http` and `Asp.Versioning.Mvc.ApiExplorer` (currently 8.0.0)
   - `Swashbuckle.AspNetCore.Filters` (currently 8.0.0)
   - `Hangfire`, `Hangfire.AspNetCore`, `Hangfire.PostgreSql`
   - `AutoMapper` (currently 13.0.1)
   - `Microsoft.EntityFrameworkCore.InMemory` (currently 8.0.4, test project)
   - `Moq.EntityFrameworkCore` (currently 8.0.1.2, test project)
   - All other packages — check for latest stable versions
3. Search the codebase for any .NET 8-specific APIs, patterns, or breaking changes and update them to .NET 10 equivalents. Use `WebSearch` to look up the official .NET 10 migration guide and breaking changes list.
4. Review each `Program.cs` for deprecated middleware, DI registration patterns, or startup configuration that changed between .NET 8 and .NET 10.
5. Create/Update a `global.json` in the project root pinning the .NET 10 SDK version.

**All 3 Dockerfiles** must be updated:
- `FighterManager.Server/Dockerfile`
- `VideoSharing.Server/Dockerfile`
- `MatchMaker.Server/Dockerfile`

For each Dockerfile:
- Change `mcr.microsoft.com/dotnet/aspnet:8.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`
- Change `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/sdk:10.0`

**CI/CD pipeline** (`.github/workflows/deploy-to-vm.yml`):
- Update any .NET SDK version references if present
- Verify the workflow still works with .NET 10 Docker images

### Scope — Frontend (React/TypeScript)

While the frontend is not migrating frameworks, use this opportunity to:
1. Update `package.json` dependencies to their latest stable versions (run `npm outdated` to check)
2. Pay special attention to:
   - `react` and `react-dom` — check if React 19 is stable and worth upgrading, or stay on 18.x latest
   - `@microsoft/signalr` (currently ^8.0.0) — update to match .NET 10 SignalR
   - `vite` (currently ^5.1.0) — update to latest
   - `typescript` (currently ^5.2.2) — update to latest
   - All `@radix-ui/*` packages — update to latest
   - `tailwindcss` (currently ^3.4.1) — check if Tailwind v4 is stable and appropriate
   - `eslint` and typescript-eslint — update to latest compatible versions
3. Run `npm run build` after updates to verify no TypeScript or build errors

### Validation Criteria

After all changes:
- [ ] `dotnet build` succeeds with zero errors and zero warnings
- [ ] `docker compose --env-file ./.env build` succeeds for all services
- [ ] `npm run build` succeeds in `SampleAspNetReactDockerApp.Client/`
- [ ] No hardcoded references to `net8.0`, `8.0`, or .NET 8-specific APIs remain in the codebase

---

## Workstream 2: Rewrite All Test Coverage from Scratch

### Step 1 — Delete all existing tests

Remove all existing test files completely:
- **Backend**: Delete all `.cs` files in `SampleAspNetReactDockerApp.Tests/` (currently `ShareVideoRepositoryTests.cs`, `VideoControllerTests.cs`, `YoutubeDataServiceTests.cs`)
- **Frontend**: Delete `SampleAspNetReactDockerApp.Client/src/unit-tests/SharedVideoList.test.ts`

Keep the test project structure and configuration files (`.csproj`, `jest.config.ts`), but update them for .NET 10 / latest packages.

### Step 2 — Backend test rewrite

Use `@backend-developer` and `@qa-tester` agents collaboratively.

**Test project setup** (`SampleAspNetReactDockerApp.Tests/`):
- Update `.csproj` to target `net10.0` with latest test packages (xUnit, Moq, EF Core InMemory, coverlet)
- Ensure the test project references all 3 service projects and SharedEntities

**Write new tests organized by service:**

```
SampleAspNetReactDockerApp.Tests/
├── FighterManager/
│   ├── Controllers/
│   │   └── [Controller]Tests.cs     — for each controller
│   └── Services/
│       └── [Service]Tests.cs        — for each domain service
├── VideoSharing/
│   ├── Controllers/
│   │   └── [Controller]Tests.cs
│   └── Services/
│       └── [Service]Tests.cs
├── MatchMaker/
│   ├── Controllers/
│   │   └── [Controller]Tests.cs
│   └── Services/
│       └── [Service]Tests.cs
├── SharedEntities/
│   └── [Model]ValidationTests.cs    — model validation tests
└── Integration/
    └── DatabaseIntegrationTests.cs  — EF Core InMemory integration tests
```

**Testing standards:**
- Naming convention: `Should_[ExpectedResult]_When_[Condition]`
- AAA pattern (Arrange-Act-Assert) with clear section comments
- Use Moq for mocking dependencies
- Use EF Core InMemory provider for database tests
- Test happy paths, error paths, edge cases, and authorization
- Aim for coverage of all public controller actions and service methods
- Read each controller and service file first to understand what needs testing

### Step 3 — Frontend test rewrite

Use `@frontend-developer` and `@qa-tester` agents collaboratively.

**Test setup:**
- Update `jest.config.ts` if needed for latest packages
- Update test-related devDependencies in `package.json` to latest versions:
  - `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`
  - `jest`, `ts-jest`, `jest-environment-jsdom`, `@types/jest`, `@jest/types`
  - Remove `jest-fetch-mock` if no longer needed (prefer `msw` for API mocking)
- Consider migrating from Jest to Vitest if it provides a better DX with the current Vite setup (evaluate and decide)

**Write new tests organized by feature area:**

```
SampleAspNetReactDockerApp.Client/src/
├── __tests__/               — or colocate with components
│   ├── components/
│   │   └── [Component].test.tsx    — for key UI components
│   ├── pages/
│   │   └── [Page].test.tsx         — for each page/route
│   ├── stores/
│   │   └── [Store].test.ts         — for Zustand stores
│   └── services/
│       └── [Service].test.ts       — for API service layers
```

**Testing standards:**
- Use React Testing Library — test behavior, not implementation
- Use `userEvent` for simulating user interactions (not `fireEvent`)
- Mock API calls with `msw` (Mock Service Worker) or jest mocks
- Test: rendering, user interactions, form validation, error states, loading states
- Verify i18n keys are used (no hardcoded strings in rendered output)
- Read each component/page file first to understand what needs testing
- Aim for coverage of all pages and critical interactive components

### Validation Criteria

After all tests are written:
- [ ] `dotnet test` passes with all tests green
- [ ] `npm test` (or `npx vitest run`) passes with all tests green
- [ ] No skipped or pending tests
- [ ] Coverage report shows meaningful coverage of controllers, services, and key UI components

---

## Execution Strategy

Please follow this execution order:

**Phase 1 — Migration (do this first)**
1. `@backend-developer`: Upgrade all .csproj files, NuGet packages, and Program.cs files to .NET 10
2. `@devops-engineer`: Update all Dockerfiles and CI/CD pipeline for .NET 10
3. `@frontend-developer`: Update all npm packages to latest versions
4. Verify: `dotnet build`, `docker compose build`, `npm run build` all succeed

**Phase 2 — Test Infrastructure (after Phase 1 passes)**
5. `@backend-developer`: Delete old tests, restructure test project for .NET 10
6. `@frontend-developer`: Delete old tests, update test tooling (evaluate Jest vs Vitest)

**Phase 3 — Test Writing (after Phase 2)**
7. `@backend-developer` + `@qa-tester`: Write backend tests (parallelize by service)
8. `@frontend-developer` + `@qa-tester`: Write frontend tests (parallelize by feature area)

**Phase 4 — Validation**
9. `@qa-tester`: Run full test suite, verify coverage, report any failures
10. `@project-manager`: Final review of all changes, ensure no regressions

Create tasks for each phase and track progress. Report back after each phase completes.
