# Product Requirements Document: CodeJitsu BJJ Martial Art Training Assistant

## 1. Introduction

This document outlines the product requirements for the CodeJitsu BJJ Training Assistant, a Software-as-a-Service (SaaS) platform. The platform aims to enhance Brazilian Jiu-Jitsu (BJJ) training by providing AI-driven video analysis, class management tools, and curriculum recommendations for BJJ instructors and students.

The core functionalities include secure user registration and authentication (email/password and SSO), class session creation and management by instructors (including walk-in attendance and student pairing), student check-in capabilities, AI-powered analysis of uploaded sparring videos (from GCS or YouTube URLs), an editor for instructors to refine AI analysis, and an MVP feature for single-session AI-driven curriculum recommendations. The system will be accessible via a web application (primarily for instructors) and a mobile application (for in-class use by both instructors and students), operating on a Freemium model.

## 2. Product Specifications

### 2.1 User Management & Authentication

#### 2.1.1 Email/Password Authentication
-   **Feature:** Users can register using an email address and a secure password.
-   **Details:**
    -   Registration form to collect email, password, and user role (Instructor/Student).
    -   Secure login mechanism using email and password credentials.
    -   Password hashing and secure storage practices must be implemented.

#### 2.1.2 Single Sign-On (SSO)
-   **Feature:** Users can register and log in using their existing Google or Facebook accounts.
-   **Details:**
    -   Dedicated "Sign in with Google" button.
    -   Dedicated "Sign in with Facebook" button.
    -   Integration with OAuth 2.0 protocol for secure authentication with Google and Facebook.
    -   Automatic creation of a new user profile or linking to an existing profile based on SSO data (e.g., email address).

#### 2.1.3 Role-Based Access Control (RBAC)
-   **Feature:** The system differentiates between "Instructor" and "Student" roles, granting access to specific features accordingly.
-   **Details:**
    -   Role selection during registration.
    -   Backend enforcement of feature access based on the authenticated user's role.

#### 2.1.4 Fighter Profile
-   **Feature:** Each user account is associated with a "Fighter" profile.
-   **Details:**
    -   Profile stores: Fighter Name, Height, Weight, BMI (auto-calculated or manual entry), Gender, Role (Instructor/Student), Birthdate, Training Experience (e.g., Less than 2 years, 2-5 years, More than 5 years), Belt Rank (e.g., White, Blue, Black), Max Workout Duration (for pairing), and an `IsWalkIn` flag.
    -   Profile data is used for features like student pairing and tailoring AI analysis/curriculum.

### 2.2 Class Session Management (Instructor Features)

#### 2.2.1 Session Creation
-   **Feature:** Instructors can create new training sessions.
-   **Details:**
    -   Form to input session details: Date, Time, Capacity (max number of students), Description/Session Notes, Martial Art (e.g., BJJ GI, BJJ NO-GI), and Target Skill Level (e.g., Beginner, Intermediate, Advanced).
    -   Created sessions are associated with the instructor.

#### 2.2.2 Session Viewing & Management
-   **Feature:** Instructors can view and manage their created sessions.
-   **Details:**
    -   Dashboard/list view of all created sessions.
    -   Ability to view session details, including a list of checked-in students and AI-generated pairings.

#### 2.2.3 Student Pairing
-   **Feature:** The system automatically pairs students for partner-based exercises within a session.
-   **Details:**
    -   Pairing algorithm considers student size (height, weight) and skill level (belt rank).
    -   Generated pairs are displayed to the instructor in the session details view.

#### 2.2.4 Walk-in Student Attendance
-   **Feature:** Instructors can record attendance for students who are not pre-registered for the session (walk-ins).
-   **Details:**
    -   A dedicated interface (e.g., an editable table/form within the "Take Attendance" feature) for instructors to input walk-in student details.
    -   Required fields for walk-in attendance: Fighter Name, Birthdate, Weight, Height, Belt Color, Gender.
    -   Upon submission, the system creates new "Fighter" records for these walk-in students, flagged as `IsWalkIn=true`.
    -   These newly created Fighter records are then associated with the current training session.

#### 2.2.5 Bulk Student Import (Excel)
-   **Feature:** Instructors can import a list of existing students from an Excel spreadsheet.
-   **Details:**
    -   Interface for uploading an Excel file.
    -   The system must validate the spreadsheet format and provide clear instructions on the required columns/data.
    -   The system maps data from the spreadsheet to student Fighter profiles.
    -   Successfully imported students are added to the instructor's student roster and the system database with default roles and profiles.

### 2.3 Student Features

#### 2.3.1 Session Check-in
-   **Feature:** Registered students can check into active class sessions created by an instructor.
-   **Details:**
    -   Interface for students to view available sessions and check themselves in.
    -   Successful check-in adds the student to the session's roster.

#### 2.3.2 Video Upload for AI Analysis
-   **Feature:** Students can submit their sparring videos for AI analysis.
-   **Details:**
    -   **FR3.1.1 Direct Upload:** Interface to upload video files (MP4, AVI specified) directly to Google Cloud Storage.
    -   **FR3.1.2 YouTube URL:** Interface to submit a YouTube video URL.
    -   Students must provide a "student identifier" (e.g., "Fighter in Blue GI") to help the AI focus on them.

### 2.4 AI Video Analysis & Feedback

#### 2.4.1 AI Analysis Generation
-   **Feature:** The system processes submitted videos using Google Vertex AI Gemini Vision for analysis.
-   **Details:**
    -   AI identifies techniques performed, timestamps, strengths, and areas for improvement.
    -   For instructor-uploaded footage of multiple students, AI analyzes both fighters. For self-uploaded videos, AI focuses on the uploader.
    -   Analysis results are stored in PostgreSQL, with techniques in JSONB format and textual descriptions for strengths/weaknesses.

#### 2.4.2 Video Analysis Editor (Instructor Feature)
-   **Feature:** Instructors can review, edit, and approve AI-generated video analysis.
-   **Details:**
    -   A dedicated UI displaying the video player alongside editable fields for AI-generated data (overall description, techniques, strengths, areas for improvement, suggested drills).
    -   Instructors can modify technique names, types, positional scenarios, descriptions.
    -   Instructors can add, edit, or delete identified strengths and areas for improvement.
    -   Instructors can add, edit, or delete suggested drills, including their name, focus, duration, description, and related technique.
    -   Ability to specify video segments (start/end timestamps) for specific feedback points or newly identified techniques.
    -   Edited data is saved to the database, linked to the original analysis.

#### 2.4.3 Personalized Feedback Distribution (Instructor Feature)
-   **Feature:** Instructors can distribute approved/edited analysis results to students.
-   **Details:**
    -   Mechanism for an instructor to finalize their review of an AI analysis.
    -   System sends an email to the student containing the instructor's approved/edited feedback and analysis.

### 2.5 AI-Driven Curriculum Recommendation (MVP - Instructor Feature)

#### 2.5.1 Single-Session Curriculum Generation
-   **Feature:** The system generates a tailored curriculum for a single class session.
-   **Details:**
    -   Input for generation:
        -   Aggregated data of checked-in students (age, height, weight, belt rank, gender).
        -   Analyzed weaknesses from students' uploaded videos (if available and applicable to attendees).
    -   The AI suggests 5-7 drills addressing common weaknesses relevant to the attending students.
    -   Drills are tailored to the class's average skill level (based on belt ranks) and the session's defined "Target Level".
    -   The generated curriculum includes warm-up, techniques, drills, sparring, and cool-down sections.

#### 2.5.2 Curriculum Display & Interaction
-   **Feature:** Instructors can view and interact with the AI-generated curriculum.
-   **Details:**
    -   Intuitive UI displaying the curriculum with drill names, detailed descriptions, focus areas, durations, and related weakness categories.
    -   Sections for Warm-Up, Techniques (with descriptions and tips), Drills (with description, focus, duration), Sparring (with description, guidelines, duration), and Cool-Down.
    -   For each drill/exercise, a time tracker/timer function is available for instructors to manage activity durations during the class.

#### 2.5.3 Instructor Feedback on Curriculum
-   **Feature:** Instructors can provide feedback on the generated curriculum.
-   **Details:**
    -   Mechanism for instructors to rate or comment on the usefulness/relevance of the suggested curriculum.
    -   This feedback is intended for future improvement of the AI recommendation engine.

### 2.6 Freemium Model & Payment Integration

#### 2.6.1 Free Tier
-   **Feature:** Provides basic access to the platform with limitations.
-   **Details:**
    -   Access to user registration and login.
    -   Limit of one (1) video upload per month for AI analysis.
    -   Simplified AI analysis for free tier: 1 technique, 1 strength, 1 improvement per video.
    -   Limit of one (1) active class session creation/management for instructors.
    -   Prompts to upgrade when limits are approached or reached.

#### 2.6.2 Premium Tier (Instructors)
-   **Feature:** Provides enhanced access for paying instructors.
-   **Details:**
    -   Unlimited video analysis uploads and processing.
    -   Full access to the Video Analysis Editor features.
    -   Unlimited single-session curriculum recommendations.
    -   Subscription cost: $15–$20 per month.

#### 2.6.3 One-Time Payments
-   **Feature:** Allows users to purchase specific features on a pay-per-use basis.
-   **Details:**
    -   Option to pay $5 for a bundle of 3 video analyses.
    -   Option to pay $5 for 1 curriculum suggestion.

#### 2.6.4 Stripe Integration
-   **Feature:** Secure payment processing for subscriptions and one-time purchases.
-   **Details:**
    -   Integration with Stripe API to handle payment transactions.
    -   Securely store payment status and link it to user accounts to manage feature access.

#### 2.6.5 Usage Tracking & Limit Enforcement
-   **Feature:** The system monitors user activity to enforce Freemium tier limits.
-   **Details:**
    -   Backend logic to track video uploads, analysis requests, and active class sessions per user.
    -   Notifications (in-app or email) to users when they are approaching or have exceeded their free-tier limits.

## 3. User Experience (UX)

### 3.1 General UX Principles
-   **Intuitive Navigation:** Clear and consistent navigation across both web and mobile platforms.
-   **Responsive Design:** Web application optimized for desktop use by instructors; mobile application designed for iOS and Android, responsive to various screen sizes.
-   **User Assistance:** Tooltips and help text for new users to understand features.
-   **Real-time Feedback:** For operations like video upload progress and AI analysis status.

### 3.2 Key User Flows

#### 3.2.1 User Onboarding (Registration & Login)
-   **Web & Mobile:**
    -   Users access a landing page (`LandingPage.tsx`, `LandingPageForm.tsx`) with options to Register or Login.
    -   **Registration:** Users fill a form (`Register.tsx`) with email, password, role, and fighter profile details (name, height, weight, gender, birthdate, experience, belt rank, sparring duration).
    -   **SSO Registration/Login:** Users can click "Sign in with Google" or "Sign in with Facebook" buttons, redirecting to the respective OAuth provider and then back to an SSO callback URL (`SSOCallback.tsx`) which handles token exchange and login/profile creation.
    -   **Login:** Users enter email and password (`Login.tsx`). Upon successful authentication, they are redirected to their respective dashboard (e.g., class session view).

#### 3.2.2 Class Session Management (Instructor)
-   **Web/Mobile:**
    -   **Creation:** Instructors navigate to a "Create Session" interface (`TrainingSessionForm.tsx`), fill in details (date, capacity, etc.), and submit.
    -   **Viewing:** Instructors see a list of their sessions (`Dashboard.tsx`/ClassSession page). Clicking a session navigates to `TrainingSessionDetails.tsx`.
    -   **Walk-in Attendance:** Within session details, instructors access an "Take Attendance" feature (`AttendancePage.tsx`), an editable table to input walk-in student data.
    -   **Student Pairing:** Within session details, an instructor can trigger the "Pair Up" function. Results (pairs of student names) are displayed.
    -   **Curriculum Generation:** Within session details, an instructor can trigger "Generate Today's Lessons." A loading indicator is shown. The generated curriculum is displayed in a structured, navigable format (Warm-Up, Techniques, Drills, Sparring, Cool-Down sections), with interactive elements like timers for drills.

#### 3.2.3 Video Upload & Analysis (Student & Instructor)
-   **Web/Mobile:**
    -   **Student Upload:** Students navigate to a video upload section (`VideoSharingForm.tsx` which includes `VideoUploadForm.tsx`). They can choose to upload a file or provide a YouTube URL, add a description, and specify their identifier in the video. Progress bar shown during upload.
    -   **Instructor Upload (for student footage):** Similar flow to student upload, but the system needs to be aware it's an instructor uploading for (potentially multiple) students, influencing AI analysis focus.
    -   **Analysis Status:** Users are notified when analysis begins and completes (potentially via SignalR notifications - `NotificationsListener.tsx`).
    -   **Viewing Raw AI Analysis:** Users can view the raw JSON output of the AI analysis (`AiAnalysisResults.tsx`).

#### 3.2.4 Video Analysis Review & Editing (Instructor)
-   **Web (Primary):**
    -   Instructors navigate to a "Video Review" page (`VideoReview.tsx`) for a specific uploaded video.
    -   The page displays a video player (`VideoPlayer.tsx`) alongside the AI-generated feedback (`TechniqueFeedback.tsx`).
    -   **Video Player Interaction:**
        -   Standard playback controls.
        -   Timeline displays markers for AI-identified techniques. Clicking a marker seeks the video.
        -   Instructors can select time segments on the timeline to associate with new or existing feedback.
    -   **Feedback Editing:**
        -   Tabs for "Identified Techniques" (`TechniquesEditorial.tsx`), "Suggested Drills" (`DrillsEditorial.tsx`), and "Overall Analysis" (`OverallAnalysisEditorial.tsx`).
        -   Editable fields (inputs, textareas, selects) for all aspects of the analysis.
        -   Ability to add new techniques/drills or delete existing ones.
        -   Changes are saved via a "Save Changes" button.

#### 3.2.5 Mobile Application UX (In-Class Focus)
-   **Instructors & Students:**
    -   Simplified interface optimized for quick actions during class.
    -   Access to session check-in (students), attendance taking (instructors).
    -   Viewing generated curriculum, including drill descriptions and timers.
    -   (Future) Students access instructor feedback on lessons.

## 4. Implementation Requirements

### 4.1 Technical Stack
-   **Frontend:** React with Vite, TailwindCSS, Shadcn UI components, Zustand (state management), i18next (internationalization).
-   **Backend:** .NET 8.0 Web API, Entity Framework Core (ORM).
    -   `FighterManager.Server`: Handles user authentication, fighter profiles, class session management, student pairing.
    -   `VideoSharing.Server`: Handles video metadata, YouTube integration, GCS uploads, AI analysis initiation and result storage, curriculum generation logic.
    -   `MatchMaker.Server`: (Potentially a separate service for pairing logic if complex, though current SRS implies pairing is part of FighterManager).
-   **Database:** PostgreSQL with JSONB support for flexible storage of analysis data.
-   **AI Services:** Google Cloud Vertex AI Gemini 2.5 Pro model for video analysis and curriculum recommendation.
-   **Cloud Storage:** Google Cloud Storage (GCS) for storing uploaded video files.
-   **Deployment:** Docker containers for all services, managed by Docker Compose for local development and production-like environments. Nginx as a reverse proxy and for serving the static frontend.
-   **Mobile:** React Native with Expo for iOS and Android application development.

### 4.2 Performance
-   **API Response Time:** < 2 seconds for up to 100 concurrent users.
-   **Video Analysis Processing:** A 5-minute video should be processed within 5 minutes.
-   **Mobile App Latency:** Minimal latency for real-time in-class interactions.

### 4.3 Scalability
-   The system must be designed to scale to 1,000 users within 6 months post-launch. Cloud resource optimization will be key.

### 4.4 Security
-   **Data Encryption:** Videos in GCS must be encrypted at rest.
-   **Data Privacy:** Compliance with GDPR/CCPA, including opt-in consent for data usage.
-   **Authentication:** JWT for API authentication, secure password hashing. Mobile security standards for the app.

### 4.5 Reliability
-   **Uptime:** Target 99.9% uptime, excluding scheduled maintenance.
-   **Database Backups:** Daily backups of the PostgreSQL database.

### 4.6 Cost Efficiency
-   **Cloud Costs:** Minimize GCS and Vertex AI costs, potentially through quotas for free-tier users (e.g., 500MB video storage).

### 4.7 Development & Deployment
-   **Database Migrations:** Use EF Core migrations, managed via the `FighterManager.Server` startup project.
-   **Environment Configuration:** Utilize `.env` files for local Docker Compose and `appsettings.json` (with environment-specific overrides like `appsettings.Development.json`) for .NET applications.
-   **Containerization:** All services (frontend, backends, database) must be containerized using Docker.
-   **API Versioning:** APIs should support versioning (e.g., `/api/v1/...`).
-   **Logging:** Implement structured logging (e.g., Serilog) for backend services, writing to console and daily log files.