# CodeJitsu REST API Reference

> Comprehensive API documentation for building client applications (Flutter, mobile, etc.) against the CodeJitsu backend services.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Authentication](#authentication)
- [API Routing (Nginx)](#api-routing-nginx)
- [FighterManager API](#fightermanager-api)
  - [Fighter Registration & Login](#fighter-registration--login)
  - [Fighter Management](#fighter-management)
  - [Training Sessions](#training-sessions)
  - [Attendance](#attendance)
  - [Waitlist](#waitlist)
  - [Google OAuth SSO](#google-oauth-sso)
  - [Identity Endpoints](#identity-endpoints)
- [VideoSharing API](#videosharing-api)
  - [YouTube Video Sharing](#youtube-video-sharing)
  - [Video Upload (GCS)](#video-upload-gcs)
  - [Video Management](#video-management)
  - [AI Video Analysis (Gemini)](#ai-video-analysis-gemini)
  - [Curriculum Generation](#curriculum-generation)
  - [Matchmaker (AI Pairing)](#matchmaker-ai-pairing)
  - [Grok Live Search](#grok-live-search)
- [MatchMaker API](#matchmaker-api)
- [Data Models & Enumerations](#data-models--enumerations)
- [SignalR Real-Time Hubs](#signalr-real-time-hubs)
- [Error Handling](#error-handling)

---

## Architecture Overview

The backend consists of three .NET 8.0 Web API microservices sharing a single PostgreSQL database via the `SharedEntities` library:

| Service              | Internal Port | Description                                    |
|----------------------|---------------|------------------------------------------------|
| FighterManager.Server| 7080          | Auth, user/fighter profiles, training sessions |
| VideoSharing.Server  | 7081          | Video sharing, upload, AI analysis, curriculum |
| MatchMaker.Server    | 7082          | Fighter pair matching algorithm                |

All services are behind an **Nginx** reverse proxy in production.

---

## Authentication

### JWT Bearer Tokens

All protected endpoints require a JWT token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

**Token Details:**
- Algorithm: HS256
- Access Token Expiry: 3 days (configurable)
- Refresh Token Expiry: 7 days

**Obtaining Tokens:** Use the [Login endpoint](#post-apifighterlogin) or [Google OAuth](#google-oauth-sso).

---

## API Routing (Nginx)

In production, all APIs are accessed through a single domain via Nginx path-based routing:

| Path Prefix       | Routes To           | Service              |
|--------------------|---------------------|----------------------|
| `/api/*`           | `fighter-manager:7080` | FighterManager.Server |
| `/vid/api/*`       | `video-sharing:7081/api` | VideoSharing.Server |
| `/pair/api/*`      | `match-maker:7082/api`  | MatchMaker.Server |
| `/swagger`         | FighterManager Swagger | FighterManager.Server |
| `/vid/swagger`     | VideoSharing Swagger   | VideoSharing.Server |
| `/videoShareHub`   | SignalR Hub            | VideoSharing.Server |
| `/videoAnalysisHub` | SignalR Hub           | VideoSharing.Server |
| `/hangfire`        | Background Job Dashboard | VideoSharing.Server |

**Local Development Ports (no Nginx):**
- FighterManager: `http://localhost:5136`
- VideoSharing: `http://localhost:5137`
- MatchMaker: `http://localhost:5138`

---

## FighterManager API

Base path: `/api`

### Fighter Registration & Login

#### `POST /api/fighter/register`

Register a new fighter with a user account. **No authentication required.**

**Request Body:**
```json
{
  "fighterName": "string (required)",
  "height": 175.0,
  "weight": 80.0,
  "bmi": 26.1,
  "gender": "Male",
  "birthdate": "1990-05-15T00:00:00Z",
  "fighterRole": "Student",
  "maxWorkoutDuration": 60,
  "beltColor": "White",
  "experience": "LessThanTwoYears",
  "email": "user@example.com",
  "password": "MinLength8!"
}
```

**Response `201 Created`:**
```json
{
  "type": "string",
  "title": "string",
  "status": 201,
  "detail": "string",
  "instance": "string",
  "errors": {},
  "createdObject": {
    "id": 1,
    "fighterName": "John Doe",
    "height": 175.0,
    "weight": 80.0,
    "bmi": 26.1,
    "gender": "Male",
    "birthdate": "1990-05-15T00:00:00Z",
    "fighterRole": "Student",
    "maxWorkoutDuration": 60,
    "beltColor": "White",
    "experience": "LessThanTwoYears"
  }
}
```

**Errors:** `400` invalid input or duplicate email.

**Validation Rules:**
- `password`: minimum 8 characters
- `gender`: `"Male"` or `"Female"`
- `fighterRole`: `"Student"` or `"Instructor"`
- `beltColor`: `"None"`, `"White"`, `"Blue"`, `"Purple"`, `"Brown"`, `"Black"`
- `experience`: `"LessThanTwoYears"`, `"FromTwoToFiveYears"`, `"MoreThanFiveYears"`

---

#### `POST /api/fighter/login`

Authenticate and receive JWT tokens. **No authentication required.**

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "MinLength8!",
  "twoFactorCode": null,
  "twoFactorRecoveryCode": null
}
```

**Response `200 OK`:**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOi...",
  "expiresIn": 3600,
  "refreshToken": "eyJhbGciOi..."
}
```

**Errors:** `400` invalid input, `401` wrong credentials, `500` server error.

---

### Fighter Management

All endpoints below require `Authorization: Bearer <token>`.

#### `GET /api/fighter`

Get all fighters.

**Response `200 OK`:** `List<ViewFighterDto>`

---

#### `GET /api/fighter/{id}`

Get fighter by ID.

**Response `200 OK`:**
```json
{
  "id": 1,
  "fighterName": "John Doe",
  "height": 175.0,
  "weight": 80.0,
  "bmi": 26.1,
  "gender": "Male",
  "birthdate": "1990-05-15T00:00:00Z",
  "fighterRole": "Student",
  "maxWorkoutDuration": 60,
  "beltColor": "White",
  "experience": "LessThanTwoYears"
}
```

**Errors:** `404` not found.

---

#### `GET /api/fighter/info`

Get the current authenticated user's profile (user + fighter info).

**Response `200 OK`:**
```json
{
  "email": "user@example.com",
  "isEmailConfirmed": true,
  "fighter": {
    "id": 1,
    "fighterName": "John Doe",
    "belkRank": "White",
    "role": "Student",
    "birthdate": "1990-05-15T00:00:00Z",
    "height": 175.0,
    "weight": 80.0
  }
}
```

**Errors:** `404` user/fighter not found.

---

#### `PUT /api/fighter/{id}`

Update a fighter profile.

**Request Body:**
```json
{
  "fighterName": "string",
  "height": 175.0,
  "weight": 80.0,
  "bmi": 26.1,
  "gender": "Male",
  "birthdate": "1990-05-15T00:00:00Z",
  "fighterRole": "Student",
  "maxWorkoutDuration": 60,
  "beltColor": "Blue",
  "experience": "FromTwoToFiveYears"
}
```

**Response `200 OK`:** `ViewFighterDto`

**Errors:** `400` invalid input, `404` not found.

---

#### `DELETE /api/fighter/{id}`

Delete a fighter.

**Response:** `204 No Content`

**Errors:** `404` not found.

---

### Training Sessions

All endpoints require `Authorization: Bearer <token>`.

#### `GET /api/trainingsession`

Get all training sessions for the authenticated instructor.

**Response `200 OK`:** `List<TrainingSessionDtoBase>`

---

#### `GET /api/trainingsession/{id}`

Get training session details with instructor, students, and curriculum status.

**Response `200 OK`:**
```json
{
  "id": 1,
  "trainingDate": "2025-03-20T10:00:00Z",
  "description": "Morning BJJ fundamentals",
  "capacity": 20,
  "duration": 1.5,
  "status": "Active",
  "targetLevel": "Beginner",
  "instructorId": 5,
  "studentIds": [10, 11, 12],
  "instructor": { "id": 5, "fighterName": "Coach Smith", "..." : "..." },
  "students": [
    { "id": 10, "fighterName": "Student A", "..." : "..." }
  ],
  "isCurriculumGenerated": false,
  "rawFighterPairsJson": null
}
```

**Errors:** `404` not found.

---

#### `POST /api/trainingsession`

Create a new training session. Requires Instructor role.

**Request Body:**
```json
{
  "trainingDate": "2025-03-20T10:00:00Z",
  "description": "Morning BJJ fundamentals",
  "capacity": 20,
  "duration": 1.5,
  "status": "Active",
  "targetLevel": "Beginner",
  "instructorId": 5,
  "studentIds": []
}
```

**Response:** `201 Created` with `TrainingSessionDtoBase`.

**Errors:** `400` invalid input, `404` instructor not found.

---

#### `PUT /api/trainingsession/{id}`

Update a training session.

**Request Body:**
```json
{
  "trainingDate": "2025-03-20T10:00:00Z",
  "description": "Updated description",
  "capacity": 25,
  "duration": 2.0,
  "status": "Active",
  "targetLevel": "Intermediate",
  "instructorId": 5,
  "studentIds": [10, 11, 12, 13]
}
```

**Response `200 OK`:** `GetSessionDetailResponse`

**Errors:** `400`, `404`.

---

#### `PATCH /api/trainingsession/{id}/close`

Close/complete a training session.

**Response:** `204 No Content`

**Errors:** `404`, `500`.

---

#### `DELETE /api/trainingsession/{id}`

Delete a training session.

**Response:** `204 No Content`

**Errors:** `404`, `500`.

---

### Attendance

#### `POST /api/trainingsession/{id}/attendance`

Record attendance for a training session. Auto-creates fighters for walk-ins if not found by name. Requires authorized instructor.

**Request Body:**
```json
{
  "records": [
    {
      "fighterName": "Walk-in Student",
      "birthdate": "2000-01-01T00:00:00Z",
      "weight": 70.0,
      "height": 170.0,
      "beltColor": "White",
      "gender": "Male"
    }
  ]
}
```

**Validation Rules:**
- `fighterName`: required, max 100 characters
- `weight`: range 0–500
- `height`: range 0–300
- `beltColor`: required
- `gender`: required
- No duplicate fighter names in a single request

**Response `200 OK`:**
```json
{
  "success": true,
  "message": "Attendance recorded",
  "updatedSession": { "..." : "..." }
}
```

**Errors:** `400` duplicates or invalid data, `401` not the session's instructor, `404` session not found.

---

#### `DELETE /api/trainingsession/{id}/attendance/remove/{fighterId}`

Remove a fighter from a training session.

**Response:** `204 No Content`

**Errors:** `400`, `404`.

---

### Waitlist

#### `POST /api/fighter/join-waitlist`

Add an email to the waitlist. **No authentication required.**

**Request Body:**
```json
{
  "email": "user@example.com",
  "role": "Student",
  "region": "US-West"
}
```

**Response `200 OK`:**
```json
{
  "message": "Successfully joined the waitlist"
}
```

**Errors:** `400` invalid email, `500` server error.

---

### Google OAuth SSO

#### `GET /api/externalauth/signin-google?returnUrl=/`

Initiates Google OAuth 2.0 sign-in flow. Redirects the browser to Google's login page. On success, auto-creates a user with Fighter profile (Instructor role by default) and redirects back with JWT token in query parameters.

**No authentication required.** This is a browser redirect flow — not suitable for direct API calls from mobile without a WebView or custom scheme handler.

---

### Identity Endpoints

Auto-mapped ASP.NET Identity endpoints at `/api/auth/v1`:

| Method | Endpoint                      | Description              |
|--------|-------------------------------|--------------------------|
| POST   | `/api/auth/v1/register`       | Register user (Identity) |
| POST   | `/api/auth/v1/login`          | Login (Identity)         |
| POST   | `/api/auth/v1/refresh`        | Refresh JWT token        |
| POST   | `/api/auth/v1/logout`         | Logout                   |
| POST   | `/api/auth/v1/2fa-confirmation`| Confirm 2FA             |

> **Note:** The custom `/api/fighter/login` and `/api/fighter/register` endpoints are the recommended auth flow as they handle Fighter profile creation alongside Identity.

---

### Health Check

#### `GET /health`

Returns service health status. **No authentication required.**

---

## VideoSharing API

Base path: `/api/video` (proxied via Nginx at `/vid/api/video`)

All endpoints require `Authorization: Bearer <token>` unless marked `[AllowAnonymous]`.

### YouTube Video Sharing

#### `POST /api/video/metadata`

Share a YouTube video. Fetches metadata from YouTube Data API and saves it.

**Request Body:**
```json
{
  "videoUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
}
```

**Response `200 OK`:**
```json
{
  "id": 1,
  "videoId": "dQw4w9WgXcQ",
  "title": "Video Title",
  "description": "Video description",
  "embedLink": "https://www.youtube.com/embed/dQw4w9WgXcQ",
  "sharedBy": {
    "userId": "abc-123",
    "username": "john@example.com"
  }
}
```

**Errors:** `400` invalid URL, `404` video not found on YouTube.

---

#### `GET /api/video/getall` — **No authentication required**

Get all shared YouTube videos.

**Response `200 OK`:** `List<VideoDetailsResponse>`

---

### Video Upload (GCS)

#### `POST /api/video/upload-sparring`

Upload a sparring video to Google Cloud Storage.

**Request:** `multipart/form-data`

| Field               | Type     | Required | Description                                  |
|----------------------|----------|----------|----------------------------------------------|
| `videoFile`          | File     | Yes      | Video file (max 300 MB)                      |
| `description`        | string   | No       | Video description                            |
| `studentIdentifier`  | string   | Yes      | Identifier (e.g., "Fighter in blue gi")      |
| `martialArt`         | string   | No       | MartialArt enum value                        |

**Supported formats:** `video/mp4`, `video/avi`, `video/mov`, `video/mpeg`, `video/webm`

**Response `200 OK`:**
```json
{
  "videoId": 123,
  "signedUrl": "https://storage.googleapis.com/bucket/path?signature=..."
}
```

**Errors:** `400` file too large or wrong format, `409` duplicate video (returns existing `videoId` + `signedUrl`).

---

#### `POST /api/video/upload-demonstration`

Upload a demonstration video. Same as sparring upload but without `studentIdentifier`.

**Request:** `multipart/form-data`

| Field          | Type   | Required | Description         |
|----------------|--------|----------|---------------------|
| `videoFile`    | File   | Yes      | Video file (max 300 MB) |
| `description`  | string | No       | Video description   |

**Response:** Same as sparring upload.

---

### Video Management

#### `GET /api/video/getall-uploaded`

Get all uploaded videos for the authenticated user.

**Response `200 OK`:** `List<UploadedVideoDto>`
```json
[
  {
    "id": 1,
    "userId": "abc-123",
    "fighterName": "John Doe",
    "studentIdentifier": "Fighter in blue gi",
    "filePath": "gs://bucket/path/video.mp4",
    "uploadTimestamp": "2025-03-15T10:00:00Z",
    "description": "Training sparring round 1",
    "aiAnalysisResult": null,
    "signedUrl": "https://storage.googleapis.com/...",
    "martialArt": "BrazilianJiuJitsu_GI",
    "fighterId": 1
  }
]
```

---

#### `GET /api/video/{videoId}`

Get a single uploaded video by ID.

**Response `200 OK`:** `UploadedVideoDto`

**Errors:** `404` not found.

---

#### `DELETE /api/video/delete-uploaded/{videoId}`

Delete an uploaded video (removes from GCS and database).

**Response `200 OK`:**
```json
{
  "message": "Video with ID 123 deleted successfully"
}
```

**Errors:** `404`, `500`.

---

### AI Video Analysis (Gemini)

#### `POST /api/video/analyze/{videoId}`

Trigger asynchronous AI video analysis via Google Vertex AI (Gemini). Runs as a Hangfire background job.

**Response `202 Accepted`:**
```json
{
  "message": "Video analysis is processing",
  "videoId": 123
}
```

> **Real-time updates:** Subscribe to the `/videoAnalysisHub` SignalR hub to receive progress updates.

---

#### `GET /api/video/{videoId}/feedback`

Get the AI analysis result for a video.

**Response `200 OK`:**
```json
{
  "id": 1,
  "techniques": [
    {
      "id": 1,
      "name": "Armbar",
      "description": "Arm lock from guard position",
      "techniqueType": {
        "id": 1,
        "name": "Submission",
        "positionalScenarioId": 3
      },
      "positionalScenario": {
        "id": 3,
        "name": "Guard"
      },
      "startTimestamp": "00:01:23",
      "endTimestamp": "00:01:45"
    }
  ],
  "drills": [
    {
      "id": 1,
      "name": "Armbar Repetition Drill",
      "focus": "Submission accuracy",
      "duration": "5 minutes",
      "description": "Practice armbar transition from closed guard",
      "relatedTechniqueId": 1,
      "relatedTechniqueName": "Armbar"
    }
  ],
  "strengths": [
    {
      "description": "Good hip movement during guard transitions",
      "relatedTechnique": "Hip Escape",
      "relatedTechniqueId": 2
    }
  ],
  "areasForImprovement": [
    {
      "description": "Grip breaks need more urgency",
      "weaknessCategory": "Guard Retention",
      "relatedTechnique": "Grip Fighting",
      "relatedTechniqueId": 3
    }
  ],
  "overallDescription": "Solid fundamentals with room for improvement in transitions"
}
```

**Errors:** `404` video or analysis not found.

---

#### `PATCH /api/video/{videoId}/analysis`

Update/edit an existing analysis result (partial update).

**Request Body:** `PartialAnalysisResultDto` — same structure as the analysis response, all fields optional. Only provided fields are updated.

**Response `200 OK`:** Updated `AnalysisResultDto`.

**Errors:** `400` null body, `404` analysis not found.

---

#### `GET /api/video/import-ai/{videoId}`

Import and process raw AI analysis JSON into structured database entities.

**Response `200 OK`:**
```json
{
  "message": "AI analysis processed successfully."
}
```

**Errors:** `404` video or analysis not found.

---

### Curriculum Generation

#### `GET /api/video/session/{sessionId}/generate`

Generate an AI-powered training curriculum for a session based on student analysis data.

**Response `200 OK`:** Dynamic JSON structure, typically:
```json
{
  "session_title": "Beginner BJJ Fundamentals",
  "duration": "1.5 hours",
  "target_level_specific_weaknesses_identified": ["Guard retention", "Takedown defense"],
  "warm_up": { "..." : "..." },
  "techniques": [ { "..." : "..." } ],
  "drills": [ { "..." : "..." } ],
  "sparring": { "..." : "..." },
  "cool_down": { "..." : "..." }
}
```

**Errors:** `400` invalid sessionId, `500` generation error.

---

#### `GET /api/video/{sessionId}/curriculum`

Retrieve a previously generated curriculum for a session.

**Response `200 OK`:** Raw curriculum JSON string.

**Errors:** `400`, `500`.

---

### Matchmaker (AI Pairing)

#### `POST /api/video/matchmaker/{sessionId}`

Generate AI-suggested sparring partner pairings for a training session.

**Request Body:**
```json
{
  "studentFighterIds": [10, 11, 12, 13, 14],
  "instructorFighterId": 5
}
```

**Response `200 OK`:**
```json
{
  "suggestedPairings": {
    "pairs": [
      {
        "fighter1Id": 10,
        "fighter1Name": "Student A",
        "fighter2Id": 11,
        "fighter2Name": "Student B"
      }
    ],
    "unpairedStudent": {
      "studentId": 14,
      "studentName": "Student E",
      "reason": "Odd number of students; paired with instructor for drilling"
    },
    "pairingRationale": "Pairs matched by weight class and experience level"
  },
  "rawGenerateContentResponseJson": "...",
  "isSuccessfullyParsed": true,
  "errorMessage": null
}
```

**Errors:** `400` invalid fighter IDs or session not found, `500` generation failure.

---

### Grok Live Search

#### `POST /api/grok/search`

Search for instructional videos and resources using xAI (Grok) live web search.

**Request Body:**
```json
{
  "techniqueName": "Armbar from closed guard",
  "trainingSessionId": 1
}
```

**Response `200 OK`:** Markdown-formatted results containing:
```json
{
  "youtube_videos": [
    {
      "title": "string",
      "description": "string",
      "video_id": "string",
      "publication_date": "string",
      "embed_link": "string"
    }
  ],
  "free_videos": [
    { "title": "string", "link": "string" }
  ],
  "paid_resources": [
    {
      "title": "string",
      "description": "string",
      "web_link": "string",
      "relevance": "string"
    }
  ]
}
```

**Errors:** `404` session not found, `500` xAI API error.

---

## MatchMaker API

Base path: `/api/matchmaker` (proxied via Nginx at `/pair/api/matchmaker`)

#### `POST /api/matchmaker`

Generate fighter pair matchups using the algorithmic (non-AI) matching service. **No authentication required.**

**Request Body:**
```json
{
  "studentFighterIds": [10, 11, 12, 13],
  "instructorFighterId": 5,
  "howManyUniquePairs": 3
}
```

**Response `200 OK`:**
```json
[
  {
    "Fighter1": "Student A",
    "Fighter2": "Student B"
  },
  {
    "Fighter1": "Student C",
    "Fighter2": "Student D"
  }
]
```

**Matching Algorithm:** Pairs fighters based on minimum difference in weight, BMI, and max workout duration. Handles odd numbers by pairing the highest-ranked fighter with the instructor.

**Errors:** `400` no valid students or instructor found.

---

## Data Models & Enumerations

### Core DTOs

#### ViewFighterDto
```json
{
  "id": "int",
  "fighterName": "string",
  "height": "double (cm)",
  "weight": "double (kg)",
  "bmi": "double?",
  "gender": "Gender enum",
  "birthdate": "DateTime",
  "fighterRole": "FighterRole enum",
  "maxWorkoutDuration": "int (minutes)",
  "beltColor": "BeltColor enum",
  "experience": "TrainingExperience enum"
}
```

#### TrainingSessionDtoBase
```json
{
  "id": "int?",
  "trainingDate": "DateTime?",
  "description": "string?",
  "capacity": "int?",
  "duration": "double? (hours)",
  "status": "SessionStatus enum?",
  "targetLevel": "TargetLevel enum?",
  "instructorId": "int?",
  "studentIds": "List<int>"
}
```

#### UploadedVideoDto
```json
{
  "id": "int",
  "userId": "string",
  "fighterName": "string?",
  "studentIdentifier": "string?",
  "filePath": "string",
  "uploadTimestamp": "DateTime",
  "description": "string?",
  "aiAnalysisResult": "string? (JSON)",
  "signedUrl": "string",
  "martialArt": "string",
  "fighterId": "int"
}
```

### Enumerations

| Enum                | Values                                                                                           |
|---------------------|--------------------------------------------------------------------------------------------------|
| **Gender**          | `Male`, `Female`                                                                                 |
| **FighterRole**     | `Student`, `Instructor`                                                                          |
| **BeltColor**       | `None`, `White`, `Blue`, `Purple`, `Brown`, `Black`                                              |
| **TrainingExperience** | `LessThanTwoYears`, `FromTwoToFiveYears`, `MoreThanFiveYears`                                |
| **SessionStatus**   | `Active`, `Completed`, `Cancelled`                                                               |
| **TargetLevel**     | `Kids`, `Beginner`, `Intermediate`, `Advanced`, `Expert`                                         |
| **MartialArt**      | `None`, `BrazilianJiuJitsu_GI`, `BrazilianJiuJitsu_NO_GI`, `Wrestling`, `Boxing`, `MuayThai`, `Judo`, `Karate`, `Taekwondo`, `Kickboxing`, `Sumo` |
| **VideoType**       | `Shared`, `StudentUpload`, `Demonstration`                                                       |
| **FocusModule**     | `General`, `SelfDefense`, `Competition`                                                          |

> **Note:** Enum values are serialized as strings in JSON (not integers).

---

## SignalR Real-Time Hubs

Both hubs use WebSocket transport with `withCredentials: true`. No explicit hub-level auth — relies on the JWT bearer token passed during connection.

### Video Share Hub — `/videoShareHub`

Receives notifications when videos are shared or uploaded.

**Events (Server -> Client):**

| Event Name | Parameters | Triggered When |
|------------|------------|----------------|
| `ReceiveVideoSharedNotification` | `(string bannerTitle, string videoTitle, string userName)` | YouTube video shared, sparring video uploaded, or demonstration video uploaded |

**Banner title values:**
- `"New Video Shared!"` — YouTube video shared
- `"New Sparring Video Uploaded!"` — sparring video uploaded
- `"New Demonstration Video Uploaded!"` — demonstration video uploaded

### Video Analysis Hub — `/videoAnalysisHub`

Receives notification when async AI video analysis completes.

**Events (Server -> Client):**

| Event Name | Parameters | Triggered When |
|------------|------------|----------------|
| `AnalysisCompleted` | `(int videoId)` | Background AI analysis job finishes |

### Connection Configuration (for Flutter)

```
Transport: WebSockets only
Credentials: withCredentials = true
Auto-reconnect: enabled
Hub URLs (production): wss://thecodejitsu.com/videoShareHub, wss://thecodejitsu.com/videoAnalysisHub
Hub URLs (local dev): ws://localhost:5137/videoShareHub, ws://localhost:5137/videoAnalysisHub
```

Use `signalr_netcore` or `signalr_pure` Flutter package.

---

## Error Handling

All API errors follow a consistent format:

```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "Descriptive error message",
  "instance": "string",
  "errors": {
    "fieldName": ["Validation error message"]
  }
}
```

### Common HTTP Status Codes

| Code | Meaning                                                |
|------|--------------------------------------------------------|
| 200  | Success                                                |
| 201  | Resource created                                       |
| 202  | Accepted (async processing started)                    |
| 204  | Success, no content (DELETE/PATCH)                     |
| 400  | Bad request / validation error                         |
| 401  | Unauthorized (missing or invalid JWT)                  |
| 404  | Resource not found                                     |
| 409  | Conflict (duplicate resource, e.g., duplicate video)   |
| 422  | Unprocessable entity                                   |
| 500  | Internal server error                                  |

---

## JWT Token Details

### Token Claims

The JWT access token contains these claims:

| Claim | JWT Name | Description |
|-------|----------|-------------|
| Subject | `sub` | User ID (AppUserEntity.Id, a GUID string) |
| JWT ID | `jti` | Unique token identifier (new GUID per token) |
| Email | `email` | User's email address |

### Token Lifecycle

| Token | Expiry | Notes |
|-------|--------|-------|
| Access Token | 3 days | Signed with HmacSha256 |
| Refresh Token | 7 days | Same claims, longer expiry |
| Application Cookie | 3 days | Sliding expiration enabled |

### Token Validation (server-side strictness)

- **VideoSharing.Server** is stricter: `ClockSkew = TimeSpan.Zero` (no tolerance), `ValidateLifetime = true`, and returns a `Token-Expired: true` response header when a token is expired.
- **FighterManager.Server** uses default clock skew (5 minutes).
- Both validate: Issuer, Audience, IssuerSigningKey.

### Refresh Flow

Use `POST /api/auth/v1/refresh` (standard ASP.NET Identity endpoint) to refresh expired tokens.

> **Note:** `POST /api/auth/v1/login` is internally redirected to `POST /api/fighter/login` via RouteRedirectMiddleware — they are equivalent.

---

## Enhanced Identity Endpoint

### `GET /api/auth/v1/manage/info`

This standard Identity endpoint is enhanced by `ResponseEnhancementMiddleware` to include fighter data:

**Response `200 OK`:**
```json
{
  "userInfo": {
    "email": "user@example.com",
    "emailConfirmed": true
  },
  "fighterInfo": {
    "id": 1,
    "fighterName": "John Doe",
    "beltRank": "Blue",
    "role": "Student",
    "birthdate": "1990-05-15T00:00:00Z",
    "height": 175.0,
    "weight": 80.0
  }
}
```

This is useful for fetching the current user's profile after login without a separate fighter lookup.

---

## Production Environment

### Base URLs

| Environment | Base URL | Protocol |
|-------------|----------|----------|
| Production | `thecodejitsu.com` | HTTPS only (TLS 1.2+), auto-redirects HTTP |
| Local Docker | `localhost:3000` | HTTP |
| Local Dev (no Nginx) | See per-service ports below | HTTP |

### Per-Service Direct URLs (Local Dev without Nginx)

| Service | URL |
|---------|-----|
| FighterManager | `http://localhost:5136` |
| VideoSharing | `http://localhost:5137` |
| MatchMaker | `http://localhost:5138` |

### Request Size Limits

| Layer | Limit |
|-------|-------|
| Nginx (`client_max_body_size`) | 100 MB |
| VideoSharing backend (`MultipartBodyLengthLimit`) | 300 MB |
| **Effective limit** | **100 MB** (Nginx is the bottleneck) |

### Google OAuth Callback

On successful Google SSO, the backend redirects to:
```
{returnUrl}?token={accessToken}&refreshToken={refreshToken}
```
This is a browser redirect flow. For Flutter mobile, use native `google_sign_in` and exchange the Google ID token server-side instead.

---

## Flutter Integration Notes

### 1. Authentication

- Use `/api/fighter/register` and `/api/fighter/login` as the primary auth flow.
- Store `accessToken` and `refreshToken` securely (`flutter_secure_storage`).
- Use `/api/auth/v1/refresh` to refresh expired tokens.
- Check for `Token-Expired: true` response header — signals that the token needs refreshing.
- No CSRF/anti-forgery tokens required.
- Password requirement: minimum 8 characters, unique email.

### 2. Google OAuth (Mobile)

The web app uses a browser redirect flow (`/api/externalauth/signin-google`) which isn't suitable for mobile. Instead:
- Use the `google_sign_in` Flutter package for native Google Sign-In.
- You will likely need a **new backend endpoint** to accept a Google ID token from mobile and return JWT tokens — the current backend only supports the redirect-based OAuth flow.
- Google Client ID: configured via `GOOGLE_CLIENT_ID` env var.

### 3. API Configuration

- In production, all requests go through `https://thecodejitsu.com` with path-based routing (see [API Routing](#api-routing-nginx)).
- For local dev with Docker, use `http://localhost:3000`.
- For local dev without Docker, hit each service directly on its own port.
- Set `Authorization: Bearer <token>` header for all authenticated requests.

### 4. File Uploads

- Use `multipart/form-data` with the `dio` package (preferred for progress tracking).
- Effective max upload: **100 MB** (Nginx limit).
- Supported video formats: `video/mp4`, `video/avi`, `video/mov`, `video/mpeg`, `video/webm`.
- Duplicate detection: server checks file hash — `409 Conflict` if duplicate.

### 5. SignalR (Real-Time)

- Use `signalr_netcore` Flutter package.
- Transport: WebSockets only.
- Pass JWT token during hub connection.
- Enable auto-reconnect.
- Listen for `ReceiveVideoSharedNotification` and `AnalysisCompleted` events.

### 6. Enum Handling

All enums are serialized as **string values** in JSON, not integers. Your Dart models should map these as string enums matching the exact casing (e.g., `"BrazilianJiuJitsu_GI"`, `"LessThanTwoYears"`).
