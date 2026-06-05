# Plan — Analysis pipeline speedup, live progress, and DELETE cleanup

_Authored 2026-06-06. Branch `feature/gcp-vm-deploy`. Status: awaiting approval (no code changed yet)._

## Context

Two problems surfaced after the Transcoder work shipped and v2 analysis was validated on a real BJJ
competition tape (coaching reports + prescribed drills rated useful by a human black-belt; technique /
event / visual-DNA *details* ~70–80% accurate):

1. **The v2 analysis pipeline is slow** (≈60–120 s/run) and, when the user is on the
   `/video-review/{id}` page, **there is no visible signal that analysis finished** — it feels broken.
2. **The "Uploaded Videos" DELETE leaves residue.** It must fully purge GCS assets (original +
   transcoded) and *all* AI-analysis rows so the same video can be re-uploaded and re-analyzed clean.

### Critical framing decision (cost)
The pipeline is **I/O-bound on remote Vertex AI inference**, not CPU/RAM-bound on the VM. The Hangfire
worker just issues 4 sequential HTTPS calls to `aiplatform.googleapis.com` and waits. **Upgrading the
e2-small VM would not reduce analysis latency** — that spend is wasted. Transcoding is also off-VM
(managed GCP Transcoder). So this plan spends **$0 on infra**; all speed comes from model choice and
generation-config tuning. This matches the bootstrapped constraint.

### Strategy (per your direction)
- **Keep all 4 phases** (Auto-Profiler → Event Logger → Event Verifier → Head Coach). Do not merge.
- **Single model across all phases** — required for Vertex multimodal context caching (cache↔inference
  model must match). The headline lever is swapping the model to **Gemini 3.5 Flash** (speed/cost),
  benchmarked against the current `gemini-3.1-pro-preview` on your real tape.
- **Don't gut the reasoning that protects what already works.** Coaching quality (Head Coach) and the
  accuracy guardrails (Profiler visual-DNA, Verifier) are your weak/important spots — tune those
  carefully, not aggressively.

---

## Task 1 — Pipeline performance

### 1a. Model swap to Gemini 3.5 Flash (headline lever) — single model, caching preserved

Today every phase defaults to `gemini-3.1-pro-preview`
([VertexAiPipelineOptions.cs:40](VideoAnalysis.Server/Configuration/VertexAiPipelineOptions.cs#L40)).
The cache-eligibility guard in `AgenticPipelineService` only creates a shared context cache when **all
four phase `ModelId`s are equal** — so the model must be changed in lockstep.

**Make the swap a one-knob, env-overridable change** (so you can A/B Pro vs Flash on the VM with **no
image rebuild** — just edit `.env` + restart):

- Add a top-level `ModelId` to `VertexAiPipelineOptions` (the shared default for all phases).
- Change `PhaseModelOptions.ModelId` default to empty, and add a `Normalize()` that fills any unset
  phase `ModelId` from the shared default. Call it where options are built in
  [Program.cs](VideoAnalysis.Server/Program.cs) (the existing `GetSection(...).Get<...>()` for the
  pipeline). This **guarantees the caching invariant** (all phases match) and makes the swap:
  ```
  # VM .env  (or appsettings)  — flip the whole pipeline with one value
  VertexAi__Pipeline__ModelId=gemini-3.5-flash
  ```
- ⚠️ **Verify before flipping:** (1) the exact published Vertex model id (likely `gemini-3.5-flash` — confirm in Model Garden / release notes), and (2) that it supports **explicit
  context caching on the `global` endpoint for multimodal video**. If Flash doesn't cache the same way,
  the video is re-sent per phase — Flash is cheap so cost is tolerable, but latency would regress;
  confirm caching works (see 1c) before declaring a win.
- **Rollback** is one line: set `VertexAi__Pipeline__ModelId=gemini-3.1-pro-preview` and restart.

### 1b. Generation-config tuning (careful — protect the weak/good spots)

Current per-phase config
([VertexAiPipelineOptions.cs:31-34](VideoAnalysis.Server/Configuration/VertexAiPipelineOptions.cs#L31-L34)):

| Phase | fps | MaxOutputTokens | ThinkingBudget | Proposed | Rationale |
|---|---|---|---|---|---|
| Profiler | 1 | 1024 | 8192 | **keep** | Builds visual-DNA — a weak spot. Don't starve it. With Flash's speed headroom, consider *raising* to improve accuracy if benchmarks allow. |
| Event Logger | 4 | 65535 | 16384 | MaxOut→**16384**, Thinking→**8192** (benchmark) | 65535 is 40×+ real need (10–30 events). Flash thinks efficiently; 8192 likely fine. **Keep fps=4** initially — events are your weak area; lowering fps risks them. Revisit fps only if speed is still short. |
| Verifier | 0 | 65535 | 4096 | MaxOut→**16384**, Thinking **keep/raise** | This is the identity/accuracy guardrail (your visual-DNA-wrong fix). Keep or raise thinking — it's cheap and targets the exact weakness. |
| Head Coach | 0 | 8192 | 16384 | **keep** | Produces the coaching your coach liked. Do not cut. |

Net: lower output ceilings (safe, mild benefit), trim only Event Logger thinking, and **spend Flash's
speed headroom on the accuracy-critical phases** rather than racing them. All values are config — tune
via `.env` and benchmark, no redeploy.

### 1c. Verify context caching actually hits (observability, not guesswork)

The whole cost/latency model assumes the cache is created once and reused by phases 2–4. Add concise
logging so we *know*:
- Log cache-create outcome (created vs skipped-ineligible vs failed) with the reason, in the
  `TryCreateCacheAsync` path of
  [AgenticPipelineService.cs](VideoAnalysis.Server/Domain/AIServices/AgenticPipelineService.cs).
- Log, per phase, whether the request used `cachedContent` (cache hit) or fell back to re-sending the
  `fileUri`. If fallback rate is high after the Flash swap, caching isn't supported as assumed → revisit.

### 1d. Vertex call resilience (avoid throwing away a 90 s run)

A transient 429/503 on phase 4 currently fails the whole pipeline, wasting phases 1–3. Add a small
bounded retry with backoff (e.g. 3 attempts, 2s→5s→10s) to the POST path in
[VertexRestClient.cs](VideoAnalysis.Server/Domain/AIServices/VertexRestClient.cs) for transient status
codes only (not 4xx validation). This is reliability, which directly affects *perceived* speed (no
silent restarts). The existing 0-events fallback in the Event Logger is already bounded (one retry) —
leave it.

### 1e. Perceived performance (cheapest real win) → see Task 2
The backend **already emits per-phase SignalR events**; the review page just doesn't listen. Surfacing
live progress makes the same wall-clock feel dramatically faster. This is the highest ROI item in the
whole plan.

**Files (Task 1):** `VideoAnalysis.Server/Configuration/VertexAiPipelineOptions.cs`,
`VideoAnalysis.Server/Program.cs` (Normalize call), `…/Domain/AIServices/AgenticPipelineService.cs`
(cache logging), `…/Domain/AIServices/VertexRestClient.cs` (retry). Plus `.env` / `appsettings.json`
for the model + tuning values.

---

## Task 2 — Live progress + completion notification on the review page

**The backend is already correct.** `AgenticPipelineService` emits, on `VideoAnalysisHub`:
- `AnalysisStatusChanged(videoId, status)` after each phase (Profiling/Logging/Verifying/Coaching),
- `AnalysisV2Completed(videoId)` on success,
- `AnalysisV2Failed(videoId, error)` on failure.

These already drive a toast on `VideoAnalysisManagement.tsx`. **The `/video-review/{id}` page
(`VideoReview.tsx`) registers no SignalR listeners** — it only polls every 10 s for *transcode* status.
So on the page where the user waits for analysis, completion is invisible.

**Fix — mirror the existing pattern from
[VideoAnalysisManagement.tsx](CodeJitsu.Client/src/pages/VideoAnalysisManagement.tsx) into
[VideoReview.tsx](CodeJitsu.Client/src/pages/VideoReview.tsx):**
- In a `useEffect`, ensure `analysisConnection` (from
  [SignalRService.ts](CodeJitsu.Client/src/services/SignalRService.ts)) is started, then register
  handlers filtered by the current `videoId`:
  - `AnalysisStatusChanged` → show a live phase indicator reusing existing i18n keys
    `videoReviewV2.progress.{profiling|logging|verifying|coaching}`.
  - `AnalysisV2Completed` → refetch the analysis (existing `GET …/analysis-v2` path) to swap in results,
    and toast `videoReviewV2.progress.completeTitle`.
  - `AnalysisV2Failed` → toast `videoReviewV2.progress.failedTitle` / `failedBody`.
  - Clean up handlers on unmount (`connection.off(...)`).
- (Optional, nice) Add an `AnalysisV2Completed` listener to
  [NotificationsListener.tsx](CodeJitsu.Client/src/components/NotificationsListener.tsx) so the global
  banner fires regardless of which page the user is on. The legacy banner currently only listens for v1
  `AnalysisCompleted`.

**Auth note:** the hubs broadcast to `Clients.All` and are not `[Authorize]`-gated, and the client
filters by `videoId`, so the current connection works as-is — **no SignalR auth change is required**
for this fix. (Per-user group targeting is a future nicety, out of scope.)

**No new i18n keys needed** — `videoReviewV2.progress.*` already exist in en + pl.

**Files (Task 2):** `CodeJitsu.Client/src/pages/VideoReview.tsx` (primary),
optionally `CodeJitsu.Client/src/components/NotificationsListener.tsx`.

---

## Task 3 — DELETE "Uploaded Videos": full GCS + DB cleanup

Endpoint: `DELETE /vid/api/video/delete-uploaded/{videoId}`
([VideoController.cs:294-327](VideoAnalysis.Server/Controllers/VideoController.cs#L294-L327)).

### What's actually wrong (verified against source)
- ❌ **`AiAnalysisResult` row is never deleted.** Only `Techniques` + `Drills` (which reference it) are
  bulk-deleted. The parent row is orphaned — **and because the v2 children (`MatchEvent`,
  `CoachingReport` + Strengths/Weaknesses/PrescribedDrills) cascade *from* `AiAnalysisResult`, they
  never get cleaned either.** This is the core "can't re-analyze clean" bug.
- ❌ **`AnalysisWeakness`** (FK→`AiAnalysisResult`, no cascade) and **`VideoSegmentFeedback`**
  (FK→`Video`, no cascade) are orphaned.
- ❌ **No ownership check** — any authenticated user can delete any video. (`getall-uploaded` correctly
  filters `v.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)`
  ([VideoController.cs:339](VideoAnalysis.Server/Controllers/VideoController.cs#L339)) — delete should
  match.)
- ⚠️ **GCS original delete throws → 500** ([line 317](VideoAnalysis.Server/Controllers/VideoController.cs#L317))
  after the DB row is already gone. The transcoded copy is best-effort (good), but the original is not.
- ⚠️ `_serviceProvider.CreateScope()` is not disposed (minor leak); deletes are not transactional.
- ✅ GCS original + transcoded *are* targeted for deletion. **Correction to earlier review:** the
  frontend `isLoading` is set to `false` in `finally`
  ([VideoStorageListing.tsx:78](CodeJitsu.Client/src/pages/VideoStorageListing.tsx#L78)) — **no stuck-
  spinner bug**. The only frontend gap is no server re-sync after the optimistic remove, and errors
  render as red text rather than a toast.

### Backend fix — `DeleteUploadedVideoAsync`
Rewrite to be owner-scoped, complete, atomic, and non-throwing on GCS:

1. `using var scope = _serviceProvider.CreateScope();` (dispose it).
2. **Owner check:** load the video filtered by `Id == videoId && UserId == current user`; if null →
   `NotFound` (don't reveal others' videos).
3. **DB cleanup inside a transaction** (`BeginTransactionAsync`), in FK-safe order:
   - bulk-delete `Techniques` where `AiAnalysisResultId == aiId`
   - bulk-delete `Drills` where `AiAnalysisResultId == aiId`
   - bulk-delete `AnalysisWeakness` where `AiAnalysisResultId == aiId`  *(confirm DbSet name)*
   - bulk-delete `VideoSegmentFeedback` where `VideoId == videoId`  *(confirm DbSet name)*
   - **delete the `AiAnalysisResult` row** → cascades `MatchEvent` + `CoachingReport`
     (+Strengths/Weaknesses/PrescribedDrills)
   - delete the `Video` row
   - commit. (Guard the `aiId == null` case — video may have no analysis yet.)
4. **GCS cleanup — best-effort, never 500:** delete `FilePath` (original) and the transcoded artifacts;
   wrap each in try/catch + `LogWarning`, then return `200`. An orphaned GCS object is a minor cost
   issue, not a user-facing error — satisfies "no error shows in client."
5. **Robust transcoded cleanup:** instead of relying solely on `PlaybackFilePath`, best-effort delete
   the whole `transcoded/{videoId}/` prefix so residue from a failed/partial transcode is also removed.
   Add `DeleteByPrefixAsync(prefix)` (list + delete) to
   [GoogleCloudStorageService.cs](VideoAnalysis.Server/Domain/GoogleCloudStorageService/GoogleCloudStorageService.cs)
   alongside the existing `DeleteFileAsync`.

This guarantees a deleted video leaves **zero** GCS or DB residue, so re-upload → re-analysis starts
fresh even for the same source file (note: re-upload creates a new `videoId` + new GUID-prefixed GCS
path anyway, so there's no collision — this is purely about not leaking storage/rows).

### Frontend fix — `VideoStorageListing.tsx`
- After a successful delete, call the existing `fetchVideos()` to **re-sync with the server** (keep the
  optimistic `setVideos(filter)` for snappiness, then reconcile). Today it only does the optimistic
  filter ([line 74](CodeJitsu.Client/src/pages/VideoStorageListing.tsx#L74)).
- Surface errors via the app's toast (consistent with the rest of the app) in addition to the inline
  `error` text. No other changes needed (spinner handling is already correct).

**Files (Task 3):** `VideoAnalysis.Server/Controllers/VideoController.cs`,
`VideoAnalysis.Server/Domain/GoogleCloudStorageService/GoogleCloudStorageService.cs`,
`CodeJitsu.Client/src/pages/VideoStorageListing.tsx`. Confirm DbSet names in
`SharedEntities/Data/DatabaseContext.cs` / `SharedEntities/Models/Feedbacks.cs`.

---

## Verification

**Automated**
- Backend: `dotnet test` (baseline 151 pass). Add an EF-InMemory test for the delete path asserting
  that `AiAnalysisResult`, `MatchEvent`, `CoachingReport` (+children), `AnalysisWeakness`,
  `VideoSegmentFeedback`, `Techniques`, `Drills`, and the `Video` row are all gone, and that a
  non-owner gets `NotFound`.
- Frontend: `cd CodeJitsu.Client && npx vitest run` (baseline 99 pass). Add a test that delete triggers
  a re-fetch; `tsc` must stay clean.

**Manual E2E (local Docker, then VM)**
1. Upload a tape, sit on `/video-review/{id}` → confirm live phase text advances and a completion
   toast fires + results appear **without manual reload**. Kill a phase (or force-fail) → failure toast.
2. Delete a video → table refreshes, no client error; verify GCS is empty
   (`gcloud storage ls gs://martial-art-demo-vids/** | grep {videoId}` → nothing, original + transcoded
   gone) and DB rows are gone (Supabase MCP `execute_sql` against `AiAnalysisResults`/`MatchEvents`/
   `CoachingReports`/`Videos`). Re-upload the same file → fresh analysis runs clean.

**Performance / accuracy benchmark (the real acceptance test for Task 1)**
- Time one pipeline run end-to-end on the **same tape**, Pro vs Flash (watch the new per-phase SignalR
  timing + cache-hit logs). Expect a meaningful wall-clock drop with Flash + caching.
- Re-grade output quality with your black-belt coach on the Flash run. **Acceptance:** coaching/drills
  stay useful and technique/event/visual-DNA accuracy is **≥ the current 70–80%**. If Flash regresses
  accuracy, roll back the model (one `.env` line) and keep the tuning + caching + SignalR wins.

**Deploy**
- The `ModelId` plumbing + retry + delete/SignalR changes need a normal image rebuild (CI) + manual VM
  deploy (per `HANDOFF.md` "Deploy reality"). After that, **model/tuning experiments are `.env`-only**
  (`VertexAi__Pipeline__ModelId`, `…__EventLogger__ThinkingBudget`, etc.) + `docker-compose restart` —
  no rebuild. No new GCP IAM/API prereqs (Flash uses the same Vertex `aiplatform.user` the VM SA holds).

---

## Risks & rollback
- **Flash accuracy regression** → one-line `.env` rollback to `gemini-3.1-pro-preview`. The non-model
  wins (caching verification, output-token trims, SignalR progress, delete cleanup, retry) are
  model-independent and stay.
- **Flash multimodal caching unsupported on `global`** → caught by the 1c logging; cost stays low
  (Flash) but re-evaluate the latency claim before calling it done.
- **Delete transaction across Supabase session pooler** → uses the same EF context/connection settings
  already in production; `ExecuteDeleteAsync` + a single transaction is well within `CommandTimeout`.

## Suggested execution order
1. Task 2 (SignalR live progress) — cheapest, biggest *felt* improvement, zero risk.
2. Task 3 (DELETE cleanup) — correctness/data-hygiene, unblocks clean re-analysis.
3. Task 1 (model swap + tuning + caching/retry) — deploy once, then iterate model/tuning via `.env`
   and benchmark with your coach.
```
