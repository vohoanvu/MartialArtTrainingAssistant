# Hand-off — what to work on next

_Last updated: 2026-06-05. Previous: infra migration (codejitsu → MyCoach), 2026-06-04._

`thecodejitsu.com` runs on the MyCoach project (`project-afa815fe-26c6-40c3-a8b`). Operational
gotchas live in `CLAUDE.md` → *Deployment*. **Read "Deploy reality" below before deploying** — the
CI/CD only builds images; the actual VM deploy is manual.

---

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
  gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-c --project=project-afa815fe-26c6-40c3-a8b
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

### A. GCP Transcoder API — H.264 web-playable version (PRIMARY, started, not committed)
**Goal:** make HEVC tapes viewable in-browser by transcoding each upload to H.264/AAC MP4 (managed,
off-VM). Analysis already ingests HEVC fine — this is purely for playback. No code was written yet.

**Design (REST, consistent with `VertexRestClient` — ADC bearer to `transcoder.googleapis.com`):**
- **EF:** `VideoMetadata` += `PlaybackFilePath` (string?), `TranscodeStatus` enum
  (`None/Processing/Ready/Failed`). New migration + apply to Supabase (same flow as
  `AddAgenticPipelineEntities`: `dotnet ef migrations add … --project SharedEntities --startup-project
  FighterManager.Server`, then `database update` with `ConnectionStrings__AppDb` from `.env`).
- **`VideoTranscodeService`:** `CreateJobAsync(inputGcsUri, outputUriPrefix)` → `POST
  https://transcoder.googleapis.com/v1/projects/{project}/locations/{location}/jobs` with JobConfig:
  one H.264 video elementary stream (`heightPixels:720`, omit width = keep aspect, `bitrateBps`,
  `frameRate:30`), one AAC audio stream, one `mp4` mux stream keyed `playback`; `inputUri` =
  `gs://…/original.mp4`, `outputUri` = `gs://martial-art-demo-vids/transcoded/{videoId}/` → output
  lands at `…/transcoded/{videoId}/playback.mp4`. `GetJobAsync(name)` polls `state`
  (`PENDING/RUNNING/SUCCEEDED/FAILED`). Location `us-central1`.
- **Hangfire `VideoTranscodeBackgroundJobService.ProcessTranscodeAsync(videoId)`:** set `Processing`
  → create job → poll (`Task.Delay ~15s`, timeout ~30 min) → on success set `PlaybackFilePath` +
  `Ready` + SignalR `TranscodeReady(videoId)`; on fail set `Failed`. (Own queue or default queue.)
- **Triggers:** auto-enqueue in `analyze-v2` (new uploads) **and** a manual `POST
  /api/video/{videoId}/transcode` (for already-uploaded videos like #10).
- **Read path:** `VideoController.GetUploadedVideoAsync` returns the signed URL of `PlaybackFilePath`
  when `Ready` (else original) and includes `transcodeStatus` in `UploadedVideoDto`.
- **Frontend:** `requestTranscode(videoId)` API; `VideoReview` uses the returned signed URL (playback
  when ready), shows "Converting…" + polls `getVideoDetails` (~10 s) while `Processing`, and the
  `VideoPlayer` HEVC fallback gets a **"Convert for playback"** button (calls `requestTranscode`) when
  `None/Failed`. en/pl i18n.

**Infra prerequisites (MUST run first — IAM/API; the assistant's sandbox blocks these, run manually):**
```bash
P=project-afa815fe-26c6-40c3-a8b
gcloud services enable transcoder.googleapis.com --project=$P
# let the VM SA create jobs:
gcloud projects add-iam-policy-binding $P \
  --member="serviceAccount:codejitsu-vm-runtime@$P.iam.gserviceaccount.com" \
  --role="roles/transcoder.user"
# let the Transcoder service agent read input + write output on the bucket:
NUM=$(gcloud projects describe $P --format='value(projectNumber)')
gcloud storage buckets add-iam-policy-binding gs://martial-art-demo-vids \
  --member="serviceAccount:service-$NUM@gcp-sa-transcoder.iam.gserviceaccount.com" \
  --role="roles/storage.objectAdmin"
```
**Cost:** ~$0.015/min (SD) – $0.06/min (HD) of *output*. **Decision to confirm:** transcode every
upload (simple, uniform) vs only HEVC/on-demand (cheaper, needs the manual-trigger button). Started
leaning: auto on `analyze-v2` + manual endpoint for existing videos. 720p is a sensible default res.

### B. Harden the VM `.env` against CR/LF  _(root cause of the `8d69829` 500)_
The code now trims the GCS/Vertex values, but **other env values still carry `\r`** (e.g. JWT
`iss/aud` = `DEV\r`). Fix `~/app/refresh-env-secret-manager.sh` to strip CR when it writes `.env`
(pipe Secret Manager values through `tr -d '\r'`, or `sed -i 's/\r$//' .env` after writing).
⚠️ If you strip the whole `.env`, JWT `iss/aud` change `DEV\r`→`DEV` → **one-time re-login** for
anyone with an active session (acceptable, but do it knowingly). Then the code trims become belt-and-
suspenders.

### C. Decommission the old `codejitsu` project  _(carried over; stability window has passed)_
Old VM is stopped-but-intact as rollback. Tear down:
```bash
CONFIRM=DECOMMISSION bash decommission-codejitsu.sh
```
Deletes old VM, static IP, AR repo, firewall, secrets, and the MyCoach staging bucket
`martial-art-demo-vids-migrate`. Manual (Console/external): delete old OAuth client + old YouTube key
in `codejitsu`; **rotate the GitHub PAT** in `.mcp.json`/`.claude/.mcp.json`
(https://github.com/settings/tokens — it appeared in a transcript); optionally
`gcloud projects delete codejitsu`.

### D. Smaller follow-ups
- **Validate agentic v2 output quality** on a real (H.264) tape end-to-end now that upload + analysis
  work — confirm the per-phase pipeline produces good events + coaching, and the editable V2 review UI
  saves correctly.
- `nginx.conf`/`default.conf`: `listen … http2` → separate `http2 on;` directive (cosmetic warning).
- Consider switching the GitHub MCP config to read the PAT from an env var (see C).
