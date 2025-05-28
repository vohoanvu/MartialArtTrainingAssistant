# Project Status Tracking: CodeJitsu BJJ Martial Art Training Assistant

**Version:** 1.0
**Date:** July 24, 2024

## 1. Implementation Phases

This & User Management (FighterManager.Server)**
*   **Objective:** Establish foundational user authentication, fighter profiles, and basic session project will be implemented in a phased approach, prioritizing core MVP functionality to enable early launch and revenue generation, followed by enhancements management.
*   **Key Features:**
    *   User Registration (Email/Password) & Login
    *   Class Session Management.
    *   AI-driven Video Analysis and editor.

### Phase 1: MVP Core Backend & Web Frontend (Target: 4 Weeks fromighter Profile Creation & Management (linked to User)
    *   Role-Based Access Control (Instructor, Student)
    *   Basic Training Session Creation (Instructor)
    *   Student Check-in to Sessions
    *   Database Schema Setup & Initial Mig Backend Completion)

**Objective:** Launch core features for instructors, focusing on video analysis and single-session curriculum planning, with a functionalrations for Core Entities

**Phase 2: Video Sharing & Storage Foundation (VideoSharing.Server)**
*   **Objective:** Implement Freemium model and payment integration.

**Key Features:**
1.  **User Authentication (FighterManager. video upload capabilities and YouTube integration.
*   **Key Features:**
    *   YouTube Video URL Sharing & Metadata Storage
    *   DirectServer & Client):**
    *   Email/Password Registration & Login.
    *   Google SSO Registration & Login.
    *   F Video Upload to Google Cloud Storage (GCS)
        *   File hashing for duplicate detection
        *   Secureighter Profile creation and association with User.
    *   Role assignment (Instructor, Student).
2.  **Class Session Management - Basic (FighterManager.Server & Client):**
    *   Instructor: Create, view, and update signed URL generation for viewing
    *   Video Metadata Storage (linking to user, type, martial art, etc.)
    *    basic session details (date, capacity, duration, description, target level).
    *   Student: View sessions and check-in.
    Basic Video Listing & Deletion for User-Uploaded Content
    *   SignalR Hub setup for real-time notifications (*   Instructor: Basic student pairing (manual or simple algorithm).
3.  **Video Upload & Storage (Videoe.g., video shared)

**Phase 3: AI Video Analysis - Core Engine (VideoSharing.Server)**
*   **Sharing.Server & Client):**
    *   Student/Instructor: Upload video files to GCS.
    *   StudentObjective:** Integrate Gemini Vision for initial AI analysis of uploaded videos.
*   **Key Features:**
    *   Integration/Instructor: Share YouTube video URLs.
    *   Secure storage and retrieval of video metadata.
4.   with Google Vertex AI Gemini Vision API.
    *   Backend logic to send video (GCS path) and prompt**AI Video Analysis - Core (VideoSharing.Server & Client):**
    *   Integration with Google Vertex AI Gemini for to Gemini.
    *   Receive and store raw AI analysis JSON (`AiAnalysisResult.AnalysisJson`).
    *   Parse and store top-level AI results (OverallDescription, Strengths, AreasForImprovement).
    *   ` video processing.
    -   Extraction and storage of techniques, strengths, areas for improvement (raw and parsed).
    -   Instructor: View AI analysis results.
5.  **Video Analysis Editor - MVP (VideoSharing.Server & Client):**
AiAnalysisProcessorService` initial implementation to extract and store `Techniques`, `Drills`, and link `VideoSegmentFeedback    -   Instructor: Edit AI-generated analysis (techniques, strengths, improvements, overall description).
    -   Instructor: Specify video segments for feedback.
    -   Save edited analysis to PostgreSQL.
6.  **Single-Session Curriculum Recommendation`.

**Phase 4: MVP Feature - Single-Session Curriculum Recommendation (VideoSharing.Server & FighterManager.Server Integration)**
*   **Objective:** Deliver the AI-driven curriculum recommendation feature.
*   **Key Features:**
    *   ` - MVP (VideoSharing.Server & Client):**
    -   AI-driven curriculum generation based on student data (if available) and session parameters.
    -   Display curriculum to instructors (warm-up, techniques, drills, sparringCurriculumRecommendationService` implementation.
    *   Logic to gather student data (profiles, latest AI analysis weaknesses) for a session.
    *   Prompt engineering for Gemini to generate class curriculum.
    *   Storage of raw AI-generated curriculum JSON in `TrainingSession` entity.
    *   API endpoint to trigger curriculum generation for a session.
    *   API endpoint to retrieve stored/generated curriculum.

**Phase 5: Frontend - Core User Flows & MVP Features**
*   **, cool-down).
7.  **Freemium Model & Payments - Basic (FighterManager.Server, VideoSharing.Server & Client):**
    -   Enforce free-tier limits (1 video upload/month, 1 active session).
    -   Basic Stripe integration for instructor premium subscription ($15-20/month).
    -   Track usage and notify users of limits.
8.  **Walk-in Student Attendance (FighterManager.Server & Client):**Objective:** Develop the user interface for core functionalities and the MVP.
*   **Key Features:**
    *   User
    -   Instructor interface to add walk-in students to a session.
    -   Creation of "walk-in" Fighter records.

### Phase 2: Feature Enhancements & Mobile App MVP (Target: Weeks 4 Registration, Login, and Profile display.
    *   (Instructor) Training Session Creation, Listing, and basic Details view.
    *   (Student) Session Check-in.
    *   Video Upload (YouTube & GCS) forms-7 Post Backend MVP Completion)

**Objective:** Enhance existing features, complete remaining backend functionalities from SRS, and launch the MVP.
    *   Basic display of shared YouTube videos.
    -   (Instructor) Display of AI-Generated mobile application for instructors and students.

**Key Features:**
1.  **Backend Enhancements:**
    * Curriculum for a session, including drill timers.
    -   (Instructor) "Take Walk-in Attendance" UI   Facebook SSO integration.
    *   Refine student pairing algorithm.
    *   Implement one-time payment options and functionality.

**Phase 6: Advanced Features & Refinements**
*   **Objective:** Implement remaining via Stripe.
    *   Personalized lessons distributor via email (FR3.4).
    *   AI functional requirements and enhance UX.
*   **Key Features:**
    *   SSO Integration (Google, Facebook). analysis for *both* fighters in instructor-uploaded footage (FR3.5).
    *   Instructor feedback mechanism for AI
    *   Video Analysis Editor (Instructor): Full UI for viewing and editing AI analysis (Techniques, Drills, Overall-generated curriculum (FR5.5).
    *   Drill time tracker in curriculum view (FR5.6).
    ).
    *   Student Pairing UI in Session Details.
    *   Freemium Model Implementation:
        **   Complete Students List Import (Excel - if not fully covered by Walk-in Attendance in Phase 1).
2.  **Web   Usage tracking for free-tier limits.
        *   Upgrade prompts.
        *   Stripe integration for premium subscriptions and one-time payments.
    *   Personalized Feedback Distribution via email.
    *   Instructor Upload Frontend Enhancements:**
    *   UI polish and improvements based on initial feedback.
    *   Full implementation of the Video of Student Footage (with dual fighter analysis).
    *   Students List Import (Excel).
    *   Advanced Analysis Editor with all described editing capabilities.
    *   Intuitive display for AI-generated curriculum with interactive timers.
3.  **Mobile Video Listing & Management (user's GCS uploads).

**Phase 7: Mobile Application Development (React Native with Expo)**
*   **Objective:** Develop and launch the mobile application for iOS and Android.
*   **Key Features (MVP for Application MVP (React Native with Expo):**
    *   User Authentication (Email/Password, Google SSO).
    *   Class Mobile):**
    *   User Authentication (Email/Password, SSO).
    *   Class Session Management ( Session Management:
        *   Instructor: Create/view sessions.
        *   Student: View/check-in toInstructor: create/view; Student: check-in).
    *   View AI-Generated Curriculum & Drill Timers. sessions.
        *   Instructor: View pairings and generated curriculum (read-only initially, with timers).
    *   Video
    *   (Future mobile MVP) Student video upload.
    *   (Future mobile MVP) Instructor view/edit Upload (Student): Upload sparring videos to GCS or submit YouTube URLs for AI analysis.
    *   View AI of student analysis.

**Phase 8: Deployment, Testing & Launch**
*   **Objective:** Deploy the application Analysis (Student): Students can view their (instructor-approved) analysis.
    *   (Future consideration for this phase if to production, conduct thorough testing, and launch.
*   **Key Features:**
    *   Setup and configure G time allows) Instructor: Basic view/edit of AI analysis on mobile.

### Phase 3: Post-MVP Features &KE for production.
    *   Implement CI/CD pipelines (Cloud Build).
    -   Comprehensive E2E testing. Iteration

**Objective:** Introduce advanced features and iterate based on user feedback and market demand.

**Key Features:**
1.  **Long-Term Curriculum Recommendation:** Personalized 3-6 month curriculum plans.
2.  **Advanced Partner-Matching:** Based on training goals and more detailed student attributes.
3.  **Student Premium Features:** Access
    *   Beta launch to early adopters.
    *   Public launch.

## 2. Milestone Checklist

| ID   | Feature / Deliverable                                     | Phase | Status      | Target Date | Notes                                     |
| :--- | :-------------------------------------------------------- | :---- | :---------- | :---------- | :---------------------------------------- |
| ** to instructor feedback on training lessons within the mobile app.
4.  **Dojo/Workspace Management:** Allow instructors to managePhase 1: Core Backend & User Management**                       |       |             |             |                                           |
| M1. sessions within their own company/dojo workspace.
5.  **AI Model Fine-tuning:** Utilize instructor-1 | User Registration (Email/Password) API & DB Schema        | 1     | Done        |             | `edited analysis data to improve AI accuracy.
6.  **Expanded SSO Options:** Potentially add other SSO providers.

## 2. MilestoneFighterManager.Server`                   |
| M1.2 | User Login API & JWT Generation                         | 1     | Done        |             | `FighterManager.Server`                   |
| M1.3 | Fighter Profile DB Schema & Basic Checklist

**Phase 1: MVP Core Backend & Web Frontend**
-   [ ] **M1.1:** CRUD API              | 1     | Done        |             | `FighterManager.Server`                   |
| M1.4 | Role-Based Access Control (Initial Setup)                 | 1     | Done        |             | Middleware/ User registration (Email/Pass, Google SSO) & Fighter Profile creation fully functional.
-   [ ] **M1.2:** InstructorAttributes                     |
| M1.5 | Training Session Creation API (Basic)                   | 1     | Done        | can create and view basic training sessions.
-   [ ] **M1.3:** Students can view and check-in to sessions.
             | `FighterManager.Server`                   |
| M1.6 | Student Check-in API                                    | 1     | Done        |             | `FighterManager.Server`                   |
| **Phase-   [ ] **M1.4:** Video upload (GCS & YouTube URL) to `VideoSharing.Server` is operational. 2: Video Sharing & Storage**                            |       |             |             |                                           |
| M2.1 | YouTube Video URL
-   [ ] **M1.5:** AI analysis (Gemini) is triggered for uploaded videos, and raw Sharing API & Metadata Storage        | 2     | Done        |             | `VideoSharing.Server`                     |
| M2.2 |/parsed results are stored.
-   [ ] **M1.6:** Instructors can view AI analysis results on GCS Video Upload API (File to GCS)                      | 2     | Done        |             | `VideoSharing.Server` the web.
-   [ ] **M1.7:** Video Analysis Editor MVP: Instructors can edit overall                     |
| M2.3 | VideoMetadata DB Schema & CRUD                          | 2     | Done        | description, techniques (name, description), strengths, and areas for improvement. Changes are saved.
-   [ ] **             | `VideoSharing.Server`                     |
| M2.4 | SignalR Hub for Basic Notifications (Video Shared)      | 2     | Done        |             | `VideoSharing.Server`                     |
| **Phase 3: AI Video Analysis - Core**                           |       |             |             |                                           |
| M3M1.8:** Single-Session Curriculum Recommendation MVP: AI generates a curriculum based on session parameters; instructors can view it.
-   [ ] **M1.9:** Freemium limits (video upload, active session) are enforced.
-   [.1 | Gemini Vision API Integration for Video Analysis        | 3     | Done        |             | `VideoSharing.Server` ] **M1.10:** Stripe integration for instructor premium subscription is functional.
-   [ ] **M                     |
| M3.2 | Store Raw AI Analysis JSON (`AiAnalysisResult`)           | 3     | Done        |1.11:** Walk-in student attendance feature allows instructors to add students and create `IsWalkIn` fighter             | `VideoSharing.Server`                     |
| M3.3 | Initial Parsing of AI JSON (Overall records.

**Phase 2: Feature Enhancements & Mobile App MVP**
-   [ ] **M2., Strengths, Areas)  | 3     | Done        |             | `AiAnalysisProcessorService`              1:** Facebook SSO implemented and functional.
-   [ ] **M2.2:** One-time payment options|
| M3.4 | Store Parsed Techniques & Drills from AI JSON           | 3     | Done        |             | `AiAnalysisProcessorService`              |
| **Phase 4: MVP - Curriculum Recommendation**                     via Stripe are available.
-   [ ] **M2.3:** AI can analyze both fighters in instructor-uploaded footage|       |             |             |                                           |
| M4.1 | `CurriculumRecommendationService` Logic                 .
-   [ ] **M2.4:** Email distribution of instructor-approved analysis to students is working.
-   [ ] **M| 4     | Done        |             | `VideoSharing.Server`                     |
| M4.2 | API2.5:** Drill time trackers are functional in the web curriculum view.
-   [ ] **M2.6:** Mobile App to Generate Curriculum for a Session                | 4     | Done        |             | `VideoSharing.Server`                     |
| M4: User authentication (Email/Pass, Google SSO) is working.
-   [ ] **M2.7:** Mobile.3 | API to Retrieve Stored Curriculum                       | 4     | Done        |             | `VideoSharing.Server` App: Students can view sessions and check-in.
-   [ ] **M2.8:** Mobile App: Instruct                     |
| **Phase 5: Frontend - Core & MVP**                              |       |             |             |                               ors can create and view sessions.
-   [ ] **M2.9:** Mobile App: Instructors/            |
| M5.1 | User Registration & Login Forms UI                      | 5     | Done        |             |                               Students can view AI-generated curriculum with timers.
-   [ ] **M2.10:** Mobile App            |
| M5.2 | Instructor Session Creation & List UI                   | 5     | Done        |             |                                           |
: Students can upload videos for analysis.

## 3. Testing Criteria

### 3.1 Functional Testing
-   **User Authentication| M5.3 | Student Session Check-in UI                             | 5     | Done        |             |                                           |
| M:**
    -   Verify successful registration and login with email/password.
    -   Verify successful registration and login with Google SSO5.4 | Video Upload Forms UI (YouTube & GCS)                   | 5     | Done        |             |                                           |
| M5.5 | Instructor Curriculum Display UI (with Timers)          | 5     | Done        |             |                                           | (and Facebook SSO in Phase 2).
    -   Verify correct role assignment and access restrictions.
    -   Verify
| M5.6 | Instructor "Take Walk-in Attendance" UI                | 5     | Done        | Fighter Profile data persistence and retrieval.
-   **Class Session Management:**
    -   Verify instructors can create,             |                                           |
| **Phase 6: Advanced Features & Refinements**                    |       |             |             |                                update, and view sessions with all specified details.
    -   Verify students can successfully check into sessions.
    -   Verify student            |
| M6.1 | SSO Integration (Google) Backend & Frontend           | 6     | Done        |             | `FighterManager.Server`, Client           |
| M6.2 | SSO Integration (Facebook) Backend pairing logic correctly pairs students based on size/skill.
    -   Verify walk-in attendance correctly creates `Fighter` records and adds & Frontend         | 6     | To Do       |             | `FighterManager.Server`, Client           |
| M6.3 | them to the session.
    -   Verify Excel student import (if separate from walk-in) correctly creates student Video Analysis Editor UI (Full Functionality)           | 6     | Done        |             | Client                                    |
| M6.4 | profiles.
-   **Video Upload & Analysis:**
    -   Verify successful video upload to GCS and YouTube Student Pairing UI in Session Details                   | 6     | Done        |             | Client, `MatchMaker.Server`              URL submission.
    -   Verify AI analysis is triggered and results (techniques, strengths, weaknesses, overall description, drills)|
| M6.5 | Freemium Model - Usage Tracking & Limits Backend        | 6     | To are stored correctly.
    -   Verify AI correctly identifies the student based on `studentIdentifier`.
    -   Verify AI Do       |             | All Servers                               |
| M6.6 | Freemium Model - Upgrade Prompts UI                     | 6     | To Do       |             | Client                                    |
| M6.7 | Stripe Payment Integration Backend analyzes both fighters for instructor-uploaded footage.
    -   Verify instructor can view raw and processed AI analysis.
- & UI                 | 6     | To Do       |             | `FighterManager.Server` (or dedicated   **Video Analysis Editor:**
    -   Verify all fields in the editor are editable and changes are saved correctly.
    -   Verify instructors), Client |
| M6.8 | Personalized Feedback Email Distribution                | 6     | To Do       |             | ` can add/delete techniques, strengths, areas for improvement, and drills.
    -   Verify video segment selection and associationVideoSharing.Server`                     |
| M6.9 | Instructor Upload of Student Footage (Dual Analysis)    | 6     | To Do       |             | `VideoSharing.Server`, Client           |
| M6.10 with feedback works.
-   **Curriculum Recommendation:**
    -   Verify curriculum is generated based on student data/| Students List Import (Excel) Backend & UI               | 6     | To Do       |             | `FighterManager.Server`, Clientweaknesses and session parameters.
    -   Verify curriculum display is accurate and interactive (timers, collapsible sections).
    -   Verify instructor           |
| **Phase 7: Mobile Application**                                 |       |             |             |                                           |
| M7.1 feedback on curriculum can be submitted.
-   **Freemium & Payments:**
    -   Verify free-tier limits are correctly | Mobile App - User Auth UI & Logic                       | 7     | To Do       |             | React Native                               enforced.
    -   Verify upgrade prompts are displayed.
    -   Verify Stripe subscription and one-time payments|
| M7.2 | Mobile App - Session Management (View, Check-in, Create)| 7     | To Do       | can be processed successfully.
    -   Verify premium features are unlocked upon successful payment.
-   **Mobile Application (             | React Native                              |
| M7.3 | Mobile App - Curriculum Viewer & Timers                 | 7     | To Do       |             | React Native                              |
| **Phase 8: Deployment, Testing & Launch**                       |       |             |             Phase 2):**
    -   Test all MVP mobile features on both iOS and Android target versions.
    -   Verify UI responsiveness and performance.

### 3.2 Non-Functional Testing
-   **Performance:**
    -   Test|                                           |
| M8.1 | GKE Production Environment Setup                        | 8     | Done        |             | Kubernetes manifests API response times under simulated load (target <2s for 100 concurrent users).
    -   Measure                      |
| M8.2 | CI/CD Pipeline (Cloud Build) for Production             | 8     | Done        |             | `cloudbuild-prod.yaml`                    |
| M8.3 | Comprehensive E2E Testing video analysis processing time (target <5 mins for a 5-min video).
-   **Scalability:**
    -   Review                               | 8     | In Progress |             |                                           |
| M8.4 | Beta Launch & Feedback Collection                       | 8     | To Do       |             |                                           |
| M8.5 | Public Launch                                architecture for scalability bottlenecks.
    -   (Post-MVP) Conduct load tests to simulate 1000 users.
-   **Security:**
    -   Penetration testing (basic initially, more comprehensive post-MVP).
    -   Verify data            | 8     | To Do       |             |                                           |

## 3. Testing Criteria

### 3.1 Functional Testing
-   **User Authentication:**
    -   Verify email/password registration and login.
    -   Verify Google SSO registration and login.
    -   Verify Facebook SSO registration and login (once implemented).
    -   Verify role encryption at rest (GCS).
    -   Verify JWT implementation for vulnerabilities.
    -   Verify role-based access controls are correctly enforced.
    -   Verify compliance with GDPR/CCPA for user data handling.
-   **Usability:**-based access to features (e.g., instructor can create sessions, student cannot).
    -   Verify fighter
    -   Conduct user acceptance testing (UAT) with target instructors and students.
    -   Gather feedback on UI profile creation and data persistence.
-   **Class Session Management:**
    -   Verify instructor can create, view, and update sessions.
    -   Verify student can check into sessions.
    -   Verify student pairing logic works intuitiveness, clarity, and ease of use.
-   **Reliability:**
    -   Monitor system uptime.
    -   Test database backup and restore procedures.

## 4. Deployment Stages

### 4.1 Local based on size/skill.
    -   Verify walk-in attendance: data entry, new fighter creation (`IsWalkIn=true`), association Development Environment
-   **Tools:** Docker Compose (`docker-compose.yml`).
-   **Services:**
    -   `app with session.
    -   Verify Excel student import functionality.
-   **Video Upload & Analysis:**
    -   -db` (PostgreSQL)
    -   `fighter-manager` (.NET API)
    -   `video-sharing` (.NET API)
    -   `match-maker` (.NET API)
    -   Verify direct video upload to GCS.
    -   Verify YouTube URL submission.
    -   Verify AI analysis is`app-client` (React app served by Nginx, configured via `nginx.conf`)
-   **Configuration:** Via triggered and results are stored (raw and processed).
    -   Verify duplicate video detection (hash-based for GCS). `.env` file and `appsettings.Development.json`.
-   **Purpose:** Individual developer work, local
    -   Verify instructor can upload student footage for dual analysis.
-   **Video Analysis Editor:**
    -   Verify all testing, debugging.

### 4.2 Staging/Test Environment (GCP - GKE)
-   **Infrastructure:** Google Kubernetes Engine (GKE) cluster (e.g., `codejitsu-cluster` in `us fields in the editor are editable and changes persist.
    -   Verify adding/deleting techniques, strengths, areas for improvement,-central1-a`).
-   **Deployment:**
    -   Automated via Google Cloud Build trigger (`cloud drills.
    -   Verify video player seeking works correctly when clicking timestamps.
    -   Verify segment selection updatesbuild-test.yaml`).
    -   Builds Docker images for each service, pushes to Google Container Registry (GCR).
     timestamps in forms.
-   **Curriculum Recommendation:**
    -   Verify curriculum generation based on student weaknesses/-   Uses `kubectl` to apply Kubernetes manifests (`k8s/test/` directory, including generated deployment YAMLprofiles.
    -   Verify curriculum display format and content.
    -   Verify drill timers function correctly.
    -   Verify instructor feedback on curriculum can be submitted.
-   **Freemium Model:**
    -   s from `generate-yaml.sh`).
    -   Manages secrets via Kubernetes Secrets (`k8s/Verify free-tier limits are enforced (video uploads, active sessions, simplified analysis).
    -   Verify upgrade prompts are displayed correctly.
    -test/secrets.yaml`).
    -   Uses a staging Let's Encrypt issuer for SSL certificates.
    -   Ingress configured with   Verify Stripe payment integration for subscriptions and one-time purchases (once implemented).
-   **Mobile Application (once path-based routing and rewrites (`ingress-*.yaml` files).
-   **Namespace:** `test`. developed):**
    -   Test core features (auth, session management, curriculum view) on both iOS and Android.
-   **Purpose:** Integration testing, UAT, pre-production validation.

### 4.3 Production

### 3.2 Non-Functional Testing
-   **Performance:**
    -   Test API response times under simulated Environment (GCP - GKE)
-   **Infrastructure:** Separate GKE cluster or a dedicated, more robust node load (target < 2s for 100 concurrent users).
    -   Measure video analysis processing time ( pool within the same cluster (e.g., `codejitsu-cluster` in `us-central1-a`,target < 5 mins for a 5-min video).
-   **Scalability:** (More relevant post but with production configurations).
-   **Deployment:**
    -   Automated via Google Cloud Build trigger (`cloudbuild-prod.yaml`).-launch with monitoring)
    -   Monitor resource usage under increasing load on GKE.
-   **Security
    -   Builds Docker images, pushes to GCR (potentially with different tagging or a separate production GCR repository:**
    -   Penetration testing (once major features are stable).
    -   Verify JWTs are handled).
    -   Uses `kubectl` to apply production-ready Kubernetes manifests (`k8s/prod/` directory).
        -   Includes `db-migration-job.yaml` to run EF Core migrations.
        -   Deploy securely (HTTPS, httpOnly cookies if applicable for web-to-backend).
    -   Verify GCS bucketments for `frontend`, `fighter-manager`, `video-sharing`, `match-maker`.
        -   Services policies and video access controls.
    -   Verify data encryption at rest for GCS.
    -   Verify role and Ingress (`ingress.yaml`) for external access.
    -   Manages secrets via Kubernetes Secrets, ideally-based access controls are correctly enforced at API level.
-   **Usability:**
    -   User acceptance integrated with Google Secret Manager for production.
    -   Uses a production Let's Encrypt issuer for SSL certificates.
-   **Namespace testing (UAT) with target users (BJJ instructors, students).
    -   Ensure intuitive navigation and clear UI:** `prod`.
-   **Database:** Production PostgreSQL instance (e.g., Cloud SQL for PostgreSQL or a managed elements.
-   **Reliability:**
    -   Test database backup and restore procedures.
    -   Monitor system Supabase instance as implied by connection string examples).
-   **Monitoring & Logging:** Integrated with Google Cloud Logging and Monitoring uptime in staging/production.
    -   Test error handling and recovery for external service failures (GCP, YouTube)..
-   **Purpose:** Live user access.