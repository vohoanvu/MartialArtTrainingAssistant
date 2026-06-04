# MyCoach MVP – Backend Services PRD
## .NET 10 API & AI Analysis Pipeline

> **Status note (2026-05-06):** This PRD documents the shipped MyCoach MVP (B2C AI video-analysis pipeline). It predates the B2B2C portfolio pivot and the TKNQ "retention OS" positioning shift. For the current product direction (MyGym web SaaS + MyCoach mobile, multi-tenancy, retention engine, Stripe lock-in, App Store IAP for solo MyCoach), see `docs/proposals/feature-expansion-roadmap-v1.md` and ADR-035 / ADR-036 / ADR-037 in `docs/decisions.md`. This PRD remains accurate for the existing AI pipeline; it is **NOT being rewritten** — it is the historical MVP record.

> Companion document to: `MyCoach-mobile-MVP-PRD.md`
> Scope: Backend API, database schema, cloud infrastructure, and the agentic VLM video analysis pipeline required to serve the MyCoach Flutter mobile client.

---

## 1. Executive Summary

| Item | Detail |
|---|---|
| **Backend Role** | Stateless REST API serving the Flutter mobile client. Owns all business logic, AI orchestration, and data persistence. |
| **Core Infrastructure** | .NET 10 Web API · Supabase PostgreSQL · Google Cloud Storage · Vertex AI (Gemini) · Firebase Auth |
| **Critical Pipeline** | A 5-phase agentic VLM pipeline that processes user-uploaded BJJ footage and returns structured, timestamped coaching analysis. |
| **Design Philosophy** | Thin API surface, heavy AI orchestration layer. The mobile client is a display shell; all intelligence lives in the backend. |
| **MVP Constraint** | No web annotator portal, no social features, no model fine-tuning pipeline. Upload → Analyze → Store → Return. |

---

## 2. Authentication Strategy

### 2.1 Decision: Firebase Auth + Supabase PostgreSQL (Separate Concerns)

> **Why not Supabase Auth?** Supabase Auth works well, but Firebase Auth provides out-of-the-box Google Sign-In SDK integration for Flutter with zero custom token exchange code on the mobile side. Since the mobile PRD mandates Google SSO via Firebase Auth, we keep it.

> **Why not Firestore for the database?** The analysis pipeline produces deeply relational, structured data — events belong to analyses, analyses belong to users, drills reference weaknesses. This is a natural fit for PostgreSQL, not a document store. Firestore would require denormalization and complex client-side joins. Supabase free tier gives us a fully managed PostgreSQL instance with no operational overhead.

**The correct MVP split:**

| Concern | Service | Rationale |
|---|---|---|
| **Identity & Sessions** | Firebase Auth (Google Sign-In) | Zero-code Google SSO for Flutter. JWT tokens issued automatically. |
| **Token Verification (Server)** | Firebase Admin SDK (in .NET) | Verify `idToken` from the mobile client on every API request. |
| **All data storage** | Supabase PostgreSQL | Relational structure for users, analyses, events, and drills. |
| **Video / media files** | Google Cloud Storage | Large binary storage; signed URLs for secure direct upload and streaming. |

### 2.2 Authentication Flow

```
Mobile Client                  .NET API                      Firebase Auth
     │                             │                               │
     │── Google Sign-In ──────────────────────────────────────────►│
     │◄── Firebase idToken ─────────────────────────────────────── │
     │                             │                               │
     │── POST /api/auth/session ──►│                               │
     │   { idToken: "..." }        │── VerifyIdTokenAsync() ──────►│
     │                             │◄── DecodedToken (uid, email) ─│
     │                             │                               │
     │                             │── Upsert user in PostgreSQL   │
     │                             │   (firebase_uid, email, ...)  │
     │◄── 200 { user, jwt } ───────│                               │
```

- Every subsequent API call includes the Firebase `idToken` in the `Authorization: Bearer <token>` header.
- The .NET API verifies the token on every request using `FirebaseAuth.DefaultInstance.VerifyIdTokenAsync()`.
- No custom session tokens or refresh logic needed — Firebase handles token lifecycle.
- The PostgreSQL `users` table stores the `firebase_uid` as its primary external reference key.

---

## 3. Technology Stack

| Layer | Technology | Version / Plan |
|---|---|---|
| **API Framework** | .NET Web API | 9 (Native AOT optional for non-AI endpoints) |
| **Runtime / Hosting** | Google Cloud Run | Serverless container — scales to zero |
| **Authentication** | Firebase Auth + Firebase Admin SDK for .NET | Latest stable |
| **Database** | PostgreSQL via Supabase | Free tier (500MB, 2 CPU, 500MB RAM) |
| **ORM** | Dapper (lightweight) or EF Core 9 | Prefer Dapper for explicit SQL control |
| **Video Storage** | Google Cloud Storage (GCS) | Standard storage class |
| **AI / VLM** | Vertex AI — Gemini API | Gemini Flash (Event Logger) + Gemini Pro (Auto-Profiler, Head Coach) |
| **Background Jobs** | Cloud Tasks (GCP) OR in-process `IHostedService` | For async analysis pipeline execution |
| **Logging** | Google Cloud Logging (structured JSON) | Via `Serilog` + GCP sink |

---

## 4. Database Schema (PostgreSQL / Supabase)

### 4.1 Tables Overview

```
users
  └─► analyses (one-to-many)
        └─► match_events       (one-to-many — structured event log from pipeline)
        └─► coaching_report    (one-to-one — final Head Coach synthesis)
              └─► prescribed_drills  (one-to-many)
```

### 4.2 Schema Definitions

```sql
-- Users: synced from Firebase Auth on first login
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    firebase_uid    TEXT NOT NULL UNIQUE,
    email           TEXT NOT NULL,
    display_name    TEXT,
    avatar_url      TEXT,
    belt_level      TEXT CHECK (belt_level IN ('white','blue','purple','brown','black')),
    age             INT,
    weight_kg       NUMERIC(5,1),
    weekly_sessions INT,
    primary_goal    TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Analyses: one per uploaded video session
CREATE TABLE analyses (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status              TEXT NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending','uploading','profiling',
                                              'logging','verifying','coaching','complete','failed')),
    source_type         TEXT NOT NULL CHECK (source_type IN ('local_upload','url_paste','record')),
    source_url          TEXT,                        -- original URL if url_paste
    gcs_raw_video_path  TEXT,                        -- GCS object path of uploaded video
    gcs_thumbnail_path  TEXT,
    video_duration_secs INT,
    student_identifier  TEXT,                        -- user-provided description for Auto-Profiler
    visual_dna          TEXT,                        -- Phase I output: immutable athlete profile
    technical_grade     NUMERIC(5,2),                -- 0–100 score from Head Coach
    grade_label         TEXT,                        -- e.g., "Solid Fundamentals"
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

-- Match Events: structured output from Phase 2 (Event Logger), verified by Phase 3 (Event Verifier)
CREATE TABLE match_events (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analysis_id         UUID NOT NULL REFERENCES analyses(id) ON DELETE CASCADE,
    start_timestamp_ms  BIGINT NOT NULL,             -- absolute ms from start of video
    end_timestamp_ms    BIGINT NOT NULL,
    actor               TEXT NOT NULL CHECK (actor IN ('Student','Opponent')),
    technique_category  TEXT NOT NULL,               -- Takedown, Submission, Sweep, Pass, etc. (12 values)
    technique_name      TEXT NOT NULL,               -- specific technique name (free-text)
    position_before     TEXT NOT NULL,               -- position at start of action (13 values)
    position_after      TEXT NOT NULL,               -- position at end of action (13 values)
    outcome             TEXT NOT NULL,               -- Successful, Failed, Partial, Countered, InProgress
    guard_type          TEXT,                         -- nullable: specific guard variant when in guard
    submission_type     TEXT,                         -- nullable: specific submission name
    actions_description TEXT NOT NULL,
    confidence          NUMERIC(3,2) NOT NULL,       -- 0.00-1.00: VLM self-assessed confidence
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Coaching Report: one-to-one with analyses, output from Phase 4 (Head Coach)
CREATE TABLE coaching_reports (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analysis_id     UUID NOT NULL UNIQUE REFERENCES analyses(id) ON DELETE CASCADE,
    match_summary   TEXT NOT NULL,
    strengths       JSONB NOT NULL DEFAULT '[]',     -- [{title, explanation, timestamp_start_ms, timestamp_end_ms}]
    weaknesses      JSONB NOT NULL DEFAULT '[]',     -- [{title, explanation, timestamp_start_ms, timestamp_end_ms, severity, scoring_impact?}]
    elite_tip       TEXT,                            -- "The 1% Detail" — single expert insight
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Prescribed Drills: output from Head Coach, linked to coaching_reports
CREATE TABLE prescribed_drills (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    report_id       UUID NOT NULL REFERENCES coaching_reports(id) ON DELETE CASCADE,
    drill_name      TEXT NOT NULL,
    instructions    TEXT NOT NULL,
    goal            TEXT NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0
);

-- Indexes
CREATE INDEX idx_analyses_user_id ON analyses(user_id);
CREATE INDEX idx_analyses_status ON analyses(status);
CREATE INDEX idx_match_events_analysis_id ON match_events(analysis_id);
CREATE INDEX idx_match_events_timestamp ON match_events(analysis_id, start_timestamp_ms);
CREATE INDEX idx_match_events_confidence ON match_events(confidence);
```

---

## 5. API Surface

### 5.1 Base URL & Versioning

```
https://api.mycoach.app/api/v1/
```

All endpoints require `Authorization: Bearer <firebase_idToken>` unless marked `[Public]`.

### 5.2 Endpoints

#### Auth

| Method | Path | Description |
|---|---|---|
| `POST` | `/auth/session` | Verify Firebase idToken, upsert user in PostgreSQL, return user profile. |

#### User

| Method | Path | Description |
|---|---|---|
| `GET` | `/users/me` | Return authenticated user's profile and stats. |
| `PATCH` | `/users/me` | Update onboarding fields (belt level, weight, training frequency, goal). |

#### Analyses

| Method | Path | Description |
|---|---|---|
| `POST` | `/analyses/upload-url` | Request a GCS signed upload URL for a local video file. Returns `{ analysisId, uploadUrl }`. |
| `POST` | `/analyses` | Submit analysis job. Body: `{ analysisId, sourceType, sourceUrl?, studentIdentifier }`. Triggers the async pipeline. |
| `GET` | `/analyses` | List all analyses for the authenticated user (summary list for History tab). |
| `GET` | `/analyses/{id}` | Get full analysis result including events, coaching report, and drills. |
| `DELETE` | `/analyses/{id}` | Delete an analysis and associated GCS video. |

#### Pipeline Status (Polling / SSE)

| Method | Path | Description |
|---|---|---|
| `GET` | `/analyses/{id}/status` | Lightweight status poll. Returns `{ status, progressLabel }`. Mobile client polls this during Screen 5 (Processing). |

> **Note:** For MVP, simple polling (every 3s from the mobile client) is sufficient. Server-Sent Events (SSE) or WebSockets can be added post-MVP if latency is a concern.

---

## 6. Core Feature: Agentic VLM Analysis Pipeline v2

> **Architecture Note:** Pipeline v2 replaces the original 5-phase chunking-based design (see ADR-016 in `docs/decisions.md`). Gemini 2.5+/3.1 models support up to 1 hour of video natively — the context window limitation that necessitated chunking no longer exists. This design leverages Gemini's **context caching** (`CachedContent` API) to reuse the uploaded video across all phases at ~90% input cost reduction.

### 6.1 Pipeline Overview

The pipeline uses 4 phases: one VLM call per phase, all sharing a single cached video resource. No FFmpeg chunking, no timestamp rebasing, no deduplication. Identity verification is handled by a dedicated lightweight verification phase instead of relying on thought signature relay across chunks.

```
[Video Upload Complete]
        │
        ▼
┌──────────────────────────┐
│  Phase 1                 │  Auto-Profiler Agent
│  Context Cache Creation  │  Gemini Pro (High Thinking)
│  + Visual DNA Generation │  → Creates: CachedContent resource
│                          │  → Produces: visual_dna (text)
└────────┬─────────────────┘
         │  cache_name
         ▼
┌──────────────────────────┐
│  Phase 2                 │  Event Logger Agent
│  Full-Video Event        │  Gemini Pro (High Thinking)
│  Logging (3-5 FPS)       │  Input: cached video + Visual DNA
│                          │  → Produces: enriched event JSON
└────────┬─────────────────┘
         │  events[]
         ▼
┌──────────────────────────┐
│  Phase 3                 │  Event Verifier Agent (NEW)
│  Identity Verification   │  Gemini Pro (Low Thinking)
│  Pass                    │  Input: cached video + events
│                          │  → Produces: corrected event JSON
└────────┬─────────────────┘
         │  verified_events[]
         ▼
┌──────────────────────────┐
│  Phase 4                 │  Head Coach Agent
│  Video-Aware Coaching    │  Gemini Pro (High Thinking)
│  Synthesis               │  Input: cached video + verified events
│                          │  → Produces: coaching_report JSON
└────────┬─────────────────┘
         │
         ▼
[Delete CachedContent resource]
[Persist to PostgreSQL → analysis.status = 'complete']
```

**Why chunking was eliminated:**
- Gemini 3.1 processes up to 1 hour of video in a single call (our max is 20 min)
- Chunking introduced identity drift at chunk boundaries — the primary accuracy problem
- Timestamp rebasing (Phase IV) was error-prone and added complexity
- Overlap window deduplication produced false merges in fast scramble sequences
- Context caching makes full-video multi-phase processing cost-effective

**Context caching lifecycle:**
1. **Create** in Phase 1 via `GenAiCacheServiceClient.CreateCachedContentAsync()`
2. **Reference** in Phases 2-4 via `request.CachedContent = cacheName`
3. **Delete** after Phase 4 completes (or on pipeline failure in a `finally` block)

### 6.2 Phase 1 — Auto-Profiler + Context Cache Creation

**Purpose:** (a) Create a `CachedContent` resource containing the full video and system context, enabling cost-efficient reuse across all subsequent phases. (b) Generate an immutable Visual DNA profile of the student athlete.

**Model:** Configurable, default `gemini-3-pro` (or `gemini-3.1-pro-preview-preview`)

**FPS:** 1 (configurable) — sufficient for visual identification; higher FPS reserved for Phase 2

**Two operations in this phase:**

#### Operation A: Create Context Cache

```csharp
// Pseudocode: Context cache creation
var cacheServiceClient = new GenAiCacheServiceClient();

var videoUri = $"gs://{bucketName}/{analysis.GcsRawVideoPath}";

var cachedContent = await cacheServiceClient.CreateCachedContentAsync(new CachedContent
{
    Model = $"projects/{projectId}/locations/{location}/publishers/google/models/{modelId}",
    DisplayName = $"analysis-{analysis.Id}",
    Contents =
    {
        new Content
        {
            Role = "user",
            Parts =
            {
                new Part
                {
                    FileData = new FileData
                    {
                        MimeType = "video/mp4",
                        FileUri = videoUri
                    }
                }
            }
        }
    },
    SystemInstruction = new Content
    {
        Parts =
        {
            new Part { Text = SYSTEM_INSTRUCTION }
        }
    },
    ExpireTime = Timestamp.FromDateTime(
        DateTime.UtcNow.AddMinutes(config.ContextCacheTtlMinutes) // default 30
    )
});

string cacheName = cachedContent.Name; // e.g., "projects/.../cachedContents/abc123"
```

The `cacheName` is stored in memory for the pipeline run and passed to Phases 2-4. It is NOT persisted to the database.
> Vertex AI note: any subsequent inference on this video cache must use the same model configuration as the cached model in this step

#### Operation B: Generate Visual DNA

**System Instruction:**
```
You are a Forensic Video Analyst specializing in combat sports footage. Your sole
task is to identify a specific athlete and document their unique visual characteristics
with enough detail that another analyst could unerringly pick them out in any frame.
Ignore all technical grappling actions.
```

**Prompt template:**
```
Watch the opening segment of this video. Locate the athlete matching this description:
'{studentIdentifier}'.

Generate a hyper-detailed, immutable 'Visual DNA' profile of this specific athlete.
Include ALL of the following when visible:
- Gi/rashguard: color, brand, sleeve length, any distinguishing wear/damage
- Belt: color, stripe count, knot style
- Physical: hair color/style, skin tone, body type, approximate height relative to opponent
- Distinguishing marks: visible team patches and their locations, sponsor logos,
  ankle supports, knee braces, athletic tape on fingers or joints, ear guards
- Movement signature: dominant stance (orthodox/southpaw), posture tendency

Return ONLY a single descriptive paragraph. No bullet points. No headers. No analysis.
```

**Output:** Plain text paragraph stored as `analyses.visual_dna`.

**Model Settings:**
```json
{
  "model": "gemini-3.1-pro-preview",
  "temperature": 0.1,
  "max_output_tokens": 1024,
  "thinking_config": { "thinking_budget": 8192 }
}
```

### 6.3 Phase 2 — Event Logger (Full-Video Analysis)

**Purpose:** Analyze the complete video in a single pass, logging all positional changes and technique executions with enriched structured output. No chunking — the full video is accessed via the context cache created in Phase 1.

**Model:** Configurable, default `gemini-3.1-pro-preview`

**FPS:** 3-5 (configurable) — **critical upgrade from v1's 1 FPS**. Fast BJJ scrambles, grip fights, and submission attempts can occur in under 1 second. At 1 FPS these are missed entirely. 3-5 FPS captures the granularity needed for accurate event logging.

**Input:**
- Cached video via `request.CachedContent = cacheName` (no re-upload of video bytes)
- Visual DNA paragraph injected into the prompt

**System Instruction:**
```
You are a meticulous grappling play-by-play logger and an expert Brazilian Jiu-Jitsu
black belt instructor. You are analyzing a complete BJJ match video.

You must strictly track the student matching the provided 'Visual DNA' profile.
CRITICAL: Do not swap identities during scrambles. When uncertain, flag the event
with a lower confidence score rather than guessing.

Focus on: positional changes, technique attempts (successful and failed), sweeps,
passes, submissions, escapes, and transitions. Log what BOTH athletes do, attributing
each action to the correct actor.
```

**Prompt template:**
```
Analyze this complete BJJ match video from start to finish.

=== STUDENT VISUAL DNA ===
{visualDna}

=== IDENTITY REINFORCEMENT ===
Every 30 seconds of video time, re-confirm which athlete matches the Visual DNA
before continuing your analysis. If at any point you lose certainty about which
athlete is the student, set confidence to 0.3 or lower for those events.

=== INSTRUCTIONS ===
Log every significant positional change, technique attempted, technique suffered,
and transition by BOTH the student and their opponent. Use absolute video timestamps
(from the start of the video, not relative to any segment).

For each event, assess your confidence (0.0 to 1.0) in:
- Correct actor attribution (is this really the Student or the Opponent?)
- Correct technique classification
- Correct position identification

Return the lower of these three as the event's confidence score.
```

**Enriched Structured Output Schema** (enforced via Vertex AI `responseSchema`):

This is the **biggest schema change from v1**. New fields support the future HITL annotation portal and provide richer coaching data.

| Field | Change vs v1 | Rationale |
|-------|-------------|-----------|
| `technique_category` | Renamed from `technique_type`, expanded enum (12 values) | Adds: `GuardPull`, `Scramble`, `StandUp`, `GripFight` |
| `technique_name` | **NEW** (free-text) | Specific technique e.g. "Double Leg", "Armbar from Guard". Critical for HITL taxonomy building |
| `position_before` | Replaces `position_scenario` | Where the action starts. Expanded enum (~13 values) with guard types |
| `position_after` | **NEW** | Where the action ends. Enables positional flow analysis |
| `outcome` | **NEW** (enum) | `Successful`, `Failed`, `Partial`, `Countered`, `InProgress` |
| `guard_type` | **NEW** (nullable free-text) | Specific guard variant when in a guard position (e.g. "De La Riva", "Spider", "Butterfly") |
| `submission_type` | **NEW** (nullable free-text) | Specific submission name when technique is Submission (e.g. "Rear Naked Choke", "Armbar") |
| `confidence` | **NEW** (0.0-1.0) | VLM self-assessed confidence. Below threshold → auto-queue for HITL review |
| `chunk_index` | **REMOVED** | No chunks in v2 |

**PositionScenario enum** (~13 values — moderate expansion):
`Standing`, `OpenGuard`, `ClosedGuard`, `HalfGuard`, `SideControl`, `Mount`, `BackControl`, `Turtle`, `KneeOnBelly`, `NorthSouth`, `FiftyFifty`, `Crucifix`, `Scramble`

> Specific guard variants (De La Riva, Spider, Lasso, Butterfly, X-Guard, etc.) are captured in the nullable `guard_type` free-text field. This keeps the enum manageable for VLM accuracy while enabling HITL annotators to add specificity.

**TechniqueCategory enum** (12 values — expanded from 8):
`Takedown`, `Submission`, `Sweep`, `Pass`, `Escape`, `Transition`, `Control`, `Defense`, `GuardPull`, `Scramble`, `StandUp`, `GripFight`

**Outcome enum** (new):
`Successful`, `Failed`, `Partial`, `Countered`, `InProgress`

**Full JSON schema for `responseSchema`:**
```json
{
  "type": "object",
  "required": ["events"],
  "properties": {
    "events": {
      "type": "array",
      "items": {
        "type": "object",
        "required": [
          "start_timestamp_ms", "end_timestamp_ms", "actor",
          "technique_category", "technique_name",
          "position_before", "position_after",
          "outcome", "actions_description", "confidence"
        ],
        "properties": {
          "start_timestamp_ms": {
            "type": "integer",
            "description": "Absolute milliseconds from start of video"
          },
          "end_timestamp_ms": {
            "type": "integer",
            "description": "Absolute milliseconds from start of video"
          },
          "actor": {
            "type": "string",
            "enum": ["Student", "Opponent"]
          },
          "technique_category": {
            "type": "string",
            "enum": [
              "Takedown", "Submission", "Sweep", "Pass", "Escape",
              "Transition", "Control", "Defense", "GuardPull",
              "Scramble", "StandUp", "GripFight"
            ]
          },
          "technique_name": {
            "type": "string",
            "description": "Specific technique name, e.g. 'Double Leg', 'Armbar from Guard', 'Knee Slice Pass'"
          },
          "position_before": {
            "type": "string",
            "enum": [
              "Standing", "OpenGuard", "ClosedGuard", "HalfGuard",
              "SideControl", "Mount", "BackControl", "Turtle",
              "KneeOnBelly", "NorthSouth", "FiftyFifty", "Crucifix", "Scramble"
            ],
            "description": "Position at the start of this action"
          },
          "position_after": {
            "type": "string",
            "enum": [
              "Standing", "OpenGuard", "ClosedGuard", "HalfGuard",
              "SideControl", "Mount", "BackControl", "Turtle",
              "KneeOnBelly", "NorthSouth", "FiftyFifty", "Crucifix", "Scramble"
            ],
            "description": "Position at the end of this action"
          },
          "outcome": {
            "type": "string",
            "enum": ["Successful", "Failed", "Partial", "Countered", "InProgress"]
          },
          "guard_type": {
            "type": "string",
            "nullable": true,
            "description": "Specific guard variant if position is a guard (e.g. 'De La Riva', 'Spider', 'Butterfly', 'X-Guard'). Null when not in guard."
          },
          "submission_type": {
            "type": "string",
            "nullable": true,
            "description": "Specific submission name when technique_category is Submission (e.g. 'Rear Naked Choke', 'Armbar', 'Triangle'). Null otherwise."
          },
          "actions_description": {
            "type": "string",
            "description": "Free-text description of what happened"
          },
          "confidence": {
            "type": "number",
            "minimum": 0.0,
            "maximum": 1.0,
            "description": "VLM self-assessed confidence in actor attribution, technique classification, and position identification. Minimum of the three."
          }
        }
      }
    }
  }
}
```

**Model Settings:**
```json
{
  "model": "gemini-3.1-pro-previw",
  "temperature": 0.2,
  "max_output_tokens": 65535,
  "top_p": 1.0,
  "thinking_config": { "thinking_budget": 16384 }
}
```

### 6.4 Phase 3 — Event Verifier (Identity Verification) — NEW

**Purpose:** A lightweight verification pass that re-examines the video at each event's timestamp to verify that the `actor` field correctly matches the Visual DNA. This replaces the thought signature relay mechanism from v1, which was fragile across chunk boundaries.

**Model:** `gemini-3.1-pro-preview` (this phase corrects, it doesn't create - but note that this phase must use the same model as the cached request's model, but it can use low thinking to save cost)

**Why a separate phase?** The Event Logger (Phase 2) processes the video sequentially and may drift during fast scrambles. The Verifier examines each event independently, with the specific timestamp and Visual DNA fresh in context. It's a second pair of eyes, not a second full analysis.

**Input:**
- Cached video via `request.CachedContent = cacheName`
- Complete event list from Phase 2
- Visual DNA paragraph

**System Instruction:**
```
You are a BJJ match identity verification specialist. You are given a list of
timestamped events from a BJJ match and the Visual DNA profile of the student
athlete. Your job is to verify each event by examining the video at the specified
timestamp and confirming:

1. Is the 'actor' field correct? Does the person performing this action match
   (or not match) the Visual DNA?
2. Are 'position_before' and 'position_after' accurate for what you see?
3. Is the 'technique_category' correct?

You may CORRECT any field you find inaccurate. You must LOWER the confidence
score for any event where verification is ambiguous (e.g., camera angle obscures
the view, athletes are tangled and hard to distinguish).

Do NOT add new events. Do NOT remove events. Only correct existing ones.
```

**Prompt template:**
```
=== STUDENT VISUAL DNA ===
{visualDna}

=== EVENTS TO VERIFY ===
{eventsJsonArray}

For each event, examine the video at the specified timestamp range. Verify the
actor attribution against the Visual DNA. Return the complete event list with
any corrections applied.

If an event's actor was wrong, swap it. If a position was misidentified, correct it.
If you cannot verify with confidence (camera angle, athletes obscured), set confidence to 0.3 or lower.
```

**Output:** Corrected event list (same schema as Phase 2 output). Persisted to `match_events` table.

**Model Settings:**
```json
{
  "model": "gemini-3.1-pro-preview",
  "temperature": 0.1,
  "max_output_tokens": 65535,
  "thinking_config": { "thinking_budget": 4096 }
}
```

### 6.5 Phase 4 — Head Coach (Video-Aware Synthesis)

**Purpose:** Produce high-level coaching output: match summary, key strengths, critical weaknesses, prescribed drills, technical grade, and the "1% Detail" elite insight.

**Key change from v1:** The Head Coach now receives the **cached video + enriched verified events**, not just a text-based Master Match Log. This means the coach can reference specific visual moments in its feedback and provide more accurate timestamp-linked coaching.

**Model:** Configurable, default `gemini-3.1-pro-preview`

**Input:**
- Cached video via `request.CachedContent = cacheName`
- Verified events JSON from Phase 3
- User profile data (belt level, experience, training frequency)

**System Instruction:**
```
You are an expert Brazilian Jiu-Jitsu Head Coach and IBJJF Black Belt competitor.
You have access to both the match video and a structured event log verified for
accuracy. Analyze the student's performance and produce actionable coaching feedback.

Prioritize score-losing technical errors. Reference specific moments in the video
when citing strengths and weaknesses. Your feedback should be specific enough that
the student can rewatch the exact moment you're referencing.
```

**Prompt template:**
```
Review your student's BJJ match. You have both the video and the verified event log below.

=== VERIFIED EVENT LOG ===
{verifiedEventsJson}

=== STUDENT PROFILE ===
Belt level: {userBeltLevel}
Experience: {userExperienceLevel}
Stamina: {userStaminaMinutes} minutes
Primary goal: {userPrimaryGoal}
Height: {userHeight} meter
Weight: {userWeight} kilogram

=== INSTRUCTIONS ===
Analyze their overall performance and return structured coaching feedback:

1. match_summary: A narrative paragraph summarizing the match flow and outcome.
2. key_strengths (max 3): Specific things done well, with video timestamps (ms).
3. critical_weaknesses (max 3): Specific errors with video timestamp ranges (ms),
   severity level, and IBJJF scoring impact where applicable.
4. prescribed_drills (exactly 3): Targeted positional sparring drills for the weaknesses.
5. technical_grade: 0–100 score reflecting overall technical execution.
6. grade_label: One-line descriptor (e.g., "Solid Fundamentals", "Raw but Aggressive").
7. elite_tip: ONE highly specific "1% detail" a black belt would notice. Single sentence.

Use millisecond timestamps for all time references so the mobile app can seek directly
to the referenced moment.
```

**Enriched Coaching Report Schema:**

| Field (Weakness) | Change vs v1 | Rationale |
|------------------|-------------|-----------|
| `timestamp_start_ms` | **NEW** (replaces free-text `timestamp_reference`) | Structured timestamp for video seeking in annotator portal and mobile app |
| `timestamp_end_ms` | **NEW** | End of the problematic sequence |
| `severity` | **NEW** enum: `Critical`, `Major`, `Minor` | Prioritization for HITL review + coaching emphasis |
| `scoring_impact` | **NEW** (nullable string) | IBJJF scoring context e.g. "2 points lost — guard pass conceded" |

| Field (Strength) | Change vs v1 | Rationale |
|------------------|-------------|-----------|
| `timestamp_start_ms` | **NEW** | Structured timestamp so the annotator portal can link to video moment |
| `timestamp_end_ms` | **NEW** | End of the highlighted sequence |

**Structured Output Schema:**
```json
{
  "type": "object",
  "required": ["match_summary", "key_strengths", "critical_weaknesses", "prescribed_drills", "technical_grade", "grade_label", "elite_tip"],
  "properties": {
    "match_summary": { "type": "string" },
    "technical_grade": { "type": "number", "minimum": 0, "maximum": 100 },
    "grade_label": { "type": "string" },
    "elite_tip": { "type": "string" },
    "key_strengths": {
      "type": "array",
      "maxItems": 3,
      "items": {
        "type": "object",
        "required": ["title", "explanation", "timestamp_start_ms", "timestamp_end_ms"],
        "properties": {
          "title": { "type": "string" },
          "explanation": { "type": "string" },
          "timestamp_start_ms": { "type": "integer", "description": "Start of the highlighted moment in absolute video ms" },
          "timestamp_end_ms": { "type": "integer", "description": "End of the highlighted moment in absolute video ms" }
        }
      }
    },
    "critical_weaknesses": {
      "type": "array",
      "maxItems": 3,
      "items": {
        "type": "object",
        "required": ["title", "explanation", "timestamp_start_ms", "timestamp_end_ms", "severity"],
        "properties": {
          "title": { "type": "string" },
          "explanation": { "type": "string" },
          "timestamp_start_ms": { "type": "integer", "description": "Start of the problematic sequence in absolute video ms" },
          "timestamp_end_ms": { "type": "integer", "description": "End of the problematic sequence in absolute video ms" },
          "severity": { "type": "string", "enum": ["Critical", "Major", "Minor"] },
          "scoring_impact": {
            "type": "string",
            "nullable": true,
            "description": "IBJJF scoring context, e.g. '2 points lost — guard pass conceded'. Null if not applicable."
          }
        }
      }
    },
    "prescribed_drills": {
      "type": "array",
      "minItems": 3,
      "maxItems": 3,
      "items": {
        "type": "object",
        "required": ["drill_name", "instructions", "goal"],
        "properties": {
          "drill_name": { "type": "string" },
          "instructions": { "type": "string" },
          "goal": { "type": "string" }
        }
      }
    }
  }
}
```

**Model Settings:**
```json
{
  "model": "gemini-3.1-pro-preview",
  "temperature": 0.4,
  "max_output_tokens": 8192,
  "thinking_config": { "thinking_budget": 16384 }
}
```

### 6.6 Context Cache Lifecycle

The context cache is the backbone of v2's cost efficiency. It stores the video once and allows all phases to reference it without re-uploading.

**Creation:** Phase 1, via `GenAiCacheServiceClient.CreateCachedContentAsync()`.

**TTL:** Configurable (default 30 minutes). Must exceed the expected total pipeline duration. For a 20-minute video, the pipeline typically completes in 2-5 minutes, so 30 minutes provides ample margin.

**Deletion:** Explicitly deleted after Phase 4 completes, or on pipeline failure in a `finally` block:

```csharp
// Pseudocode: Pipeline execution with cache cleanup
string? cacheName = null;
try
{
    // Phase 1: creates cache, returns cacheName
    (cacheName, var visualDna) = await RunAutoProfiler(analysis, config);

    // Phase 2: uses cache
    var rawEvents = await RunEventLogger(analysis, cacheName, visualDna, config);

    // Phase 3: uses cache
    var verifiedEvents = await RunEventVerifier(analysis, cacheName, visualDna, rawEvents, config);

    // Phase 4: uses cache
    var report = await RunHeadCoach(analysis, cacheName, verifiedEvents, config);

    await PersistResults(analysis, verifiedEvents, report);
    analysis.Status = AnalysisStatus.Complete;
}
catch (Exception ex)
{
    analysis.Status = AnalysisStatus.Failed;
    analysis.FailureReason = ex.Message;
    throw;
}
finally
{
    if (cacheName != null)
    {
        await cacheServiceClient.DeleteCachedContentAsync(cacheName);
    }
}
```

**Cache expiry mid-pipeline:** If the cache TTL expires while a phase is in progress, the Vertex AI API returns an error. The pipeline should catch this as a retryable `ContextCacheExpiredException` and surface a user-friendly failure message. Consider increasing TTL for longer videos.

**Cost:** Gemini context caching costs ~$1.00-$4.50 per million tokens per hour, prorated to the minute. For a typical 10-minute video (~600K tokens at 3 FPS), the cache cost for a 30-minute TTL is approximately $0.30-$1.35. The savings from not re-uploading the video for phases 2-4 far exceed this cost (~90% reduction in input tokens billed).

### 6.7 Agent Model Selection Reference

| Agent | Phase | Model (default) | Thinking Budget | FPS | Why |
|---|---|---|---|---|---|
| Auto-Profiler | 1 | `gemini-3.1-pro-preview` | 8192 tokens | 1 | Visual precision for athlete identification; also creates the context cache |
| Event Logger | 2 | `gemini-3.1-pro-preview` | 16384 tokens | 3-5 | Full-video analysis requires strong reasoning; higher FPS catches fast scrambles |
| Event Verifier | 3 | `gemini-3.1-pro-preview` | 4096 tokens | N/A (seeks to timestamps) | verification pass — corrects, doesn't generate |
| Head Coach | 4 | `gemini-3.1-pro-preview` | 16384 tokens | N/A (uses cached video) | Synthesis + video-aware coaching requires Pro-level reasoning |

### 6.8 Configuration Reference

All pipeline parameters are configurable via `appsettings.json` under the `VertexAi:Pipeline` section. This allows model IDs, FPS rates, and cost parameters to be tuned without code changes.

```json
{
  "VertexAi": {
    "ProjectId": "mycoach-app",
    "Location": "us-central1",
    "Pipeline": {
      "ContextCacheTtlMinutes": 30,
      "ConfidenceThresholdForReview": 0.7,
      "Phase1_AutoProfiler": {
        "ModelId": "gemini-3.1-pro-preview",
        "Fps": 1,
        "Temperature": 0.1,
        "MaxOutputTokens": 1024,
        "ThinkingBudget": 8192
      },
      "Phase2_EventLogger": {
        "ModelId": "gemini-3.1-pro-preview",
        "Fps": 3,
        "Temperature": 0.2,
        "MaxOutputTokens": 65535,
        "ThinkingBudget": 16384
      },
      "Phase3_EventVerifier": {
        "ModelId": "gemini-3.1-pro-preview",
        "Fps": 0,
        "Temperature": 0.1,
        "MaxOutputTokens": 65535,
        "ThinkingBudget": 4096
      },
      "Phase4_HeadCoach": {
        "ModelId": "gemini-3.1-pro-preview",
        "Fps": 0,
        "Temperature": 0.4,
        "MaxOutputTokens": 8192,
        "ThinkingBudget": 16384
      }
    }
  }
}
```

**Notes:**
- `ModelId`: Use GA (generally available) models for production. Preview models (e.g., `gemini-3.1-pro-preview-preview`) may be used for experimentation but have no SLA.
- `Fps`: Set to 0 for phases that don't perform frame-by-frame analysis (Phases 3 and 4 seek to specific timestamps or rely on the cached video).
- `ConfidenceThresholdForReview`: Events with `confidence` below this value are candidates for HITL review in the future annotation portal.
- `ThinkingBudget`: Number of thinking tokens allocated. Higher values enable deeper reasoning but increase latency and cost.

### 6.9 Fallback: Long Video Chunking

For videos exceeding 30 minutes (above our current 20-minute MVP limit), the old chunking strategy MAY be re-enabled as a fallback. This is gated on `VideoDurationSecs > 1800`.

**This is NOT implemented in MVP.** Document as a future consideration:
- If Gemini's context window or context caching limits are reached for very long videos, the pipeline can fall back to chunking
- The chunking service code (`VideoChunkingService`) is retained but not invoked in the v2 pipeline
- If re-enabled, it would insert between Phase 1 and Phase 2, with Phase 2 running per-chunk and Phase 3 verifying cross-chunk identity consistency
- Requires re-adding `chunk_index` to the schema and implementing a merge step

---

## 7. Video Upload Flow (GCS Integration)

### 7.1 Local Upload (Device File)

```
Mobile Client                       .NET API                     GCS
     │                                  │                          │
     │── POST /analyses/upload-url ─────►│                          │
     │   { fileName, fileSize, mimeType }│                          │
     │                                  │── GenerateSignedUrl() ──►│
     │                                  │◄── signedUploadUrl ───── │
     │◄── 200 { analysisId, uploadUrl } ─│                          │
     │                                  │                          │
     │── PUT {uploadUrl} (video bytes) ─────────────────────────── ►│
     │◄── 200 OK ──────────────────────────────────────────────── ─│
     │                                  │                          │
     │── POST /analyses ────────────────►│                          │
     │   { analysisId, sourceType:       │                          │
     │     'local_upload',               │── Enqueue pipeline job   │
     │     studentIdentifier }           │                          │
     │◄── 202 Accepted { analysisId } ──│                          │
```

- Signed URL expiry: **15 minutes**.
- GCS path: `videos/{userId}/{analysisId}/raw.{ext}`
- Accepted MIME types: `video/mp4`, `video/quicktime`.
- Max file size enforced at signed URL generation: **2GB** (MVP limit).

### 7.2 URL Paste (Instagram / TikTok / YouTube) (IGNORE THIS FEATURE FOR MVP, IT'S NOT REQUIRED FOR MVP!!!!!!)

- The `.NET API` receives the URL from the mobile client.
- The API uses a server-side HTTP client to attempt a direct video stream download (using RapidAPI scraping service).
- If direct download fails (private/auth-gated), the analysis fails with a clear error: `{ error: "VIDEO_UNAVAILABLE", message: "This link requires login or is private." }`.
- Once downloaded, the video is uploaded to GCS under the same path convention.

---

## 8. Pipeline Execution Strategy

### 8.1 Async Execution (MVP Approach)

The analysis pipeline is long-running (estimated 30–120 seconds for a full match). It must not block the HTTP request thread.

**MVP implementation:** Use .NET `IHostedService` with a simple in-memory queue (backed by `Channel<T>`). After `POST /analyses` is received, the job is enqueued and a `202 Accepted` is returned immediately. The mobile client polls `GET /analyses/{id}/status` every 3 seconds.

**Status transitions (Pipeline v2):**
```
pending → uploading → profiling → logging → verifying → coaching → complete
                                                                    ↘ failed
```

> **v2 change:** Removed `chunking` and `merging` statuses (no longer applicable). Added `verifying` for the new Phase 3 identity verification pass.

Each transition updates `analyses.status` in PostgreSQL. The `progressLabel` field returned by the status endpoint maps to the mobile client's loading screen messages.

| DB Status | Mobile Label |
|---|---|
| `uploading` | "Uploading footage..." |
| `profiling` | "Identifying your athlete..." |
| `logging` | "Analyzing positions & transitions..." |
| `verifying` | "Verifying identity tracking..." |
| `coaching` | "Crafting your coach notes..." |
| `complete` | *(Navigate to Dashboard)* |
| `failed` | "Analysis failed — please try again." |

### 8.2 Error Handling

- If any Phase fails, set `analyses.status = 'failed'` with an error message stored in a `failure_reason` column.
- Partial results (e.g., Phase III completed but Phase V failed) should be retained in the database for debugging.
- The mobile client displays a retry CTA on failure. Retry re-uses the existing `analysisId` and GCS video — no re-upload required.

---

## 9. GCS Bucket Structure

```
mycoach-videos/
├── videos/
│   └── {userId}/
│       └── {analysisId}/
│           ├── raw.mp4              ← original upload
│           └── thumbnail.jpg        ← server-extracted frame
└── temp/
    └── {analysisId}/
        ├── chunk_0.mp4
        ├── chunk_1.mp4
        └── chunk_n.mp4              ← deleted after pipeline completes
```

- **Lifecycle policy:** `temp/` objects auto-deleted after 24 hours (GCS Object Lifecycle rule).
- **Access:** All read access via signed URLs (time-limited). No public bucket access.
- **Thumbnails:** Extracted server-side using `FFMpegCore` from the 5-second mark of the raw video.

---

## 10. Non-Functional Requirements

| Requirement | Target |
|---|---|
| **API Response Time (non-pipeline)** | P95 < 300ms |
| **Pipeline Processing Time (P90)** | ≤ 90 seconds for a 5-minute match |
| **API Availability** | ≥ 99.5% (Cloud Run SLA) |
| **Max Concurrent Pipeline Jobs (MVP)** | 5 (limited by Vertex AI quota on free/dev tier) |
| **Database Connection Pool** | Max 10 connections (Supabase free tier limit) |
| **Video Upload Size Limit** | 2GB |
| **Auth Token Verification** | Every request — no exceptions |
| **Log Retention** | 30 days (Cloud Logging) |

---

## 11. Out of Scope (Backend MVP)

| Feature | Future Phase |
|---|---|
| Web Annotator Portal API | Post-MVP — after mobile launch and pipeline validation |
| VLM Fine-tuning / Feedback Data Pipeline | Post-MVP — requires annotator portal and Golden Dataset |
| Real-time streaming (WebSockets / SSE) | Post-MVP — polling sufficient for MVP |
| RapidAPI / social media scraper integration | Post-MVP — validate direct upload loop first |
| Push notifications (FCM) | Post-MVP — for async job completion alerts |
| Multi-athlete tracking (team accounts) | Post-MVP |
| Rate limiting / abuse protection | Post-MVP — add before public launch |

---

## 12. Post-MVP: Human-in-the-Loop (HITL) Annotation Layer

> A separate PRD will be written for the Annotator Portal when the time comes. This section documents the schema design philosophy and future data model so that the VLM pipeline output (Section 6) is structured to support HITL from day one.

After the mobile MVP is live and the agentic pipeline is producing real-world results, the next phase will introduce a **BJJ Expert Annotator Web Portal** to review, correct, and approve VLM outputs — closing the loop between production AI and continuous model improvement.

### 12.1 Schema Design Philosophy

The enriched schemas in Pipeline v2 (Section 6) are designed with HITL in mind:

- **VLM populates all fields; annotators review/correct, not create from scratch.** Every event comes pre-filled with technique category, positions, outcome, and confidence. The annotator's job is to verify and fix, not to label from a blank slate. This dramatically reduces annotation time per event.

- **`confidence` enables auto-triage.** Events with confidence below the configurable threshold (`ConfidenceThresholdForReview`, default 0.7) are automatically queued for expert review. High-confidence events can be auto-approved, focusing human effort where it matters most.

- **`technique_name` (free-text) captures the VLM's specific guess.** Rather than forcing the VLM to select from a closed taxonomy (which would limit it to techniques it was explicitly trained on), the free-text field captures whatever the model identifies (e.g., "Berimbolo back take", "Darce choke attempt"). A standardized technique taxonomy is built iteratively from annotated data over time, not imposed upfront.

- **`position_before` + `position_after` enable flow analysis.** Annotators can verify not just what happened, but the positional context — enabling future training data that teaches the model about position transitions, not just isolated techniques.

- **Structured timestamps (`timestamp_start_ms`, `timestamp_end_ms`) enable video-linked review.** The annotator portal can seek directly to the relevant video moment, compare the VLM output with what they see, and correct inline.

### 12.2 Future Annotation Data Model

The following fields will be added to `match_events` and `coaching_reports` when the annotator portal is built. They are **NOT added to the database now** — they are documented here so the pipeline v2 schema is forward-compatible.

**MatchEvent annotation fields (future):**

| Field | Type | Description |
|-------|------|-------------|
| `review_status` | string (enum) | `pending_review`, `approved`, `corrected`, `rejected`. Default: `pending_review` for events below confidence threshold, `approved` for events above. |
| `reviewed_by` | UUID (FK) | → annotator_users(id). The expert who reviewed this event. |
| `reviewed_at` | timestamptz | When the review was completed. |
| `original_vlm_output` | jsonb | Snapshot of the VLM output before any corrections. Preserved for training data export. |
| `correction_notes` | text | Annotator explanation of what was wrong and why (e.g., "Camera angle made it look like Student but it was Opponent initiating the sweep"). |

**CoachingReport annotation fields (future):**

| Field | Type | Description |
|-------|------|-------------|
| `review_status` | string (enum) | `pending_review`, `approved`, `corrected`. |
| `reviewed_by` | UUID (FK) | → annotator_users(id) |
| `reviewed_at` | timestamptz | When the review was completed. |
| `original_vlm_output` | jsonb | Snapshot of original coaching report before corrections. |

**Review status enum:**
- `pending_review` — Awaiting expert review (auto-set for low-confidence events)
- `approved` — Expert confirmed VLM output is correct
- `corrected` — Expert modified one or more fields
- `rejected` — Event is invalid (e.g., VLM hallucinated an event that didn't happen)

### 12.3 Fine-Tuning Data Export

Corrected events paired with their original VLM outputs form supervised fine-tuning training pairs:

- **Input:** Video segment (referenced by timestamp range) + prompt template
- **Expected output:** The corrected structured JSON (what the model should have produced)
- **Export format:** JSONL for Vertex AI supervised fine-tuning API

```jsonl
{"input": {"video_uri": "gs://...", "start_ms": 45000, "end_ms": 52000, "prompt": "..."}, "output": {"actor": "Student", "technique_category": "Sweep", "technique_name": "Scissor Sweep", "position_before": "ClosedGuard", "position_after": "Mount", "outcome": "Successful", "confidence": 1.0}}
{"input": {"video_uri": "gs://...", "start_ms": 120000, "end_ms": 128000, "prompt": "..."}, "output": {"actor": "Opponent", "technique_category": "Pass", "technique_name": "Knee Slice Pass", "position_before": "HalfGuard", "position_after": "SideControl", "outcome": "Successful", "confidence": 1.0}}
```

**Data pipeline (future):**
1. Export all `corrected` and `approved` events with their `original_vlm_output` snapshots
2. Generate prompt + video segment pairs
3. Upload to Vertex AI as a supervised fine-tuning dataset
4. Fine-tune the Event Logger model (Phase 2) to reduce identity drift and improve technique classification
5. Evaluate fine-tuned model against a held-out Golden Dataset before promoting to production
