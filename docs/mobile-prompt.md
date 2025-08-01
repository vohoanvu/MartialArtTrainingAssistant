# React Native Mobile App requirements: CodeJitsu AI Sparring Analysis

## 1. Project Overview

### 1.1 Core Concept

This document outlines the requirements for a minimal and focused mobile application, **CodeJitsu AI Sparring Analysis**. The app's primary purpose is to provide Brazilian Jiu-Jitsu (BJJ) students with a simple tool to record their sparring sessions, upload them, and receive immediate, AI-driven analysis and feedback on their performance.

### 1.2 Target User

The target user is a BJJ student at any skill level who owns a smartphone and wants to leverage AI to identify strengths and weaknesses in their sparring (rolling) sessions.

### 1.3 Core User Flow

1.  **Authenticate**: The user registers or logs into their account using Google SSO.
2.  **Record**: The user records a sparring video using their phone's camera.
3.  **Upload**: The user provides basic details and uploads the video for analysis.
4.  **Review**: The user views a detailed breakdown of the AI-generated feedback for their session.

## 2. Design and UI/UX Guidelines

The mobile app's interface must be **clean, minimal, and highly intuitive**. The aesthetic should be inspired by modern component libraries similar to **ShadcnUI**, prioritizing clarity and ease of use over complex visual elements.
### 2.1 UI Framework
- **Tamagui**: Provides TailwindCSS-like styling and cross-platform components, mimicking ShadCN UI’s aesthetic (`button.tsx`, `input.tsx`).
- **Expo AV**: For video playback in the analysis editor.

-   **Platform**: React Native Expo for cross-platform deployment on Android and iOS.
-   **Styling**: Use a design system similar to TailwindCSS.
-   **Color Palette**:
    -   `background`: A neutral, off-white or light gray (e.g., `#FFFFFF` or `#F9F9F9`).
    -   `foreground`: Dark text for high contrast (e.g., `#111827`).
    -   `card`: A slightly off-background color for card components (e.g., `#FFFFFF` with a subtle border).
    -   `primary`: A single, strong accent color for buttons and interactive elements (e.g., a bold blue or purple).
    -   `muted`: A subtle gray for secondary text and borders (e.g., `#6B7280`).
-   **Components**:
    -   **Buttons**: Solid background color (`primary`), rounded corners, clear text. Should have disabled and loading states.
    -   **Inputs**: Simple, clean with a border, placeholder text, and a clear focus state.
    -   **Cards**: Used to display information chunks (like a video summary or a specific piece of feedback). Should have rounded corners and a light border or shadow.
    -   **Navigation**: Use a simple stack-based navigation. No complex tab bars are needed for this focused MVP.
    -   **Icons**: Use a clean, minimalist icon set (e.g., Feather Icons or Heroicons).

## 3. Screen-by-Screen Breakdown

### 3.1 Screen: Welcome / Sign In

-   **Purpose**: To provide a single, simple entry point for both new and existing users via Google SSO. The backend will handle account creation automatically for first-time users.
-   **Components**:
    -   App Logo.
    -   A brief, welcoming tagline, such as "Analyze your sparring with AI."
    -   A prominent `Button` featuring the Google logo, labeled **"Continue with Google"**. This should be the main call-to-action on the screen.
-   **Flow**:
    1.  The user taps the "Continue with Google" button.
    2.  The app initiates the Google SSO authentication flow.
    3.  After the user authenticates with Google, the backend handles the callback, creating a new user account if one doesn't already exist for that Google account.
    4.  The backend provides a JWT to the mobile app upon success.
    5.  The app securely stores the JWT and navigates the user to the **Video Feed** screen.

### 3.3 Screen: Video Feed (Home Screen)

-   **Purpose**: To display a list of the user's previously uploaded videos and their analysis status. This is the main screen after login.
-   **Components**:
    -   **Header**: "My Sparring Videos".
    -   **Video List**: A vertically scrollable list of `VideoCard` components.
        -   If the list is empty, display a message like: "You haven't uploaded any videos yet. Tap the record button to start!"
    -   **VideoCard Component**:
        -   `Title` of the video.
        -   `Upload Date`.
        -   `Status Badge`: A small tag indicating the status ("Analyzing", "Complete", "Failed"). The color should change based on status (e.g., orange for analyzing, green for complete, red for failed).
        -   Tapping a card with "Complete" status navigates to the **Analysis Result Screen**.
    -   **Floating Action Button (FAB)**: A circular button with a "video camera" icon, fixed to the bottom-right of the screen. Tapping it initiates the video recording flow.

-   **API Call**: `GET /api/video/getall-uploaded` to populate the list.

### 3.4 Screen: Video Recording

-   **Purpose**: To allow the user to record video using the device's camera.
-   **Implementation**: This screen should launch the native camera interface.
-   **Components**:
    -   Camera Viewfinder (full screen).
    -   `Record Button` (a large red circle) to start and stop recording.
    -   A timer display showing the current recording duration.
    -   A "Cancel" button to exit the recording interface without saving.
-   **Flow**: After the user stops the recording, they are automatically navigated to the **Upload Form Screen**, passing the URI of the recorded video file.

### 3.5 Screen: Upload Form

-   **Purpose**: To allow the user to add metadata to their recording and initiate the upload process.
-   **Components**:
    -   **Video Preview**: A component that displays the recorded video with basic playback controls (play/pause).
    -   `Input` for "Video Title" (required).
    -   `Input` (multiline) for "Description" (optional).
    -   `Input` for "Who are you in the video?" (e.g., "Fighter in the blue gi"). This is the `studentIdentifier`.
    -   `Button` labeled "Upload for Analysis".
        -   This button should show a loading spinner and become disabled while the upload is in progress.
    -   `Button` (styled as secondary/outline) labeled "Discard and Record Again".
-   **Flow**: Upon successful upload, the app should navigate back to the **Video Feed** screen, where the new video will appear at the top of the list with an "Analyzing" status.
-   **API Call**: `POST /api/video/upload-sparring` (as `multipart/form-data`).

### 3.6 Screen: Analysis Result

-   **Purpose**: To display the AI-generated feedback in a clear and digestible format.
-   **Components**:
    -   **Header**: Displays the `Video Title`.
    -   **Tab Navigation**: A simple tab navigator with three tabs: "Summary", "Strengths", and "Improvements".
    -   **Tab 1: Summary**
        -   A `Card` component with the heading "Overall Summary".
        -   Displays the `overallDescription` text from the API response.
    -   **Tab 2: Strengths**
        -   A scrollable list of `FeedbackCard` components.
        -   Each card corresponds to an item in the `strengths` array.
        -   **FeedbackCard (Strength) Content**:
            -   **Description**: The `description` field.
            -   **Related Technique**: If `related_technique` exists, display it with a label like "Related Technique:".
    -   **Tab 3: Improvements**
        -   A scrollable list of `FeedbackCard` components.
        -   Each card corresponds to an item in the `areasForImprovement` array.
        -   **FeedbackCard (Improvement) Content**:
            -   **Description**: The `description` field.
            -   **Category**: A tag/badge displaying the `weakness_category`.
            -   **Related Technique**: If `related_technique` exists, display it with a label.
-   **API Call**: `GET /api/video/{videoId}/feedback`

## 4. API Integration & Data Models

The mobile app will interact with the backend via the following REST API endpoints.

### 4.1 Authentication

-   **Authentication Method**: Google Single Sign-On (SSO) is the sole method for user authentication and registration.
-   **API Interaction**:
    -   The mobile app initiates the OAuth 2.0 flow managed by the backend. The user is directed to the Google authentication screen.
    -   The backend endpoint (`GET /api/externalauth/signin-google`) manages the entire process. It receives the callback from Google, retrieves the user's profile (email, name), and performs the following logic:
        -   **If a user with the Google email exists**: It logs them in.
        -   **If no user exists**: It automatically creates a new `Fighter` and `User` record.
    -   Upon successful authentication, the backend returns an `AccessTokenResponse` to the mobile app.
    -   **Response Body**: `{ "tokenType": "Bearer", "accessToken": "string", "expiresIn": 3600, "refreshToken": "string" }` (The app must securely store the `accessToken`).

### 4.2 Video Management

-   **Get Uploaded Videos**: `GET /api/video/getall-uploaded`
    -   **Response Body**: `Array<UploadedVideoDto>`
        ```json
        [
          {
            "id": 123,
            "description": "Sparring session",
            "uploadTimestamp": "2025-07-25T10:00:00Z",
            "aiAnalysisResult": "Complete" // or "Analyzing", "Failed"
          }
        ]
        ```

-   **Upload Sparring Video**: `POST /api/video/upload-sparring`
    -   **Request Type**: `multipart/form-data`
    -   **Form Fields**:
        -   `videoFile`: The video file itself.
        -   `description`: `string` (optional).
        -   `studentIdentifier`: `string` (e.g., "Fighter in blue gi").
        -   `martialArt`: `string` (Default to "BrazilianJiuJitsu_GI").
    -   **Response Body**: `UploadedVideoDto`

-   **Get Analysis Feedback**: `GET /api/video/{videoId}/feedback`
    -   **Response Body**: `AnalysisResultDto`
```json
{
  "id": 1,
  "overallDescription": "The student showed good posture but occasionally exposed their back during transitions. Control of the opponent's hips was a key strength.",
  "strengths": [
    {
      "description": "Effectively maintained posture inside the opponent's closed guard.",
      "related_technique": "Posture in Guard"
    }
  ],
  "areasForImprovement": [
    {
      "description": "When attempting the guard pass, the left arm was posted on the mat, making it vulnerable to an armbar.",
      "weakness_category": "Arm Exposure During Passing",
      "related_technique": "Guard Pass",
      "keywords": "guard pass, armbar, defense"
    }
  ],
  "techniques": [], // Note: These can be ignored for the MVP
  "drills": []      // Note: These can be ignored for the MVP
}
```