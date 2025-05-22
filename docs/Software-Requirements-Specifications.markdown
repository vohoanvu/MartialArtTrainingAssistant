# Software Requirements Specification

## Martial Art Training Assistant SaaS

**Version:** 1.0\
**Date:** May 13, 2025\
**Author:** Vo Hoan Vu (Solo Developer)\
**Prepared for:** Internal Development and Stakeholders

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) document outlines the functional and non-functional requirements for the Martial Art Training Assistant, a Software-as-a-Service (SaaS) platform designed to enhance Brazilian Jiu-Jitsu (BJJ) training. The platform targets BJJ instructors and students, offering AI-driven video analysis, class management, and curriculum recommendations to streamline training processes and improve skill development.

The document reflects the current state of the project, including completed features and the prioritized feature for the Minimum Viable Product (MVP) launch, aimed at generating quick cash flow through a Freemium model.

### 1.2 Scope

The Martial Art Training Assistant provides:

- **User Authentication:** Secure sign-up and login for instructors and students.
- **Class Session Management:** Tools for instructors to create sessions and pair students, and for students to check in.
- **Video Upload and Analysis:** Students upload sparring videos for AI analysis using Google Vertex AI Gemini Vision, with results stored in a relational database.
- **Video Analysis Editor:** Instructors refine AI-generated analysis data, saving edits for improved training feedback.
- **Single-Session Curriculum Recommendation (MVP):** AI-driven suggestions for class drills based on student weaknesses, ensuring compatibility with all attendees.

The MVP focuses on delivering value to instructors through video analysis and class planning, with a Freemium model to attract users and generate revenue.

### 1.3 Definitions, Acronyms, and Abbreviations

- **BJJ:** Brazilian Jiu-Jitsu
- **SaaS:** Software-as-a-Service
- **MVP:** Minimum Viable Product
- **AI:** Artificial Intelligence
- **Gemini Vision:** Google Vertex AI Gemini 2.5 Pro model for video analysis
- **GCS:** Google Cloud Storage
- **PostgreSQL:** Relational database management system
- **Freemium:** Business model with free and premium tiers

### 1.4 References

- Market Research: Sports Coaching Market to Grow by USD 4.77 Billion (2025-2029)
- Market Research: AI in Sports Market Report 2025
- Project Repository: `repomix-output-vohoanvu-MartialArtTrainingAssistant.xml`

---

## 2. Overall Description

### 2.1 Product Perspective

The Martial Art Training Assistant is a web-based SaaS platform integrating AI-driven video analysis, class management, and curriculum planning. It interfaces with Google Cloud Storage for video storage, Vertex AI Gemini Vision for analysis, and PostgreSQL for data persistence. The platform operates on a Freemium model, offering basic features for free and premium features for instructors at $15–20/month.

### 2.2 Product Functions

- **User Authentication:** Secure registration and login for instructors and students.
- **Class Session Management:** Instructors create and manage sessions; students check in; automatic student pairing based on size and skill.
- **Video Upload and Analysis:** Students upload videos to GCS or share YouTube URLs for AI analysis, generating techniques, strengths, and improvement areas.
- **Video Analysis Editor:** Instructors refine AI analysis, saving edits to PostgreSQL for feedback and model improvement.
- **Single-Session Curriculum Recommendation:** Generates class drill plans based on aggregated student weaknesses, tailored to the class’s skill level.
- **Mobile access:** The system will support a mobile application for instructors and students to access features conveniently during in-class sessions, while the web application will primarily serve instructors for reviewing and editing feedback on student-uploaded videos.

### 2.3 User Classes and Characteristics

1. **Instructors (Primary Paying Users):**
   - BJJ coaches with varying expertise (white to black belt).
   - Need tools to analyze student videos, provide feedback, and plan classes efficiently.
   - Target market: Small to medium BJJ schools in the US, Europe, and Asia.
2. **Students:**
   - BJJ practitioners (beginner to advanced).
   - Seek feedback on sparring videos and personalized training plans.
   - Secondary market for future premium features.

### 2.4 Operating Environment

- **Frontend:** React with Vite, TailwindCSS, hosted via Nginx (Dockerized).
- **Backend:** .NET 8.0 Web API (FighterManager.Server, VideoSharing.Server), Dockerized.
- **Database:** PostgreSQL, Dockerized.
- **Cloud Services:** Google Cloud Storage, Vertex AI Gemini Vision.
- **Deployment:** Docker Compose for development and production.
- **Mobile:** The mobile application will be developed for iOS and Android platforms using React Native Expo, integrating with the backend services, while the web application will be optimized for desktop use.

### 2.5 Design and Implementation Constraints

- **Budget:** Limited due to solo developer’s financial situation; prioritize low-cost cloud services.
- **Timeline:** MVP launch within 4 weeks to generate cash flow.
- **Scalability:** Must handle up to 100 concurrent users initially, with plans for growth.
- **Security:** Encrypt video storage and comply with GDPR/CCPA for user data.

### 2.6 Assumptions and Dependencies

- **Assumptions:**
  - Instructors are willing to pay $15–20/month for premium features.
  - Students will use free tier initially, with potential for premium upgrades.
- **Dependencies:**
  - Google Cloud services (GCS, Vertex AI) for video storage and analysis.
  - Stripe or PayPal for payment processing.
  - Stable PostgreSQL hosting for data persistence.

---

## 3. Functional Requirements

### 3.1 User Authentication

- **FR1.1:** Users (instructors/students) shall register with email, password, and role or GOOGLE AND FACEBOOK SSO.
- **FR1.2:** Users shall log in securely using email and password.
- **FR1.3:** System shall assign roles (Instructor/Student) to restrict access to role-specific features.

### 3.2 Class Session Management

- **FR2.1:** Instructors shall create a class session with details (date, time, capacity, description).
- **FR2.2:** Students shall check into an active session created by an instructor.
- **FR2.3:** System shall pair students for partner-based exercises based on size (height, weight) and skill level (belt rank).
- **FR2.4:** Instructors shall view session details, including checked-in students and pairings.

### 3.3 Student Sparring Video Upload

- **FR3.1:** Students shall upload sparring videos to Google Cloud Storage or share YouTube URLs.
- **FR3.2:** System shall process videos using Vertex AI Gemini Vision to generate analysis (techniques, strengths, improvements).
- **FR3.3:** Analysis results shall be stored in PostgreSQL with JSONB for techniques and text for strengths/improvements.

### 3.4 Video Analysis Editor

- **FR4.1:** Instructors shall view student videos and AI-generated analysis (techniques, strengths, improvements).
- **FR4.2:** Instructors shall edit analysis data, including techniques (type, scenario), strengths, and improvements.
- **FR4.3:** Instructors shall specify video segments (start/end timestamps) for feedback.
- **FR4.4:** Edited data shall be saved to PostgreSQL, linked to the original analysis.
- **FR4.5:** System shall retrieve edited data for display and future model improvement.

### 3.5 Single-Session Curriculum Recommendation

- **FR5.1:** System shall generate a curriculum for a single class session based on checked-in students’ video analysis data.
- **FR5.2:** Curriculum shall include 5-7 drills addressing common weaknesses (e.g., takedown defense, guard retention).
- **FR5.3:** Drills shall be tailored to the class’s average skill level (based on belt ranks).
- **FR5.4:** Instructors shall view the curriculum with drill names, descriptions, and related weakness categories.
- **FR5.5:** System shall allow instructors to provide feedback on the curriculum for future improvements.

### 3.6 Students List Import
- **FR6.1:** Instructors shall import a list of students from an Excel spreadsheet.
- **FR6.2:** The system shall validate the spreadsheet format and map data to student profiles.
- **FR6.3:** Imported students shall be added to the database with default roles and profiles.

### 3.7 SSO Registration and Authentication
- **FR7.1:** Users shall register and log in using Google and Facebook SSO.
- **FR7.2:** The system shall integrate with OAuth 2.0 for secure authentication.
- **FR7.3:** User profiles shall be created or linked based on SSO data.

### 3.8 Freemium Feature Access Limit
- **FR8.1:** Free-tier users shall have limited access to features (e.g., 1 video upload/month).
- **FR8.2:** The system shall track usage and enforce limits based on user tier.
- **FR8.3:** Users shall receive notifications when approaching or exceeding limits.

### 3.9 Stripe Payment Integration
- **FR9.1:** The system shall integrate with Stripe for subscription and one-time payments.
- **FR9.2:** Users shall subscribe to premium plans or make one-time payments for additional features.
- **FR9.3:** Payment status shall be stored and used to grant access to premium features.
---

## 4. Non-Functional Requirements

### 4.1 Performance

- **NFR1.1:** System shall handle up to 100 concurrent users with response times under 2 seconds for API calls.
- **NFR1.2:** Video analysis processing shall complete within 5 minutes for a 5-minute video.

### 4.2 Scalability

- **NFR2.1:** System shall scale to 1,000 users within 6 months post-launch via cloud resource optimization.

### 4.3 Security

- **NFR3.1:** Videos in GCS shall be encrypted at rest.
- **NFR3.2:** User data shall comply with GDPR/CCPA, with opt-in consent for data usage.
- **NFR3.3:** Authentication shall use JWT with secure password hashing.

### 4.4 Usability

- **NFR4.1:** UI shall be intuitive, with clear navigation for instructors to access video analysis and curriculum tools.
- **NFR4.2:** System shall provide tooltips and help text for new users.

### 4.5 Reliability

- **NFR5.1:** System shall achieve 99.9% uptime, excluding scheduled maintenance.
- **NFR5.2:** Database backups shall occur daily to prevent data loss.

### 4.6 Cost Efficiency

- **NFR6.1:** Cloud costs (GCS, Vertex AI) shall be minimized using quotas for free-tier users (e.g., 500MB video storage).

### 4.7 Mobile Application Requirements
- **NFR7.1:** The mobile app shall be responsive and optimized for various screen sizes.
- **NFR7.2:** The app shall perform efficiently with minimal latency for real-time interactions.
- **NFR7.3:** User data shall be securely handled in compliance with mobile security standards.

---

## 5. Freemium Model Requirements

### 5.1 Free Tier

- **FRM1.1:** Users shall access basic authentication, 1 video upload/month with simplified AI analysis, and 1 active class session.
- **FRM1.2:** Free tier shall limit analysis to 1 technique, 1 strength, and 1 improvement per video.
- **FRM1.3:** System shall display upgrade prompts when free-tier limits are reached.

### 5.2 Premium Tier

- **FRM2.1:** Instructors shall access unlimited video analysis, full editing, and curriculum recommendations for $15–20/month.
- **FRM2.2:** System shall offer one-time payments ($5 for 3 analyses or 1 curriculum suggestion).
- **FRM2.3:** Payment processing shall integrate with Stripe for subscriptions and one-time payments.

---

## 6. System Architecture

### 6.1 Overview

- **Frontend:** React with Vite, TailwindCSS, hosted via Nginx.
- **Backend:** .NET 8.0 Web API (FighterManager.Server, VideoSharing.Server).
- **Database:** PostgreSQL with JSONB for flexible analysis storage.
- **Cloud Services:** Google Cloud Storage, Vertex AI Gemini Vision.
- **Deployment:** Docker Compose, with separate containers for frontend, backend, and database.
- The backend services will support both the web application (for instructor video feedback) and the mobile application (for in-class curriculum planning features), ensuring seamless data synchronization and API access.

### 6.2 Database Schema (Key Tables)

- **users:** Stores user data (Id, Email, PasswordHash, Role, FighterId, ...). Links to **fighter**.
- **fighter:** Stores fighter profile (Id, FighterName, Height, Weight, BMI, Gender, Role, Birthdate, Experience, BeltRank, MaxWorkoutDuration).
- **training_session:** Stores class sessions (Id, TrainingDate, Capacity, Duration, Status, TargetLevel, InstructorId, MartialArt, SessionNotes, RawCurriculumJson, EditedCurriculumJson).
- **training_session_fighter_joint:** Links students to sessions (Id, TrainingSessionId, FighterId).
- **training_session_technique_joint:** Links techniques/drills to sessions (Id, SessionId, TechniqueId, DrillId, DurationMinutes).
- **video_metadata:** Stores video details (Id, UserId, Type, Title, Description, Url, YoutubeVideoId, FilePath, FileHash, UploadedAt, Duration, StudentIdentifier, MartialArt, AISummary, TechniqueTag).
- **ai_analysis_result:** Stores AI analysis (Id, VideoId, AnalysisJson, OverallDescription, Strengths, AreasForImprovement, GeneratedAt, LastUpdatedAt, UpdatedBy).
- **video_segment_feedback:** Links feedback to video segments (Id, VideoId, AnalysisJson, StartTimestamp, EndTimestamp, TechniqueId).
- **drills:** Stores drill data (Id, Name, Description, Focus, Duration, TechniqueId, AiAnalysisResultId, VideoId).
- **techniques:** Stores technique data (Id, Name, Description, CategoryId, TechniqueTypeId, AiAnalysisResultId, VideoId).
- **technique_type:** Stores technique type (Id, Name, PositionalScenarioId).
- **point_scoring_technique:** Stores point scoring technique (Id, Name, Description, Points, MartialArt).
- **positional_scenario:** Stores positional scenario (Id, Name, FocusModule, TargetLevel).
- **curriculum:** Defines a curriculum cycle (Id, Name, Level, Module, StartDate, EndDate).
- **curriculum_scenario:** Assigns weekly themes to curriculum (Id, CurriculumId, PositionalScenarioId, WeekNumber).
- **weakness_category:** Stores weakness categories (Id, Name, Description).
- **analysis_weakness:** Links weaknesses to AI analysis (Id, AiAnalysisResultId, WeaknessCategoryId).

### 6.3 API Endpoints (Key Examples)

- **POST /api/fighter/login:** Authenticate user.
- **POST /api/trainingsession:** Create session.
- **POST /api/video/upload:** Upload video for analysis.
- **PUT /api/video/feedback:** Save instructor-edited analysis.
- **GET /api/session/{id}/curriculum:** Retrieve session curriculum.

---

## 7. Implementation Plan

### 7.1 MVP Features

1. **Video Analysis Editor:** Completed (frontend and backend functional).
2. **Single-Session Curriculum Recommendation:** Develop within 2-3 weeks.
3. Complete backend functionalities for user authentication, class management, video upload/analysis, video analysis editor, curriculum recommendation, and the additional features: students list import, SSO, Freemium limits, and Stripe payment.

### 7.2 Timeline

- **Week 1-2:** Implement Single-Session Curriculum Recommendation.
- **Week 3:** Add Freemium limits and payment integration (Stripe).
- **Week 4:** Launch MVP, targeting 50 instructors with 50% discount ($7.50/month).
- **Weeks 4-7:** Complete backend development and testing for MVP features. Develop and launch the mobile application for instructors and students

### 7.3 Future Features (Post-MVP)

- Long-Term Curriculum Recommendation (personalized, 3-6 months).
- Partner-Matching for students based on training goals.
- Paying students can access the Instructor's feedbacks on training lessons in mobile app.

---

## 8. Assumptions and Risks

### 8.1 Assumptions

- Instructors will adopt the platform as time-saving tools.
- Free tier will drive initial user growth.
- Users will adopt the mobile application for in-class activities due to its convenience over carrying laptops.

### 8.2 Risks and Mitigation

- **Risk:** Low premium conversion rates.
  - **Mitigation:** Offer one-time payments and early adopter discounts.
- **Risk:** High cloud costs.
  - **Mitigation:** Enforce strict quotas for free-tier users.
- **Risk:** Usability issues for new users.
  - **Mitigation:** Include onboarding tutorials and tooltips.
- **Risk:** Delays in mobile app development due to technical challenges.
 - **Mitigation:** Allocate additional resources or adjust feature scope if necessary.
 - **Risk:** Low user adoption of the mobile app.
 - **Mitigation:** Conduct user testing and gather feedback to improve usability and feature set.

---

## 9. Appendices

### 9.1 Pricing Context

- **US BJJ Gyms:** $100–$200/month (average $160–$195).
- **Europe BJJ Gyms:** £80–£120 (\~$100–$150 USD).
- **Asia BJJ Gyms:** $70–$130 USD.
- **Implication:** $15–$20/month for instructors and $5–$10/month for students are competitive.

### 9.2 Market Context

- AI in sports market to reach USD 7.63 billion in 2025 (CAGR 28.69%).
- Demand for video analytics and personalized coaching aligns with product features.