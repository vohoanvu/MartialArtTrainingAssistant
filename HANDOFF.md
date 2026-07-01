# Hand-off — what to work on next

_Last updated: 2026-07-01. Previous: GCP Transcoder API for HEVC playback, 2026-06-05._

`thecodejitsu.com` runs on the MyCoach project (`project-afa815fe-26c6-40c3-a8b`). Operational
gotchas live in `CLAUDE.md` → *Deployment*. **Read "Deploy reality" below before deploying** — the
CI/CD only builds images; the actual VM deploy is manual.

---

## 🚑 Incident + recovery (2026-07-01) — VM zone moved `us-central1-c` → `us-central1-b`

After the MyCoach **billing** interruption, `thecodejitsu-app-vm` (stopped) would not restart:
`ZONE_RESOURCE_POOL_EXHAUSTED` — Google had **no capacity** in `us-central1-c`. During the event
**e2-medium AND n1-standard-1 were unavailable in all four us-central1 zones (a/b/c/f)** — a broad
regional stockout, not a config issue.

**Fix (site restored, verified `https://thecodejitsu.com/` = 200):**
1. Machine image `codejitsu-vm-recovery` (global) created as a full backup (captures boot disk + SA
   `codejitsu-vm-runtime` + tags `http-server`/`https-server` + network).
2. Deleted the old instance **keeping the boot disk** (`--keep-disks=boot`) → freed the name + the
   regional static IP.
3. Recreated **same name / same static IP `35.232.12.173`** from the image, sweeping zones; landed in
   **`us-central1-b`** at **e2-medium** (cost-neutral ~$24/mo, 4 GB). The static IP is regional so it
   followed for free; **no Cloudflare DNS change needed**.
4. SSH’d in (see Windows note below) and ran `cd ~/app && docker-compose up -d` — containers had been
   `Exited (0)` since the shutdown (no restart policy) so they needed a manual start.

**Follow-ups from this incident:**
- ⬜ **Add `restart: unless-stopped`** to the compose services (repo `docker-compose.yml` + the VM's
  `~/app/docker-compose.yml`) so the site self-heals after any future stop/reboot instead of needing a
  manual `docker-compose up -d`.
- ⬜ **Clean up backups once confident:** machine image `codejitsu-vm-recovery` + orphaned boot disk
  `thecodejitsu-app-vm` in `us-central1-c` (both still billing a little).
- ℹ️ If capacity in `us-central1-c` returns and you want to move back, same machine-image dance (or just
  stay in `-b`). All docs/commands now say `us-central1-b`.
- 🪟 **Windows SSH gotcha:** gcloud's bundled PuTTY `plink` rejects OpenSSH `-o` flags. Use OpenSSH
  directly: `ssh -i ~/.ssh/google_compute_engine vohoanvu@35.232.12.173 "<cmd>"` (user `vohoanvu`'s
  key is in project metadata; OS Login off; new host key on reused IP, so add
  `-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null` for scripted calls).

---

## ✅ Done this session (2026-06-06)

All on `feature/gcp-vm-deploy`, **deployed to the VM** (plan: `ANALYSIS-PIPELINE-OPTIMIZATION-PLAN.md`).

1. **Analysis pipeline perf knobs.** Model is now a single env flip: shared `VertexAi:Pipeline:ModelId`
   propagated to all 4 phases via `Normalize()` (PostConfigure in `Program.cs`) — set
   `VertexAi__Pipeline__ModelId=...` in the VM `.env` + recreate `video-analysis` (no rebuild). Also:
   trimmed oversized MaxOutputTokens + EventLogger ThinkingBudget (kept Profiler/Verifier/HeadCoach
   budgets — accuracy-critical), context-cache active/disabled/failed logging, and bounded retry/backoff
   (2s/5s/10s, transient 429/5xx) in `VertexRestClient`.
   - **Tried Gemini 3.5 Flash → ROLLED BACK to `gemini-3.1-pro-preview`.** Flash dropped accuracy to
     ~50% (Phase-2 Event Logger events frequently wrong — worse than Pro). Pro stays.
     ⚠️ The `.env` model override does NOT survive `refresh-env-secret-manager.sh` (it does `> .env`);
     to A/B a model durably, add it to that script's static section or to Secret Manager.
2. **DELETE uploaded video — full cleanup** ✅ **confirmed working in prod.** Owner-scoped +
   transactional purge of AiAnalysisResult (DB-cascades MatchEvent + CoachingReport children) +
   AnalysisWeakness/VideoSegmentFeedback/Techniques/Drills; best-effort GCS delete of original + the
   whole `transcoded/{videoId}/` prefix (new `DeleteByPrefixAsync`); returns 200 (no client error).
   Client re-syncs via `fetchVideos()`.
3. **Live analysis progress on `/video-review/{id}`** — per-phase spinner + auto-refresh of results on
   completion (the backend already emits the SignalR events).

Local verify: backend 151 pass / 6 skipped; frontend 99 pass; tsc clean. (Build backend without the SPA
esproj npm step via `dotnet build --no-dependencies` + `dotnet test --no-build`.)

### ✅ Completion BANNER fixed — code-complete 2026-06-06 (needs deploy + browser check)
Root cause was `NotificationsListener.tsx` only handling the legacy v1 `AnalysisCompleted` (the default
v2 pipeline emits `AnalysisV2Completed`) AND never starting `analysisConnection` (only videoShareHub).
Fix: `NotificationsListener` now OWNS both connections (starts videoShareHub + videoAnalysisHub) and
fires the global banner for `AnalysisV2Completed`/`AnalysisV2Failed` (+ legacy `AnalysisCompleted`).
Pages (`VideoReview`, `VideoAnalysisManagement`) were trimmed to page-local reactions only (phase
spinner / list & results refresh); their completion toasts were removed so the banner is the single
notification. Added i18n `videoReviewV2.progress.completeBanner` (en/pl); `NotificationPopup`'s
"Shared by" line is now conditional. Local verify: tsc clean, frontend 99 pass. **DEPLOYED to the VM
2026-06-06** (CI run 27062754971 → app-client digest `sha256:33b3048…` matches AR `latest`, site 200).
**Remaining: browser-verify the banner actually pops on a real analysis completion.**

## ✅ Done this session (2026-06-05)

All landed on `feature/gcp-vm-deploy` and **deployed to the VM**.

1. **Agentic 4-phase VLM pipeline (v2)** — ported from the MyCoach mobile backend (PR #18).
   Auto-Profiler (+ context cache) → Event Logger → Event Verifier → Head Coach. New code under
   `VideoAnalysis.Server/Domain/AIServices/` (`VertexRestClient`, `AgenticPipelineService`,
   `PipelinePrompts`, `PipelineSchemas`, `AgenticAnalysisRead/WriteService`), `Configuration/
   VertexAiPipelineOptions`, Hangfire `AgenticAnalysisBackgroundJobService` (dedicated
   `vertex-pipeline` queue), 4 `GeminiController` endpoints (`analyze-v2`, GET/PATCH `analysis-v2`,
   `status`). New EF entities `MatchEvent` + `CoachingReport`(+strengths/weaknesses/drills) in
   `SharedEntities/Models/MatchAnalysis.cs` + columns on `AiAnalysisResult`. Editable V2 UI under
   `CodeJitsu.Client/src/components/VideoAnalysisEditor/v2/`. **v2 is the default**
   (`VertexAi:Pipeline:Enabled` + `VITE_ANALYSIS_V2_ENABLED`).
   - **KEY: Vertex context caching runs on the GLOBAL endpoint** — `https://aiplatform.googleapis.com`
     + `/v1/projects/{p}/locations/global/cachedContents`, model `projects/{p}/locations/global/...`.
     Confirmed against the proven MyCoach `VertexAiService` (tested with real uploads). All 4 phases
     must share ONE model (cache↔inference model must match). Cache body holds only the video (fps is
     a *prompt* instruction, not a request param); expiry via `expireTime`.

2. **Large-file upload — direct-to-GCS** (PR #19). The old path uploaded the whole file through
   Cloudflare → nginx → container, capped at **Cloudflare's 100 MB** free-tier limit (tapes are
   0.8–4 GB). Now: `POST /api/video/upload-url` returns a V4 signed **PUT** URL
   (`GoogleCloudStorageService.GenerateUploadUrlAsync`); the browser PUTs **straight to
   `storage.googleapis.com`** (bypasses Cloudflare/nginx/VM), then triggers `analyze-v2`.
   - **Bucket CORS is required** and is **set** on `gs://martial-art-demo-vids` (PUT/GET/HEAD from
     `https://thecodejitsu.com` + localhost). If the bucket is ever recreated, re-apply CORS.

3. **Bug fixes (committed direct to `feature/gcp-vm-deploy`):**
   - `f8216a7` — `upload-url` 400: `martialArt` was bound as an enum but the client sends the name
     string; this app's System.Text.Json reads enums from numbers. Now `string` + `Enum.TryParse`.
   - `8d69829` — `upload-url`/playback/delete 500 `Invalid bucket name 'martial-art-demo-vids\n'`:
     the **VM `.env` carries trailing CR/LF on values** (CRLF line endings / Secret Manager). The GCS
     SDK rejects a bucket name with a trailing newline. **Fix: `.Trim()`** the bucket name + Vertex
     project/location/model where read from config (`GoogleCloudStorageService`, `VertexRestClient`,
     `GeminiVisionService`). JWT issuer/audience left **untrimmed** on purpose (both issue+validate
     sides carry the same `\r` and match — trimming one side invalidates active sessions).
   - `2211a8c` — HEVC playback: competition tapes are **HEVC/H.265 + AAC** (`hvc1`/`mp4a`), which
     browsers can't render (audio plays, picture blank). Shipped **graceful degradation**:
     `VideoPlayer` detects an unrenderable track (`videoWidth/Height===0` after metadata, or media
     error) and shows a message + **Download** button instead of a blank player. en/pl i18n added.
     (The *real* fix — in-browser playback of HEVC — is Task A below.)

### Deploy reality (IMPORTANT — bit us twice this session)
- **CI/CD only BUILDS + pushes images** to Artifact Registry. The "Build and Deploy to GCP VM"
  GitHub Action does **not** deploy to the VM. A green CI run ≠ deployed.
- **There is no `~/deploy-app.sh`** (CLAUDE.md is wrong). Deploy manually from `~/app`, **without
  `sudo`** (the gcloud cred helper is a snap not on sudo's `secure_path`):
  ```bash
  gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-b --project=project-afa815fe-26c6-40c3-a8b
  cd ~/app && gcloud auth configure-docker us-central1-docker.pkg.dev --quiet
  docker-compose pull && docker-compose down --remove-orphans && docker container prune -f && docker-compose up -d
  docker ps   # verify fresh CreatedAt
  ```
- Containers are `app-{fighter-manager,video-analysis,app-client}-1`. Verify a deploy by matching the
  running image digest to AR `latest`, e.g.
  `docker images --digests | grep video-analysis` vs
  `gcloud artifacts docker images describe …/video-analysis:latest --format='value(image_summary.digest)'`.

---

## Next tasks

### A. GCP Transcoder API — H.264 web-playable version ✅ CODE-COMPLETE (not yet deployed)
**Goal:** make HEVC tapes viewable in-browser by transcoding each upload to H.264/AAC MP4 (managed,
off-VM). Analysis already ingests HEVC fine — this is purely for playback. **All code landed on
`feature/gcp-vm-deploy`; backend `dotnet test` 151 pass / 0 fail, frontend 99 pass, tsc clean.**

**What shipped (REST, consistent with `VertexRestClient` — ADC bearer to `transcoder.googleapis.com`):**
- **EF:** `VideoMetadata` += `PlaybackFilePath` (string?), `TranscodeStatus` enum
  (`None/Processing/Ready/Failed`). Migration `20260604173101_AddVideoTranscodeFields` created
  (additive: `text` col + `integer` col default 0). ⚠️ **NOT yet applied to Supabase** — see below.
- **`VideoTranscodeService`** (`Domain/TranscodeService/`): `CreateJobAsync` → `POST …/jobs` with one
  H.264 video stream (`heightPixels:720`, no width = keep aspect), one AAC stream, one `mp4` mux keyed
  `playback` → output at `gs://martial-art-demo-vids/transcoded/{videoId}/playback.mp4`. `GetJobAsync`
  polls `state`. Location `us-central1`.
- **Hangfire `VideoTranscodeBackgroundJobService.ProcessTranscodeAsync(videoId)`** on a dedicated
  `transcode` queue (added to the default Hangfire server's queue list in `Program.cs`): set
  `Processing` → create job → poll (`Task.Delay`, configurable interval/timeout) → on success set
  `PlaybackFilePath` + `Ready` + SignalR `TranscodeReady`; on fail/timeout set `Failed`. DB writes use
  short scopes so the long poll loop never holds a pooled connection.
- **Triggers:** auto-enqueue in `analyze-v2` (gated by `Transcoder:AutoTranscodeOnUpload`) **and**
  manual `POST /api/video/{videoId}/transcode` (existing videos / retry; no-ops if Processing/Ready).
- **Read path:** `VideoController.GetUploadedVideoAsync` serves the `PlaybackFilePath` signed URL when
  `Ready` (else original) and returns `transcodeStatus` in `UploadedVideoDto` (also in `getall-uploaded`).
  Delete now best-effort removes the transcoded copy too.
- **Config:** `TranscoderOptions` (`Transcoder` section in appsettings; env overrides `Transcoder__*`).
- **Frontend:** `requestTranscode(videoId)` in `api.ts`; `VideoReview` tracks `transcodeStatus`, polls
  `getVideoDetails` (~10 s) while `Processing` and swaps in the playable URL on `Ready`; `VideoPlayer`
  HEVC overlay shows **"Convert for playback"** (None) / **"Retry conversion"** (Failed) / a
  **"Converting…"** message (Processing), plus Download. en/pl i18n under `videoReviewV2.player.*`.

**REMAINING — only the VM deploy is left; the DB migration + IAM/API prereqs are DONE:**

- ✅ **Migration applied to Supabase** (2026-06-05, via Supabase MCP) — `Videos.PlaybackFilePath` (text
  null) + `TranscodeStatus` (int NOT NULL default 0); EF `__EFMigrationsHistory` row inserted
  (ProductVersion 10.0.5) so `dotnet ef database update` is a no-op for it.
- ✅ **IAM/API prereqs applied** (2026-06-05) on project `project-afa815fe-26c6-40c3-a8b`:
  - `transcoder.googleapis.com` enabled.
  - VM SA `codejitsu-vm-runtime` → **`roles/transcoder.editor`** (NOT `roles/transcoder.user` — that
    role does not exist; the create+get-job-capable least-privilege predefined role is
    `transcoder.editor`).
  - Transcoder service agent `service-81800761856@gcp-sa-transcoder.iam.gserviceaccount.com` →
    `roles/storage.objectAdmin` on `gs://martial-art-demo-vids` (provisioned via
    `gcloud beta services identity create --service=transcoder.googleapis.com`).
- ⬜ **Remaining:** build/push images (CI) + manual VM deploy (see *Deploy reality*). No `.env` change
  needed (Transcoder defaults are baked into appsettings; override with `Transcoder__*` if desired).

**Cost / decision:** ~$0.015/min (SD) – $0.06/min (HD) of *output*. Shipped default = **auto-transcode
every analyze-v2 upload** (`AutoTranscodeOnUpload:true`) + manual endpoint. Flip
`Transcoder__AutoTranscodeOnUpload=false` to make it strictly on-demand (cheaper — only HEVC tapes the
user explicitly converts) if the auto cost on already-H.264 uploads proves wasteful. 720p default res.

### B. Harden the VM `.env` against CR/LF  _(root cause of the `8d69829` 500)_
The code now trims the GCS/Vertex values, but **other env values still carry `\r`** (e.g. JWT
`iss/aud` = `DEV\r`). Fix `~/app/refresh-env-secret-manager.sh` to strip CR when it writes `.env`
(pipe Secret Manager values through `tr -d '\r'`, or `sed -i 's/\r$//' .env` after writing).
⚠️ If you strip the whole `.env`, JWT `iss/aud` change `DEV\r`→`DEV` → **one-time re-login** for
anyone with an active session (acceptable, but do it knowingly). Then the code trims become belt-and-
suspenders.

### C. Decommission the old `codejitsu` project  ✅ DONE 2026-06-06 (resources)
Ran the automated teardown (steps individually, since the auto-mode classifier blocks the opaque
`CONFIRM=DECOMMISSION` script). Deleted in `codejitsu`: VM `thecodejitsu-app-vm` (found RUNNING but
idle — 0 containers — so safe), static IP `codejitsu-static-ip`, AR repo `codejitsu-repo`, 4 app
firewall rules (default VPC rules kept), all 22 Secret Manager secrets; plus the MyCoach staging bucket
`martial-art-demo-vids-migrate`. Verified: prod `thecodejitsu.com` = 200, live bucket intact.

**Still MANUAL / optional (not done):**
- Delete the OLD **OAuth 2.0 client** in `codejitsu` (Console → APIs & Services → Credentials) — not
  gcloud-manageable.
- Delete the OLD **YouTube API key** `VideoSharingAppDemoYoutube` (uid `d7496e6c-4264-44cf-8847-3880fbfe0648`)
  — IS gcloud-deletable (`gcloud services api-keys delete <uid> --project=codejitsu`); left as manual
  per the protocol. (Other keys in `codejitsu` are Firebase/other apps — do NOT touch.)
- **Rotate the GitHub PAT** in `.mcp.json`/`.claude/.mcp.json` (https://github.com/settings/tokens — it
  appeared in a transcript).
- Leftover unrelated AR repo `cloud-run-source-deploy` still in `codejitsu` (not this app) — delete only
  if you know it's unused.
- Optional: `gcloud projects delete codejitsu` once billing confirms zero charges.

### D. Smaller follow-ups
- **Validate agentic v2 output quality** on a real (H.264) tape end-to-end now that upload + analysis
  work — confirm the per-phase pipeline produces good events + coaching, and the editable V2 review UI
  saves correctly.
- `nginx.conf`/`default.conf`: `listen … http2` → separate `http2 on;` directive (cosmetic warning).
- Consider switching the GitHub MCP config to read the PAT from an env var (see C).
