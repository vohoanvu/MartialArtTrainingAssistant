# CodeJitsu: BJJ Martial Art Training Assistant - Requirements Document

## 1. Project Overview

The CodeJitsu BJJ Training Assistant is a Software-as-a-Service (SaaS) platform designed to enhance Brazilian Jiu-Jitsu (BJJ) training for both instructors and students. The system aims to streamline training processes and improve skill development through AI-driven video analysis, comprehensive class session management, and intelligent curriculum recommendations.

The platform will provide features such as secure user authentication, tools for instructors to create and manage class sessions (including walk-in attendance and student pairing), and capabilities for students to upload sparring videos for AI analysis using Google Vertex AI Gemini Vision. Instructors will be able to review and edit these AI-generated analyses. A key Minimum Viable Product (MVP) feature is the single-session curriculum recommendation, which suggests AI-driven drill plans based on student data.

The system will be accessible via a web application (primarily for instructors to review and edit feedback) and a mobile application (for both instructors and students for in-class activities and curriculum planning). It will operate on a Freemium model, offering basic features for free and premium features via subscription.

## 2. Functional Requirements

### FR1: User Management & Authentication
- **FR1.1 User Registration:** The system shall allow users (instructors and students) to register for an account using an email address and password.
- **FR1.2 User Login:** The system shall allow registered users to log in securely using their email and password.
- **FR1.3 Role Assignment:** The system shall assign "Instructor" or "Student" roles to users upon registration, restricting access to role-specific features.
- **FR1.4 Single Sign-On (SSO):**
    - **FR1.4.1:** Users shall be able to register and log in using Google Single Sign-On.
    - **FR1.4.2:** Users shall be able to register and log in using Facebook Single Sign-On.
    - **FR1.4.3:** The system shall integrate with OAuth 2.0 for secure SSO authentication.
    - **FR1.4.4:** User profiles shall be automatically created or linked to existing accounts based on SSO data.
- **FR1.5 Fighter Profile:** Users shall have an associated "Fighter" profile containing details such as name, height, weight, BMI, gender, role, birthdate, experience, belt rank, and max workout duration.

### FR2: Class Session Management
- **FR2.1 Session Creation:** Instructors shall be able to create a new class session, specifying details such as date, time, capacity, description, martial art, and target skill level.
- **FR2.2 Student Check-in:** Registered students shall be able to check into an active class session.
- **FR2.3 Student Pairing:** The system shall automatically pair students for partner-based exercises based on their size (height, weight) and skill level (belt rank).
- **FR2.4 Session Viewing:** Instructors shall be able to view details of their sessions, including a list of checked-in students and generated pairings.
- **FR2.5 Walk-in Student Attendance:**
    - **FR2.5.1:** Instructors shall be able to record attendance for walk-in students (not previously registered in the system) using a form-based interface within the "Take Attendance" feature.
    - **FR2.5.2:** Data entered for walk-in students (name, birthdate, weight, height, belt color, gender) shall be used to create new "walk-in" Fighter records in the database, associated with the current session.
- **FR2.6 Bulk Student Import (Excel):**
    - **FR2.6.1:** Instructors shall be able to import a list of students from an Excel spreadsheet.
    - **FR2.6.2:** The system shall validate the spreadsheet format and map data to student profiles.
    - **FR2.6.3:** Imported students shall be added to the database with default roles and profiles.

### FR3: Video Upload and AI Analysis
- **FR3.1 Video Submission:**
    - **FR3.1.1:** Students shall be able to upload sparring videos directly to Google Cloud Storage (GCS) through the platform.
    - **FR3.1.2:** Students shall be able to share YouTube video URLs for analysis.
- **FR3.2 AI-Powered Analysis:**
    - **FR3.2.1:** The system shall process submitted videos using Google Vertex AI Gemini Vision to generate an analysis.
    - **FR3.2.2:** The AI analysis shall identify techniques performed, assess strengths, and highlight areas for improvement.
    - **FR3.2.3:** For instructor-uploaded footage of students, the AI system shall analyze the performance of *both* fighters visible in the video. For self-uploaded videos, the analysis shall focus on the video's author/student.
- **FR3.3 Analysis Storage:** Analysis results, including identified techniques (as JSONB), strengths (as text), and areas for improvement (as text), shall be stored in the PostgreSQL database.
- **FR3.4 Personalized Feedback Distribution:**
    - **FR3.4.1:** If a student uploads a video and requests instructor review, the system shall allow instructors to approve or edit the AI-generated analysis.
    - **FR3.4.2:** The system shall facilitate the distribution of this instructor-approved/edited analysis to the respective student via email.

### FR4: Video Analysis Editor (Instructor Feature)
- **FR4.1 Analysis Review:** Instructors shall be able to view student-submitted videos alongside the AI-generated analysis (techniques, strengths, areas for improvement).
- **FR4.2 Analysis Editing:** Instructors shall be able to edit the AI-generated analysis data, including:
    - Modifying identified techniques (e.g., name, type, positional scenario).
    - Editing descriptions of strengths.
    - Refining areas for improvement.
- **FR4.3 Video Segmentation:** Instructors shall be able to specify and link feedback to specific video segments by defining start and end timestamps.
- **FR4.4 Saving Edits:** Edited analysis data shall be saved to the PostgreSQL database, maintaining a link to the original AI analysis.
- **FR4.5 Data Retrieval for Display & Fine-tuning:** The system shall retrieve edited analysis data for display to users and for potential future use in fine-tuning the AI models.

### FR5: Single-Session Curriculum Recommendation (MVP)
- **FR5.1 Curriculum Generation:** The system shall generate a curriculum for a single class session based on:
    - Aggregated data of checked-in students (age, height, weight, belt rank, gender).
    - Analyzed weaknesses from uploaded videos if students have used the video analysis feature.
- **FR5.2 Drill Suggestion:** The curriculum shall include 5-7 drills designed to address common weaknesses identified (e.g., takedown defense, guard retention for BJJ).
- **FR5.3 Skill-Level Tailoring:** Drills shall be tailored to the class’s average skill level (derived from belt ranks) and the session's defined target level.
- **FR5.4 Curriculum Display:** Instructors shall be able to view the generated curriculum in an intuitive user interface, showing drill names, detailed descriptions, and the weakness categories each drill addresses.
- **FR5.5 Instructor Feedback on Curriculum:** The system shall allow instructors to provide feedback on the generated curriculum to aid future improvements of the recommendation engine.
- **FR5.6 Drill Time Tracking:** For each recommended drill or exercise, the system shall provide a time tracker to help instructors manage student pair sparring and drill durations.

### FR8: Freemium Model & Payments
- **FR8.1 Free Tier Access:**
    - **FR8.1.1:** Free-tier users shall have access to basic authentication features.
    - **FR8.1.2:** Free-tier users shall be limited to one (1) video upload per month for AI analysis.
    - **FR8.1.3:** AI analysis for free-tier users shall be simplified, limited to one technique, one strength, and one improvement per video.
    - **FR8.1.4:** Free-tier users shall be limited to creating/managing one (1) active class session at a time.
- **FR8.2 Upgrade Prompts:** The system shall display prompts to upgrade to a premium plan when free-tier users reach their usage limits.
- **FR8.3 Premium Tier Access (Instructors):**
    - **FR8.3.1:** Premium-tier instructors shall have access to unlimited video analysis.
    - **FR8.3.2:** Premium-tier instructors shall have access to full video analysis editing features.
    - **FR8.3.3:** Premium-tier instructors shall have access to unlimited curriculum recommendations.
    - **FR8.3.4:** The subscription cost for premium instructors shall be $15–$20 per month.
- **FR8.4 One-Time Payments:** The system shall offer one-time payment options (e.g., $5 for 3 video analyses or 1 curriculum suggestion).
- **FR8.5 Stripe Payment Integration:**
    - **FR8.5.1:** The system shall integrate with Stripe for processing subscription payments.
    - **FR8.5.2:** The system shall integrate with Stripe for processing one-time payments.
    - **FR8.5.3:** Payment status shall be stored and used to grant or restrict access to premium features.
- **FR8.6 Usage Tracking:** The system shall track feature usage (e.g., video uploads) to enforce free-tier limits.
- **FR8.7 Limit Notifications:** Users shall receive notifications when approaching or exceeding their free-tier usage limits.

## 3. Non-Functional Requirements

### NFR1: Performance
- **NFR1.1 Concurrency & API Response:** The system shall handle up to 100 concurrent users with API call response times under 2 seconds.
- **NFR1.2 Video Analysis Time:** AI video analysis processing for a 5-minute video shall complete within 5 minutes.

### NFR2: Scalability
- **NFR2.1 User Growth:** The system architecture shall be designed to scale to support up to 1,000 users within six (6) months post-launch, primarily through cloud resource optimization.

### NFR3: Security
- **NFR3.1 Data Encryption:** Videos stored in Google Cloud Storage (GCS) shall be encrypted at rest.
- **NFR3.2 Data Privacy & Compliance:**
    - **NFR3.2.1:** User data handling shall comply with GDPR and CCPA regulations.
    - **NFR3.2.2:** The system shall obtain opt-in consent from users for data usage related to AI model improvement and personalized features.
- **NFR3.3 Authentication Security:** User authentication shall utilize JSON Web Tokens (JWT), and passwords shall be stored using secure hashing algorithms.

### NFR4: Usability
- **NFR4.1 User Interface (UI) Intuition:** The UI shall be intuitive, with clear navigation, particularly for instructors accessing video analysis and curriculum planning tools.
- **NFR4.2 User Assistance:** The system shall provide tooltips and help text to guide new users.

### NFR5: Reliability
- **NFR5.1 System Uptime:** The system shall achieve 99.9% uptime, excluding scheduled maintenance periods.
- **NFR5.2 Data Backup:** The PostgreSQL database shall be backed up daily to prevent data loss.

### NFR6: Cost Efficiency
- **NFR6.1 Cloud Cost Management:** Cloud service costs (GCS, Vertex AI) shall be managed and minimized, including implementing quotas for free-tier users (e.g., a 500MB video storage limit per free-tier user).

### NFR7: Mobile Application Requirements
- **NFR7.1 Responsiveness:** The mobile application (iOS and Android via React Native Expo) shall be responsive and optimized for various screen sizes and orientations.
- **NFR7.2 Performance:** The mobile application shall perform efficiently with minimal latency, especially for real-time interactions during class sessions.
- **NFR7.3 Mobile Security:** User data handled by the mobile application shall be secured in compliance with mobile platform security standards.

## 4. Dependencies and Constraints

### Dependencies
- **Google Cloud Platform (GCP):**
    -   Google Cloud Storage (GCS) for video file storage.
    -   Google Vertex AI (specifically Gemini 2.5 Pro model) for AI-driven video analysis and curriculum recommendations.
- **Database:**
    -   PostgreSQL for relational data storage, including JSONB support for flexible analysis results.
- **Payment Processing:**
    -   Stripe (or PayPal as an alternative) for handling Freemium subscriptions and one-time payments.
- **External Authentication:**
    -   Google OAuth 2.0 for Google SSO.
    -   Facebook OAuth 2.0 for Facebook SSO.
- **Frontend Technologies:**
    -   React with Vite.
    -   TailwindCSS for styling.
- **Backend Technologies:**
    -   .NET 8.0 Web API.
- **Mobile Technologies:**
    -   React Native with Expo for cross-platform mobile app development.
- **Deployment & Orchestration:**
    -   Docker for containerization of all services (frontend, backend APIs, database).
    -   Docker Compose for local development and multi-container application management.
    -   Nginx for serving the React frontend and as a reverse proxy in a Dockerized environment.

### Constraints
- **Budget:** The project is developed by a solo developer with limited financial resources, necessitating a focus on low-cost cloud services and efficient development practices.
- **Timeline:** The MVP must be launched as soon as possible to start generating cash flow. Specific feature timelines (e.g., Single-Session Curriculum Recommendation in 2-3 weeks) are noted in the implementation plan.
- **Technology Stack:** The core technology stack (.NET 8, React, PostgreSQL, Docker, GCP) is defined and should be adhered to.
- **Solo Developer:** Resource constraints inherent in a solo-developer project impact the pace and breadth of concurrent development.
- **Mobile App Development:** Delays in mobile app development due to technical challenges with React Native Expo or platform-specific issues are a potential risk.