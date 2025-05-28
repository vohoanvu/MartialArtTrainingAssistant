# Backend Implementation Plan: CodeJitsu BJJ Martial Art Training Assistant

**Version:** 1.0
**Date:** July 24, 2024

## 1. API Design

This section outlines key API endpoints, HTTP methods, and example request/response payloads for core functionalities. All APIs will be versioned (e.g., `/api/v1/...`).

### 1.1 User & Fighter Management (`FighterManager.Server`)

**1.1.1 Register New Fighter & User**
-   **Endpoint:** `POST /api/fighter/register`
-   **Method:** `POST`
-   **Description:** Registers a new user and creates an associated fighter profile.
-   **Request Payload (`CreateFighterDto`):**
    ```json
    {
      "email": "newstudent@example.com",
      "password": "SecurePassword123!",
      "fighterName": "New Student",
      "height": 5.8, // in feet
      "weight": 160, // in lbs
      "gender": "Male", // Enum: Male, Female
      "birthdate": "1995-06-15T00:00:00Z",
      "fighterRole": "Student", // Enum: Student, Instructor
      "maxWorkoutDuration": 10, // minutes
      "beltColor": "White", // Enum
      "experience": "LessThanTwoYears" // Enum
    }
    ```
-   **Response (Success - 201 Created):** `ViewFighterDto` with details of the created fighter.
-   **Response (Error - 400 Bad Request):** Validation errors (e.g., email already exists, password complexity).

**1.1.2 User Login**
-   **Endpoint:** `POST /api/fighter/login`
-   **Method:** `POST`
-   **Description:** Authenticates a user and returns JWT tokens.
-   **Request Payload (`CustomLoginRequest`):**
    ```json
    {
      "email": "user@example.com",
      "password": "Password123!"
    }
    ```
-   **Response (Success - 200 OK):** `CustomLoginResponse`
    ```json
    {
      "tokenType": "Bearer",
      "accessToken": "your_jwt_access_token",
      "expiresIn": 3600,
      "refreshToken": "your_jwt_refresh_token"
    }
    ```
-   **Response (Error - 401 Unauthorized):** Invalid credentials.

**1.1.3 Get Authenticated Fighter Info**
-   **Endpoint:** `GET /api/fighter/info`
-   **Method:** `GET`
-   **Authorization:** Bearer Token
-   **Description:** Retrieves information for the authenticated user and their associated fighter profile.
-   **Response (Success - 200 OK):**
    ```json
    {
      "email": "user@example.com",
      "isEmailConfirmed": true,
      "fighter": {
        "id": 1,
        "fighterName": "Test User",
        "beltRank": "Blue",
        "role": "Student",
        // ... other fighter fields
      }
    }
    ```

**1.1.4 SSO Callback Handling (Google)**
-   **Endpoint:** `/signin-google-callback` (Handled by `ExternalAuthController`)
-   **Method:** `GET` (typically, depends on provider)
-   **Description:** Handles the callback from Google after successful SSO authentication. Creates a user/fighter if one doesn't exist, signs in the user, and generates JWT tokens. Redirects to frontend with tokens.
-   **Note:** This is an internal callback endpoint, not directly called by the frontend API client after initial redirection.

### 1.2 Training Session Management (`FighterManager.Server`)

**1.2.1 Create Training Session**
-   **Endpoint:** `POST /api/trainingsession`
-   **Method:** `POST`
-   **Authorization:** Bearer Token (Instructor role required)
-   **Request Payload (`TrainingSessionDtoBase`):**
    ```json
    {
      "trainingDate": "2025-07-20T18:00:00Z",
      "description": "Focus on guard passing",
      "capacity": 20,
      "duration": 1.5, // hours
      "status": "Active", // Enum: Active, Completed, Cancelled
      "targetLevel": "Intermediate" // Enum
    }
    ```
-   **Response (Success - 201 Created):** `TrainingSessionDtoBase` (echoing created session).

**1.2.2 Get Training Session Details**
-   **Endpoint:** `GET /api/trainingsession/{id}`
-   **Method:** `GET`
-   **Authorization:** Bearer Token
-   **Response (Success - 200 OK):** `GetSessionDetailResponse`
    ```json
    {
      "id": 1,
      "trainingDate": "2025-07-20T18:00:00Z",
      "description": "Focus on guard passing",
      // ... other session fields
      "instructor": { /* ViewFighterDto for instructor */ },
      "students": [ /* List of ViewFighterDto for checked-in students */ ]
    }
    ```

**1.2.3 Update Training Session / Student Check-in**
-   **Endpoint:** `PUT /api/trainingsession/{id}`
-   **Method:** `PUT`
-   **Authorization:** Bearer Token
-   **Description:** Allows instructors to update session details or students to check-in.
-   **Request Payload (Student Check-in - `UpdateSessionDetailsRequest`):**
    ```json
    {
      "studentIds": [5] // ID of the student checking in
    }
    ```
-   **Response (Success - 200 OK):** `GetSessionDetailResponse` (updated session).

**1.2.4 Take Walk-in Attendance**
-   **Endpoint:** `POST /api/trainingsession/{id}/attendance`
-   **Method:** `POST`
-   **Authorization:** Bearer Token (Instructor role required)
-   **Request Payload (`TakeAttendanceRequest`):**
    ```json
    {
      "records": [
        {
          "fighterName": "Walk-in Student 1",
          "birthdate": "1998-01-01T00:00:00Z",
          "weight": 150,
          "height": 5.7,
          "beltColor": "White",
          "gender": "Male"
        }
      ]
    }
    ```
-   **Response (Success - 200 OK):** `TakeAttendanceResponse` with updated session details.

### 1.3 Video Management & AI Analysis (`VideoSharing.Server`)

**1.3.1 Upload Sparring/Demonstration Video (GCS)**
-   **Endpoint:** `POST /api/video/upload-sparring` or `POST /api/video/upload-demonstration`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Request Type:** `multipart/form-data`
    -   `videoFile`: The video file.
    -   `description` (form data): Optional video description.
    -   `studentIdentifier` (form data, for sparring): E.g., "Fighter in Blue GI".
    -   `martialArt` (form data): Enum value (e.g., "BrazilianJiuJitsu_GI").
-   **Response (Success - 200 OK):**
    ```json
    {
      "videoId": 123, // DB ID of the VideoMetadata record
      "signedUrl": "https://storage.googleapis.com/your-bucket/..." // GCS signed URL for viewing (optional)
    }
    ```
-   **Response (Error - 409 Conflict):** If duplicate video (based on hash) is detected.
    ```json
    {
      "message": "Duplicate video detected...",
      "videoId": 100, // DB ID of existing video
      "signedUrl": "https://storage.googleapis.com/your-bucket/..."
    }
    ```

**1.3.2 Share YouTube Video Metadata**
-   **Endpoint:** `POST /api/video/metadata`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Request Payload (`SharingVideoRequest`):**
    ```json
    {
      "videoUrl": "https://www.youtube.com/watch?v=someVideoId"
    }
    ```
-   **Response (Success - 200 OK):** `VideoDetailsResponse` (from YouTube API + user info).

**1.3.3 Trigger AI Video Analysis**
-   **Endpoint:** `POST /api/video/analyze/{videoId}`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Description:** Initiates asynchronous AI analysis of the video identified by `videoId`.
-   **Response (Success - 200 OK):** `AiAnalysisResult` (initial or existing, potentially with updated processing status).
    ```json
    {
      "id": 1,
      "videoId": 123,
      "analysisJson": "{...raw JSON from Gemini...}",
      "overallDescription": "Student shows good posture...",
      // ... other fields
    }
    ```

**1.3.4 Get Video Analysis Feedback**
-   **Endpoint:** `GET /api/video/{videoId}/feedback`
-   **Method:** `GET`
-   **Authorization:** Bearer Token
-   **Response (Success - 200 OK):** `AnalysisResultDto` (processed and structured AI analysis).

**1.3.5 Update Video Analysis (Instructor Edits)**
-   **Endpoint:** `PATCH /api/video/{videoId}/analysis`
-   **Method:** `PATCH`
-   **Authorization:** Bearer Token (Instructor role required)
-   **Request Payload (`PartialAnalysisResultDto`):** Contains only the fields/sections that were modified by the instructor.
    ```json
    {
      "overallDescription": "Updated overall description by instructor.",
      "techniques": [
        { "id": 1, "name": "Corrected Armbar", "description": "Better hip movement." }, // Update existing
        { "name": "New Technique", "description": "Identified by instructor", "startTimestamp": "00:02:10", ... } // Add new
      ]
      // Only include sections that changed
    }
    ```
-   **Response (Success - 200 OK):** `AnalysisResultDto` (fully updated analysis).

### 1.4 Curriculum Recommendation (`VideoSharing.Server`)

**1.4.1 Generate Single-Session Curriculum**
-   **Endpoint:** `GET /api/video/session/{sessionId}/generate`
-   **Method:** `GET` (Can be POST if complex parameters are needed, but GET is simpler for this case if parameters are passed via query or derived server-side from `sessionId`)
-   **Authorization:** Bearer Token (Instructor role required)
-   **Description:** Generates an AI-driven curriculum for the specified session.
-   **Response (Success - 200 OK):** `CurriculumResponse` (as defined in `VideoSharing.Server/Models/Dtos/VideoAnalysisDtos.cs`, mirroring the Gemini output structure).

**1.4.2 Get Stored Curriculum**
-   **Endpoint:** `GET /api/video/{sessionId}/curriculum`
-   **Method:** `GET`
-   **Authorization:** Bearer Token
-   **Description:** Retrieves the previously generated (and stored) raw JSON curriculum for a session.
-   **Response (Success - 200 OK):** Raw JSON string or a parsed `CurriculumResponse`.
-   **Response (204 No Content):** If no curriculum has been generated/stored for the session.

### 1.5 Waitlist (`FighterManager.Server`)
-   **Endpoint:** `POST /api/fighter/join-waitlist`
-   **Method:** `POST`
-   **Request Payload:**
    ```json
    {
      "email": "interested@example.com",
      "role": "Instructor", // Optional
      "region": "North America" // Optional
    }
    ```
-   **Response (Success - 200 OK):**
    ```json
    {
      "message": "Joined waitlist successfully"
    }
    ```

## 2. Data Models (PostgreSQL)

Key tables and their essential fields (refer to `SharedEntities/Models/` for complete definitions):

-   **`app_users` (AspNetUsers extension):**
    -   `Id` (string, PK - from IdentityUser)
    -   `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash` (from IdentityUser)
    -   `FighterId` (int, FK to `Fighters`)
    -   `CreatedAt`, `UpdatedAt` (datetime)

-   **`Fighters`:**
    -   `Id` (int, PK)
    -   `FighterName` (string)
    -   `Height` (double), `Weight` (double), `BMI` (double)
    -   `Gender` (enum: Male, Female)
    -   `Role` (enum: Student, Instructor)
    -   `Birthdate` (datetime)
    -   `MaxWorkoutDuration` (int)
    -   `Experience` (enum: LessThanTwoYears, etc.)
    -   `BelkRank` (enum: White, Blue, etc.)
    -   `IsWalkIn` (bool, default false) - *Added as per SRS FR2.5*

-   **`TrainingSessions`:**
    -   `Id` (int, PK)
    -   `TrainingDate` (datetime)
    -   `Capacity` (int)
    -   `Duration` (double, in hours)
    -   `Status` (enum: Active, Completed, Cancelled)
    -   `TargetLevel` (enum: Beginner, Intermediate, etc.)
    -   `InstructorId` (int, FK to `Fighters`)
    -   `MartialArt` (enum)
    -   `SessionNotes` (string, nullable)
    -   `RawCurriculumJson` (jsonb, nullable) - *Stores AI-generated curriculum*
    -   `EditedCurriculumJson` (jsonb, nullable) - *Stores instructor-modified curriculum*

-   **`TrainingSessionFighterJoints`:** (Many-to-Many for session attendance)
    -   `Id` (int, PK)
    -   `TrainingSessionId` (int, FK to `TrainingSessions`)
    -   `FighterId` (int, FK to `Fighters`)

-   **`VideoMetadata`:**
    -   `Id` (int, PK)
    -   `UserId` (string, FK to `app_users`)
    -   `Type` (enum: Shared, StudentUpload, Demonstration)
    -   `Title` (string, nullable)
    -   `Description` (string, nullable)
    -   `Url` (string, nullable, for YouTube links)
    -   `YoutubeVideoId` (string, nullable)
    -   `FilePath` (string, nullable, for GCS objects, e.g., `gs://bucket-name/object-name`)
    -   `FileHash` (string, nullable, for duplicate detection)
    -   `UploadedAt` (datetime)
    -   `StudentIdentifier` (string, nullable)
    -   `MartialArt` (enum)

-   **`AiAnalysisResults`:**
    -   `Id` (int, PK)
    -   `VideoId` (int, FK to `VideoMetadata`)
    -   `AnalysisJson` (jsonb, raw JSON from Gemini)
    -   `OverallDescription` (text, nullable)
    -   `Strengths` (text, stores JSON array of `Strength` objects)
    -   `AreasForImprovement` (text, stores JSON array of `AreaForImprovement` objects)
    -   `GeneratedAt` (datetime, nullable)
    -   `LastUpdatedAt` (datetime, nullable)
    -   `UpdatedBy` (string, nullable, UserId of instructor who last edited)

-   **`Techniques`:**
    -   `Id` (int, PK)
    -   `Name` (string)
    -   `Description` (string, nullable)
    -   `TechniqueTypeId` (int, FK to `TechniqueTypes`)
    -   `AiAnalysisResultId` (int, FK to `AiAnalysisResults`, nullable, for techniques derived from AI)
    -   `VideoId` (int, FK to `VideoMetadata`, nullable, if technique is manually added and linked to a video but not through full AI analysis)

-   **`TechniqueTypes`:**
    -   `Id` (int, PK)
    -   `Name` (string, e.g., "Takedown", "Submission")
    -   `PositionalScenarioId` (int, FK to `PositionalScenarios`)

-   **`PositionalScenarios`:**
    -   `Id` (int, PK)
    -   `Name` (string, e.g., "Standing", "Guard")

-   **`Drills`:**
    -   `Id` (int, PK)
    -   `Name` (string)
    -   `Description` (text)
    -   `Focus` (string, nullable)
    -   `Duration` (string, e.g., "5 minutes")
    -   `TechniqueId` (int, FK to `Techniques`, for related technique)
    -   `AiAnalysisResultId` (int, FK to `AiAnalysisResults`, nullable, for drills derived from AI)

-   **`VideoSegmentFeedbacks`:**
    -   `Id` (int, PK)
    -   `VideoId` (int, FK to `VideoMetadata`)
    -   `StartTimestamp` (TimeSpan, nullable)
    -   `EndTimestamp` (TimeSpan, nullable)
    -   `TechniqueId` (int, FK to `Techniques`)
    -   `AnalysisJson` (jsonb, nullable, for instructor's specific notes on this segment/technique instance)

-   **`Waitlist`:**
    -   `Id` (int, PK)
    -   `Email` (string)
    -   `Role` (string, nullable)
    -   `Region` (string, nullable)
    -   `JoinedAt` (datetime)

## 4. Business Logic

### 4.1 User & Fighter Registration
-   **FighterManager.Server:**
    -   `FighterController::CreateAsync` receives `CreateFighterDto`.
    -   `FighterRegistrationService::RegisterFighterAsync` handles the transaction:
        1.  Create `Fighter` entity from DTO.
        2.  Save `Fighter` to DB to get `Fighter.Id`.
        3.  Create `AppUserEntity` with `UserName = Email`, `Email`, and link `FighterId`.
        4.  Use `UserManager<AppUserEntity>.CreateAsync(user, password)` to create the Identity user.
        5.  If Identity user creation fails, rollback transaction. Otherwise, commit.

### 4.2 Video Processing and AI Analysis
-   **VideoSharing.Server:**
    -   **GCS Upload (`VideoController::UploadSparringVideoAsync` / `UploadDemonstrationAsync`):**
        1.  Receive `IFormFile` and metadata.
        2.  Validate file size and type.
        3.  Authenticate user.
        4.  Calculate MD5 hash of the video stream.
        5.  Check DB for existing `VideoMetadata` with the same hash. If found, return 409 Conflict with existing video details.
        6.  Upload file to GCS using `GoogleCloudStorageService::UploadFileAsync`, get `gs://` path.
        7.  Create and save `VideoMetadata` record (Type: `StudentUpload` or `Demonstration`, `FilePath`, `FileHash`, `UserId`, etc.).
        8.  Return `videoId` and a short-lived signed URL for viewing.
        9.  (Asynchronously, after response) Trigger AI analysis: call `GeminiController::AnalyzeVideoAsync(videoId)`.
    -   **YouTube URL Sharing (`VideoController::GetVideoMetadata`):**
        1.  Extract YouTube video ID using `YouTubeHelper`.
        2.  Call `YoutubeDataService::GetVideoDetailsAsync` to fetch title, description.
        3.  Create and save `VideoMetadata` record (Type: `Shared`, `YoutubeVideoId`, `Url`, etc.).
        4.  Return video details.
    -   **AI Analysis Triggering (`GeminiController::AnalyzeVideoAsync`):**
        1.  Retrieve `VideoMetadata` (especially `FilePath`, `MartialArt`, `StudentIdentifier`).
        2.  Call `GeminiVisionService::AnalyzeVideoAsync` with video path/URI and a constructed prompt.
        3.  Receive JSON response from Gemini.
        4.  Validate and parse the JSON into `AiAnalysisResultResponse`.
        5.  Create or update `AiAnalysisResult` entity in DB, storing raw JSON and parsed fields (Strengths, AreasForImprovement, OverallDescription).
        6.  Call `AiAnalysisProcessorService::ProcessAnalysisJsonAsync` to further process the raw JSON, creating/linking `Techniques`, `Drills`, `WeaknessCategory`, `VideoSegmentFeedback` entities.
    -   **Instructor Feedback Saving (`GeminiController::UpdateAnalysisAsync`):**
        1.  Receive `PartialAnalysisResultDto`.
        2.  Call `AiAnalysisProcessorService::SavePartialAnalysisResultDtoByVideoId` to:
            -   Load existing `AiAnalysisResult` with related entities.
            -   Update fields based on `partialDto`.
            -   Handle creation/update/deletion of nested `Techniques`, `Drills`, etc.
            -   Update `VideoSegmentFeedback` timestamps.
            -   Set `LastUpdatedAt` and `UpdatedBy` on `AiAnalysisResult`.
            -   Save changes to DB.

### 4.3 Curriculum Recommendation
-   **VideoSharing.Server (`GeminiController::GenerateCurriculumAsync`):**
    -   `CurriculumRecommendationService::GenerateCurriculumAsync`:
        1.  Fetch `TrainingSession` details.
        2.  Fetch `FighterId`s for checked-in students from `TrainingSessionFighterJoints`.
        3.  Fetch `AppUserEntity` and `Fighter` details for these students.
        4.  Identify latest `AiAnalysisResult` for each student (if any).
        5.  Aggregate top 3 common `WeaknessCategory` names from these analyses.
        6.  Construct a detailed prompt for `GeminiVisionService::SuggestClassCurriculum` including weaknesses, student profiles, and session details.
        7.  Receive JSON curriculum from Gemini.
        8.  Store the raw JSON in `TrainingSession.RawCurriculumJson`.
        9.  Parse and return `CurriculumResponse`.

### 4.4 Walk-in Attendance
-   **FighterManager.Server (`TrainingSessionController::TakeAttendance`):**
    -   `AttendanceService::ProcessAttendanceAsync`:
        1.  Validate session and instructor authorization.
        2.  For each `AttendanceRecordDto`:
            -   Check if a `Fighter` with the same name exists.
            -   If not, create a new `Fighter` record with `IsWalkIn = true` and default student values.
            -   If `Fighter` exists or is newly created, check if they are already in `TrainingSessionFighterJoints` for this session. If so, return error.
            -   Create a new `TrainingSessionFighterJoint` record linking the fighter to the session.
        3.  Save all changes.
        4.  Return updated session details.

## 5. Security

### 5.1 Authentication
-   **Mechanism:** ASP.NET Core Identity for user management, coupled with JWT Bearer authentication for API access.
-   **Token Generation:**
    -   `FighterSignInService::GenerateJwtTokenAsync` creates an access token.
    -   `FighterSignInService::GenerateRefreshToken` creates a refresh token.
    -   Tokens include standard claims (sub, jti, email, exp) and are signed with a symmetric key (`Jwt:Key` from config).
-   **Token Validation:**
    -   Standard JWT Bearer middleware validates issuer, audience, signing key, and expiry.
    -   `JWTInvalidateMiddleware` (custom, in VideoSharing.Server) adds an explicit check for token expiry if not handled by default middleware behavior for specific paths.
-   **SSO:**
    -   Google OAuth 2.0 is configured.
    -   `ExternalAuthController::SignInWithGoogle` initiates the Google login flow.
    -   `ExternalAuthController::HandleGoogleTicketReceived` (static callback) processes the ticket from Google:
        -   Retrieves user info (email, name).
        -   Finds or creates an `AppUserEntity` and associated `Fighter` profile.
        -   Signs in the user via `SignInManager`.
        -   Generates JWT access and refresh tokens.
        -   Redirects to the frontend callback URL (`/sso-callback`) with tokens in query parameters.

### 5.2 Authorization
-   **Role-Based:** Controller actions and services use `[Authorize]` attributes, sometimes with specific role requirements (e.g., `[Authorize(Roles = "Instructor")]`).
-   **Ownership/Access Checks:** Backend logic should verify that users can only access/modify data they own or are permitted to (e.g., an instructor can only manage their own sessions, a student can only upload videos for themselves unless an instructor).

### 5.3 API Security
-   **HTTPS:** Enforced in production environments.
-   **Input Validation:** DTOs use data annotations for basic validation. Additional server-side validation is performed in controllers and services.
-   **CORS:** Configured per environment. Development allows any origin; production restricts to configured client application ports/domains.

## 6. Performance Considerations

-   **Asynchronous Operations:** Utilize `async/await` for all I/O-bound operations (database access, external API calls like YouTube/Gemini, GCS operations) to prevent thread blocking and improve scalability. This is generally followed in the existing codebase.
-   **Database Query Optimization:**
    -   **Indexing:** Ensure appropriate database indexes are created for frequently queried columns (e.g., `VideoMetadata.FileHash`, `AiAnalysisResults.VideoId`, `TrainingSessionFighterJoints.TrainingSessionId` and `FighterId`). The initial migration includes GIN indexes for JSONB columns.
    -   **Selective Loading:** Use `Select()` projections to fetch only necessary columns.
    -   **Eager Loading:** Use `Include()` and `ThenInclude()` for related data to avoid N+1 query problems where appropriate.
    -   **AsNoTracking():** Use for read-only queries to improve performance by disabling change tracking.
    -   **Split Queries:** For complex queries with multiple `Include`s, consider using `.AsSplitQuery()` to improve performance.
-   **Long-Running Tasks:**
    -   AI video analysis (`GeminiController::AnalyzeVideoAsync`) and curriculum generation (`GeminiController::GenerateCurriculumAsync`) are inherently long-running. These should be designed to be non-blocking for the client.
        -   The current approach seems to be synchronous for AI analysis trigger but the actual Gemini call is async. The client-side shows a loading state.
        -   Consider implementing a background job/queueing system (e.g., Hangfire, Azure Queues, RabbitMQ) if these tasks become too long or if a more robust retry/failure handling mechanism is needed. For MVP, the current direct async call might be acceptable.
-   **Caching:**
    -   **YouTube API Responses:** Video details from YouTube (title, description) are unlikely to change frequently. Consider caching these responses for a short period (e.g., few hours) to reduce API calls to YouTube.
    -   **Static Data:** Data like `PositionalScenarios`, `TechniqueTypes`, `WeaknessCategories` can be cached in memory on the backend.
-   **Connection Pooling:** Ensure PostgreSQL connection pooling is properly configured (default in EF Core with Npgsql, but verify settings in `AppDb` connection string).
-   **Request/Response Size:**
    -   For large data like `AiAnalysisResult.AnalysisJson`, ensure it's only fetched when necessary. The `GetVideoAnalysisResult` endpoint already structures this.
    -   Implement pagination for list endpoints if they can return a large number of items (e.g., `VideoController::GetAllUploadedVideosAsync`).

## 7. Code Examples (C#)

### 7.1 Fighter Registration (`FighterRegistrationService.cs`)

```csharp
public class FighterRegistrationService
{
    private readonly UserManager<AppUserEntity> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _objectMapper; // AutoMapper for DTO to Entity mapping

    public FighterRegistrationService(UserManager<AppUserEntity> userManager,
        IUnitOfWork unitOfWork, IMapper objectMapper)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _objectMapper = objectMapper;
    }

    public async Task<(IdentityResult identityResult, Fighter? fighter)> RegisterFighterAsync(CreateFighterDto input)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var newFighter = _objectMapper.Map<CreateFighterDto, Fighter>(input);
            // Ensure BMI is calculated if not provided
            if (input.BMI == null && input.Height > 0 && input.Weight > 0)
            {
                // Example BMI calculation (ensure units are consistent or converted)
                // Assuming height in meters and weight in kg for standard BMI
                // If input is ft/lbs, conversion is needed here or in AutoMapper profile
                newFighter.BMI = Math.Round(input.Weight / (input.Height * input.Height), 2); // Adjust if units differ
            }
            newFighter.IsWalkIn = false; // Default for standard registration

            await _unitOfWork.Repository<Fighter>().AddAsync(newFighter);
            await _unitOfWork.SaveChangesAsync(); // Save Fighter to get its ID

            var user = new AppUserEntity
            {
                UserName = input.Email,
                Email = input.Email,
                FighterId = newFighter.Id, // Link to the created Fighter
                EmailConfirmed = true // Consider setting to false and implementing email confirmation flow
            };

            var result = await _userManager.CreateAsync(user, input.Password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return (result, null);
            }

            await transaction.CommitAsync();
            return (IdentityResult.Success, newFighter);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Log the exception (ex)
            var error = new IdentityError { Code = "RegistrationError", Description = $"An unexpected error occurred during registration: {ex.Message}" };
            return (IdentityResult.Failed(error), null);
        }
    }
}
```

### 7.2 AI Video Analysis Trigger (`GeminiController.cs`)

```csharp
[HttpPost("analyze/{videoId}")]
[Authorize]
public async Task<IActionResult> AnalyzeVideoAsync(int videoId)
{
    using var scope = _serviceProvider.CreateScope(); // To resolve scoped services like DbContext
    var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

    var uploadedVideo = await dbContext.Videos
                                     .Include(v => v.AppUser)
                                     .ThenInclude(u => u.Fighter)
                                     .FirstOrDefaultAsync(v => v.Id == videoId);

    if (uploadedVideo == null || uploadedVideo.AppUser?.Fighter == null)
    {
        _logger.LogWarning("Video or associated fighter not found for VideoId {VideoId}", videoId);
        return BadRequest("Invalid VideoId or missing fighter data.");
    }

    if (string.IsNullOrEmpty(uploadedVideo.FilePath))
    {
        _logger.LogWarning("Video FilePath is missing for VideoId {VideoId}", videoId);
        return BadRequest("Video file path is missing, cannot analyze.");
    }

    try
    {
        _logger.LogInformation("Starting AI analysis for VideoId {VideoId}, FilePath: {FilePath}", videoId, uploadedVideo.FilePath);

        var visionAnalysisResult = await _geminiService.AnalyzeVideoAsync(
            uploadedVideo.FilePath,
            uploadedVideo.MartialArt.ToString(),
            uploadedVideo.StudentIdentifier ?? "The primary fighter in the video",
            uploadedVideo.Description ?? "Sparring session footage",
            uploadedVideo.AppUser.Fighter.BelkRank.ToString()
        );

        string? structuredJson = ValidateAndCleanStructuredJson(visionAnalysisResult.AnalysisJson);
        if (structuredJson == null)
        {
            _logger.LogError("Invalid or empty JSON response from Gemini API for VideoId {VideoId}", videoId);
            return StatusCode(500, "Invalid or empty JSON response from the AI analysis service.");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var analysisResponseDto = JsonSerializer.Deserialize<AiAnalysisResultResponse>(structuredJson, options);
        if (analysisResponseDto == null)
        {
             _logger.LogError("Failed to deserialize Gemini JSON response for VideoId {VideoId}. JSON: {Json}", videoId, structuredJson);
            throw new InvalidOperationException("Failed to deserialize JSON response from AI.");
        }

        var existingAiResult = await dbContext.AiAnalysisResults
            .Include(ar => ar.Techniques) // Eager load for potential updates
            .Include(ar => ar.Drills)   // Eager load for potential updates
            .FirstOrDefaultAsync(a => a.VideoId == videoId);

        AiAnalysisResult aiResultToSave;
        if (existingAiResult != null)
        {
            _logger.LogInformation("Updating existing AiAnalysisResult for VideoId {VideoId}.", videoId);
            existingAiResult.AnalysisJson = structuredJson; // Store raw response
            existingAiResult.Strengths = JsonSerializer.Serialize(analysisResponseDto.Strengths ?? new List<Strength>());
            existingAiResult.AreasForImprovement = JsonSerializer.Serialize(analysisResponseDto.AreasForImprovement ?? new List<AreaForImprovement>());
            existingAiResult.OverallDescription = analysisResponseDto.OverallDescription;
            existingAiResult.LastUpdatedAt = DateTime.UtcNow;
            existingAiResult.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier); // Or system if automated
            aiResultToSave = existingAiResult;
        }
        else
        {
            _logger.LogInformation("Creating new AiAnalysisResult for VideoId {VideoId}.", videoId);
            aiResultToSave = new AiAnalysisResult
            {
                VideoId = videoId,
                AnalysisJson = structuredJson,
                Strengths = JsonSerializer.Serialize(analysisResponseDto.Strengths ?? new List<Strength>()),
                AreasForImprovement = JsonSerializer.Serialize(analysisResponseDto.AreasForImprovement ?? new List<AreaForImprovement>()),
                OverallDescription = analysisResponseDto.OverallDescription,
                Techniques = new List<Techniques>(), // To be populated by AiAnalysisProcessorService
                Drills = new List<Drills>(),         // To be populated by AiAnalysisProcessorService
                GeneratedAt = DateTime.UtcNow,
                UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) // Or system if automated
            };
            dbContext.AiAnalysisResults.Add(aiResultToSave);
        }
        await dbContext.SaveChangesAsync(); // Save AiAnalysisResult to get its ID if new

        // Process the structured JSON to populate related entities (Techniques, Drills, etc.)
        var aiAnalysisProcessorService = scope.ServiceProvider.GetRequiredService<AiAnalysisProcessorService>();
        await aiAnalysisProcessorService.ProcessAnalysisJsonAsync(structuredJson, videoId, aiResultToSave.Id); // Pass the ID

        _logger.LogInformation("AI analysis processing complete for VideoId {VideoId}", videoId);
        return Ok(new { message = "AI analysis initiated and preliminary results stored.", analysisId = aiResultToSave.Id });
    }
    catch (JsonException jsonEx)
    {
        _logger.LogError(jsonEx, "JSON deserialization error during AI analysis for VideoId {VideoId}", videoId);
        return StatusCode(500, $"Error processing AI analysis response: {jsonEx.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error analyzing video with VideoId {VideoId}, Path: {FilePath}", videoId, uploadedVideo.FilePath);
        return StatusCode(500, $"Error analyzing video: {ex.Message}");
    }
}

// Helper to clean up potential markdown backticks from Gemini JSON response
private string? ValidateAndCleanStructuredJson(string? apiResponseJson)
{
    if (string.IsNullOrWhiteSpace(apiResponseJson))
        return null;

    string cleanedJson = apiResponseJson.Trim();
    if (cleanedJson.StartsWith("```json"))
    {
        cleanedJson = cleanedJson.Substring(7);
    }
    if (cleanedJson.EndsWith("```"))
    {
        cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
    }
    cleanedJson = cleanedJson.Trim();

    try
    {
        // Test if it's valid JSON
        JsonDocument.Parse(cleanedJson);
        return cleanedJson;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid JSON structure after cleaning. Original: {OriginalJson}, Cleaned: {CleanedJson}", apiResponseJson, cleanedJson);
        return null;
    }
}
```

### 7.3 Curriculum Generation (`CurriculumRecommendationService.cs`)

```csharp
public class CurriculumRecommendationService
{
    private readonly MyDatabaseContext _context;
    private readonly IGeminiVisionService _geminiService;
    private readonly ILogger<CurriculumRecommendationService> _logger;

    public CurriculumRecommendationService(MyDatabaseContext context, IGeminiVisionService geminiService, ILogger<CurriculumRecommendationService> logger)
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<CurriculumResponse> GenerateCurriculumAsync(int sessionId)
    {
        var trainingSession = await _context.TrainingSessions
            .Include(ts => ts.Students!) // Ensure Students are loaded if needed for prompt
                .ThenInclude(tsf => tsf.Fighter)
            .FirstOrDefaultAsync(ts => ts.Id == sessionId);

        if (trainingSession == null)
        {
            _logger.LogWarning("Training session with ID {SessionId} not found for curriculum generation.", sessionId);
            throw new BadHttpRequestException($"Training Session with ID {sessionId} not found.");
        }

        var studentFighterIds = trainingSession.Students?.Select(s => s.FighterId).ToList() ?? new List<int>();
        var studentAppUserIds = await _context.Users
                                   .Where(u => studentFighterIds.Contains(u.FighterId))
                                   .Select(u => u.Id)
                                   .ToListAsync();

        // Fetch latest AI analysis for each student in the session
        var latestAnalyses = await _context.AiAnalysisResults
            .Where(a => studentAppUserIds.Contains(a.Video.UserId) && a.Video.Type == VideoType.StudentUpload)
            .GroupBy(a => a.Video.UserId)
            .Select(g => g.OrderByDescending(a => a.GeneratedAt).FirstOrDefault())
            .Where(a => a != null) // Filter out nulls if a student has no analysis
            .ToListAsync();

        // Extract weakness categories from these analyses
        var allWeaknessDescriptions = new List<string>();
        foreach (var analysis in latestAnalyses)
        {
            if (!string.IsNullOrEmpty(analysis.AreasForImprovement))
            {
                try
                {
                    var areas = JsonSerializer.Deserialize<List<AreaForImprovement>>(analysis.AreasForImprovement);
                    if (areas != null)
                    {
                        allWeaknessDescriptions.AddRange(areas.Select(a => a.WeaknessCategory ?? "").Where(wc => !string.IsNullOrEmpty(wc)));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize AreasForImprovement for AiAnalysisResultId {AnalysisId}", analysis.Id);
                }
            }
        }

        var topWeaknessCategories = allWeaknessDescriptions
            .GroupBy(wc => wc)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(3) // Take top 3 most common weaknesses
            .ToList();
        
        if (!topWeaknessCategories.Any()) // Fallback if no weaknesses found
        {
            topWeaknessCategories.AddRange(new[] { "Guard Retention", "Guard Passing", "Submission Escapes" });
        }

        var studentFightersForPrompt = trainingSession.Students?.Select(s => s.Fighter!).ToList();

        _logger.LogInformation("Generating curriculum for SessionId {SessionId} with weaknesses: {Weaknesses}", sessionId, string.Join(", ", topWeaknessCategories));

        var geminiResponse = await _geminiService.SuggestClassCurriculum(topWeaknessCategories, studentFightersForPrompt, trainingSession);
        string curriculumJson = geminiResponse.CurriculumJson;

        var curriculum = JsonSerializer.Deserialize<CurriculumResponse>(curriculumJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (curriculum == null)
        {
            _logger.LogError("Failed to deserialize curriculum JSON from Gemini for SessionId {SessionId}. JSON: {Json}", sessionId, curriculumJson);
            throw new JsonException("The AI response for curriculum generation was not in the expected format.");
        }

        // Store the raw JSON curriculum
        trainingSession.RawCurriculumJson = curriculumJson;
        trainingSession.EditedCurriculumJson = null; // Clear any previous edits if regenerating
        await _context.SaveChangesAsync();

        _logger.LogInformation("Curriculum generated and stored for SessionId {SessionId}", sessionId);
        return curriculum;
    }
}
```

### 7.4 Walk-in Attendance (`AttendanceService.cs`)

```csharp
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository; // Specific repository for attendance
    private readonly IMapper _mapper;
    private readonly UserManager<AppUserEntity> _userManager; // If new users need to be created
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IMapper mapper,
        UserManager<AppUserEntity> userManager,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _mapper = mapper;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<TakeAttendanceResponse> ProcessAttendanceAsync(
        int sessionId,
        List<AttendanceRecordDto> records,
        string instructorUserId // Authenticated instructor's AppUserEntity.Id
    )
    {
        _logger.LogInformation("Processing attendance for session {SessionId} by instructor {InstructorUserId}", sessionId, instructorUserId);

        var session = await _attendanceRepository.GetSessionWithDetailsAsync(sessionId);
        if (session == null)
        {
            return new TakeAttendanceResponse { Success = false, Message = "Training Session not found." };
        }

        // Authorization check: Ensure the current user is the instructor of this session
        var instructorAppUser = await _attendanceRepository.GetAppUserByFighterIdAsync(session.InstructorId);
        if (instructorAppUser?.Id != instructorUserId)
        {
            return new TakeAttendanceResponse { Success = false, Message = "Unauthorized to modify this session's attendance." };
        }

        var processedFighters = new List<Fighter>();
        foreach (var record in records)
        {
            // Validate record (e.g., name not empty, valid weight/height)
            if (string.IsNullOrWhiteSpace(record.FighterName) || record.Weight <= 0 || record.Height <= 0)
            {
                _logger.LogWarning("Skipping invalid attendance record for session {SessionId}: {FighterName}", sessionId, record.FighterName);
                continue; // Or return an error for the specific record
            }

            var fighter = await GetOrCreateWalkInFighterAsync(record);
            processedFighters.Add(fighter);
        }

        foreach (var fighter in processedFighters)
        {
            var existingJoint = await _attendanceRepository.GetSessionFighterJointAsync(sessionId, fighter.Id);
            if (existingJoint != null)
            {
                _logger.LogInformation("Fighter {FighterId} '{FighterName}' already registered for session {SessionId}", fighter.Id, fighter.FighterName, sessionId);
                continue; // Already registered
            }

            var joint = new TrainingSessionFighterJoint
            {
                TrainingSessionId = sessionId,
                FighterId = fighter.Id
            };
            await _attendanceRepository.AddSessionFighterJointAsync(joint);
        }

        await _attendanceRepository.SaveChangesAsync();

        var updatedSessionDetails = await _attendanceRepository.GetSessionWithDetailsAsync(sessionId);
        return new TakeAttendanceResponse
        {
            Success = true,
            Message = "Attendance recorded successfully.",
            UpdatedSession = _mapper.Map<GetSessionDetailResponse>(updatedSessionDetails)
        };
    }

    private async Task<Fighter> GetOrCreateWalkInFighterAsync(AttendanceRecordDto record)
    {
        // Attempt to find an existing fighter by name (could be more sophisticated, e.g., email if provided)
        var existingFighter = await _attendanceRepository.GetFighterByNameAsync(record.FighterName);
        if (existingFighter != null)
        {
            // Optionally update existing fighter details if they differ and policy allows
            // For now, just return the existing one if it's a walk-in or if we allow re-using profiles
            if (existingFighter.IsWalkIn) {
                 _mapper.Map(record, existingFighter); // Update existing walk-in details
                 await _attendanceRepository.UpdateFighterAsync(existingFighter); // Assuming repository has an update method
            }
            return existingFighter;
        }

        // Create a new Fighter for walk-in
        var newFighter = _mapper.Map<Fighter>(record);
        newFighter.IsWalkIn = true; // Mark as a walk-in
        newFighter.Role = FighterRole.Student; // Default role for walk-ins

        // Note: Walk-in fighters typically won't have an AppUserEntity immediately.
        // If they later register with the same email/name, a linking process might be needed.
        // For simplicity now, we just create the Fighter record.

        return await _attendanceRepository.AddFighterAsync(newFighter);
    }
}
```


    