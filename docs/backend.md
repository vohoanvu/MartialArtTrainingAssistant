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
      "height": 177.8, // cm
      "weight": 72.5, // kg
      "gender": "Male",
      "birthdate": "1995-06-15T00:00:00Z",
      "fighterRole": "Student",
      "maxWorkoutDuration": 60, // minutes
      "beltColor": "White",
      "experience": 0 // Enum: 0=LessThanTwoYears, 1=TwoToFiveYears, 2=MoreThanFiveYears
    }
    ```
-   **Response (Success - 201 Created):** `ViewFighterDto` with details of the created fighter.

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
-   **Response (Success - 200 OK):** `AccessTokenResponse`
    ```json
    {
      "tokenType": "Bearer",
      "accessToken": "your_jwt_access_token",
      "expiresIn": 3600,
      "refreshToken": "your_jwt_refresh_token"
    }
    ```-   **Response (Error - 401 Unauthorized):** Invalid credentials.

**1.1.3 Get Authenticated Fighter Info**
-   **Endpoint:** `GET /api/fighter/info`
-   **Method:** `GET`
-   **Authorization:** Bearer Token
-   **Description:** Retrieves information for the authenticated user and their associated fighter profile.
-   **Response (Success - 200 OK):** `InfoResponse` (schema not fully detailed, but expected structure is below)
    ```json
    {
      "email": "user@example.com",
      "isEmailConfirmed": true,
      "fighter": {
        "id": 1,
        "fighterName": "Test User",
        "beltColor": "Blue",
        "fighterRole": "Student"
      }
    }
    ```

**1.1.4 SSO Callback Handling (Google)**
-   **Endpoint:** `GET /api/externalauth/signin-google`
-   **Method:** `GET`
-   **Description:** Initiates the Google sign-in process. The backend handles the callback from Google after successful SSO authentication. It creates a user/fighter if one doesn't exist, signs in the user, and generates JWT tokens, then redirects to the frontend with tokens.

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
      "status": "Active",
      "targetLevel": "Intermediate",
      "instructorId": 1
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
      "capacity": 20,
      "duration": 1.5,
      "status": "Active",
      "targetLevel": "Intermediate",
      "instructorId": 1,
      "instructor": {
        "id": 1,
        "fighterName": "Master Ken"
      },
      "students": [
        {
          "id": 5,
          "fighterName": "Student Joe"
        }
      ],
      "isCurriculumGenerated": false,
      "rawFighterPairsJson": null
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
      "studentIds": [5, 12] // Array of fighter IDs checking in
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
          "weight": 70,
          "height": 175,
          "beltColor": "White",
          "gender": "Male"
        }
      ]
    }
    ```
-   **Response (Success - 200 OK):** `TakeAttendanceResponse`
    ```json
    {
        "success": true,
        "message": "Attendance recorded successfully.",
        "updatedSession": {
            // Full GetSessionDetailResponse object
        }
    }
    ```

### 1.3 Video Management & AI Analysis (`VideoSharing.Server`)

**1.3.1 Upload Sparring/Demonstration Video**
-   **Endpoint:** `POST /api/video/upload-sparring` or `POST /api/video/upload-demonstration`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Request Type:** `multipart/form-data`
    -   `videoFile`: The video file.
    -   `description` (form data): Optional video description.
    -   `studentIdentifier` (form data, for sparring): E.g., "Fighter in Blue GI".
    -   `martialArt` (form data): Enum value (e.g., "BrazilianJiuJitsu_GI").
-   **Response (Success - 200 OK):** `UploadedVideoDto`
    ```json
    {
      "id": 123,
      "userId": "user-guid-123",
      "fighterName": "Student Joe",
      "studentIdentifier": "Fighter in Blue GI",
      "filePath": "gs://bucket/video.mp4",
      "uploadTimestamp": "2025-07-25T10:00:00Z",
      "description": "Sparring session",
      "signedUrl": "https://storage.googleapis.com/your-bucket/...",
      "martialArt": "BrazilianJiuJitsu_GI",
      "fighterId": 5
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
-   **Response (Success - 200 OK):** `VideoDetailsResponse`
    ```json
    {
        "id": 124,
        "videoId": "someVideoId",
        "title": "Awesome BJJ Technique",
        "description": "A detailed breakdown of the technique.",
        "embedLink": "https://www.youtube.com/embed/someVideoId",
        "sharedBy": {
            "userId": "user-guid-123",
            "username": "user@example.com"
        }
    }
    ```

**1.3.3 Trigger AI Video Analysis**
-   **Endpoint:** `POST /api/video/analyze/{videoId}`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Description:** Initiates asynchronous AI analysis of the video identified by `videoId`.
-   **Response (Success - 200 OK):** `AnalysisResultDto` (initial or existing, potentially with updated processing status).
    ```json
    {
      "id": 1,
      "overallDescription": "Student shows good posture...",
      "strengths": [
          {"description": "Maintained a strong base", "related_technique": "Posture in Guard"}
      ],
      "areasForImprovement": [
          {"description": "Left arm was exposed during the sweep attempt", "weakness_category": "Arm Exposure"}
      ],
      "techniques": [],
      "drills": []
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
        { "id": 1, "name": "Corrected Armbar", "description": "Better hip movement." },
        { "name": "New Technique", "description": "Identified by instructor", "startTimestamp": "00:02:10" }
      ]
    }
    ```-   **Response (Success - 200 OK):** `AnalysisResultDto` (fully updated analysis).

### 1.4 Curriculum Recommendation (`VideoSharing.Server`)

**1.4.1 Generate Single-Session Curriculum**
-   **Endpoint:** `GET /api/video/session/{sessionId}/generate`
-   **Method:** `GET`
-   **Authorization:** Bearer Token (Instructor role required)
-   **Description:** Generates an AI-driven curriculum for the specified session.
-   **Response (Success - 200 OK):** `CurriculumResponse` (A structured JSON object containing warmups, drills, and sparring scenarios).

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
-   **Request Payload (`Waitlist`):**
    ```json
    {
      "email": "interested@example.com",
      "role": "Instructor",
      "region": "North America"
    }
    ```
-   **Response (Success - 200 OK):**
    ```json
    {
      "message": "Joined waitlist successfully"
    }
    ```

### 1.6 Grok Live Search (`VideoSharing.Server`)
-   **Endpoint:** `POST /api/grok/search`
-   **Method:** `POST`
-   **Authorization:** Bearer Token
-   **Description:** Performs a live search for techniques or concepts.
-   **Request Payload (`GrokLiveSearchRequest`):**
    ```json
    {
      "techniqueName": "Armbar from Guard",
      "trainingSessionId": 55
    }
    ```
-   **Response (Success - 200 OK):**
    ```json
    {
      "searchResults": [
        // Response format to be defined
      ]
    }
    ```