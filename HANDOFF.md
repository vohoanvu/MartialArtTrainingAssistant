# Hand-off — what to work on next

_Last updated: 2026-06-04. Author: infra migration session (codejitsu → MyCoach)._

The GCP project migration is **complete and live** — `thecodejitsu.com` runs on the MyCoach
company project (`project-afa815fe-26c6-40c3-a8b`). Current infrastructure is documented in
`CLAUDE.md` → *Deployment → Current infrastructure*. Read that first for operational gotchas
(keyless ADC, no-`sudo` docker-compose, Cloudflare Origin CA / Full-Strict, app dir
`/home/vohoanvu/app`). Full history: `MIGRATION-PLAN-gcp-codejitsu-to-mycoach.md`.

---

## 1. Decommission the old `codejitsu` project  _(scheduled reminder set ~1 week out)_

The old VM is **stopped but intact** as the rollback path. After a stability window
(~3–7 days of healthy operation on MyCoach), tear it down:

```bash
CONFIRM=DECOMMISSION bash decommission-codejitsu.sh
```
This deletes the old VM, static IP, Artifact Registry repo, firewall rules, secrets, and the
MyCoach staging-backup bucket `martial-art-demo-vids-migrate`.

**Manual steps it can't do (Console UI / external):**
- Delete the **old OAuth client** + **old YouTube API key** in `codejitsu` (APIs & Services → Credentials).
- **Revoke the GitHub PAT** in `.mcp.json` / `.claude/.mcp.json` at https://github.com/settings/tokens
  (it's gitignored and never committed, but appeared in a chat transcript — rotate it). Consider switching
  the GitHub MCP config to read the token from an env var.
- Optionally `gcloud projects delete codejitsu`.

---

## 2. Refactor the Vertex AI video inference into a multi-step pipeline  _(primary feature work)_

**Goal:** improve BJJ video-analysis output quality by replacing the current single monolithic
inference with a staged pipeline.

### Current state (single-shot)
- `VideoAnalysis.Server/Domain/AIServices/GeminiVisionService.cs` → `AnalyzeVideoAsync()` makes
  **one** Vertex `generateContent` call: it passes the whole `gs://` video + a large monolithic
  prompt (`BuildVisionAnalysisPrompt`) and asks for the entire result JSON at once
  (`overall_description`, `techniques_identified`, `strengths`, `areas_for_improvement`,
  `suggested_drills`). Model: `gemini-3.1-pro-preview`, `maxOutputTokens` 65535, `thinkingBudget` 16384.
- Orchestrated by the Hangfire job `Helpers/VideoAnalysisBackgroundJobService.cs` (line ~43), which
  validates the JSON and persists `AiAnalysisResult.AnalysisJson`.
- `Domain/AIServices/AiAnalysisProcessorService.cs` parses that JSON into structured DB rows.
- Entry point: `Controllers/GeminiController.cs` → `AnalyzeVideoAsync(videoId)`.
- DTOs/schema: `Models/Dtos/VideoAnalysisDtos.cs` + `SharedEntities/Models/AIGeneratedData.cs`.

### Proposed multi-step pipeline (starting point — adjust as you learn)
1. **Segment** — one cheap pass: produce a timeline of discrete exchanges/rounds with coarse
   timestamps + positional context. Output `[{start, end, position, summary}]`.
2. **Per-segment analysis** — focused call per segment (use Gemini `videoMetadata`
   `startOffset`/`endOffset` to scope the clip window without re-uploading); extract techniques,
   execution quality, and specific mistakes. Parallelizable.
3. **Synthesize** — aggregate per-segment findings into overall strengths + **prioritized**
   weaknesses (by frequency/impact), mapped to the IBJJF-style weakness categories.
4. **Drills** — given prioritized weaknesses + student profile (skill level, training goal),
   generate targeted drills (can reuse the `SuggestClassCurriculum` prompt pattern).
5. _(optional)_ **Self-critique/verify** — validate JSON against schema, flag hallucinated
   timestamps/techniques, re-prompt the offending stage.

### Why it should help
Tighter per-stage prompts → higher fidelity; parallel per-segment analysis; per-stage model choice
(cheaper model for segmentation, pro for analysis); easier observability/debugging; progress can be
streamed to the client via the existing SignalR `videoAnalysisHub`.

### Implementation notes / constraints
- **No auth changes** — keyless ADC already works for Vertex (validated: `generateContent` → 200).
  Reuse the existing `GeminiVisionService` HTTP/token plumbing (`PostJsonAsync`, `GetAccessTokenAsync`).
- **Keep the final combined JSON shape** that `AiAnalysisProcessorService` + `AIGeneratedData`
  expect — or update the processor/DTOs/migration together (and the frontend that renders results).
- Orchestrate the stages in the Hangfire job (or a new `MultiStepAnalysisService`); keep each Vertex
  call's `maxOutputTokens`/`thinkingBudget` smaller per stage than today's single call.
- Add xUnit coverage mocking each stage (existing tests: `CodeJitsu.Tests/`).
- The prompt builders currently live as `private static` methods in `GeminiVisionService.cs` —
  consider extracting per-stage prompts to keep the class manageable.

### Suggested approach
Use the `backend-developer` agent for the implementation and `qa-tester` for verification (per the
agentic workflow in `CLAUDE.md`). Land it behind a feature flag / new endpoint so the current
single-shot path stays available until the multi-step output is validated as better.

---

## 3. Smaller follow-ups
- Confirm a real **video upload → analysis** end-to-end on MyCoach (keyless GCS upload + signed-URL
  playback + the analysis job) — prerequisites all validated, just needs a real upload.
- `nginx.conf`/`default.conf` emit a deprecation warning: `listen ... http2` → switch to the
  separate `http2 on;` directive (cosmetic).
