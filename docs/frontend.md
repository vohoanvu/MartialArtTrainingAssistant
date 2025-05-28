# Frontend Implementation Plan: CodeJitsu BJJ Martial Art Training Assistant

**Version:** 1.0
**Date:** July 24, 2024

## 1. Component Structure

This section outlines the key UI components required to build the features described in the project specifications. Components are organized by feature area. We will leverage Shadcn UI components as a base and customize or create new ones as needed.

### 1.1 Core UI Components (from `components/ui/`)
-   **Button:** (Existing `Button.tsx`) Standard buttons for actions.
-   **Input:** (Existing `Input.tsx`) Standard text, number, email, password inputs.
-   **Textarea:** (Existing `Textarea.tsx`) For multi-line text input.
-   **Select:** (Existing `Select.tsx`) Dropdown selectors for roles, belt ranks, etc.
-   **Card:** (Existing `Card.tsx`) For displaying grouped information (e.g., session details, video info).
-   **Table:** (Existing `Table.tsx` & `DataTable.tsx`) For displaying lists of sessions, students, videos.
-   **Dialog/Modal:** (Existing `ConfirmationDialog.tsx`) For confirmations, and can be adapted for forms like "Create New Session" if not a full page.
-   **Toast/Toaster:** (Existing `toast.tsx`, `toaster.tsx`) For notifications.
-   **LoadingSpinner/PingAnim:** (Existing `PingAnim.tsx`) To indicate loading states.

### 1.2 Authentication Components (`components/Auth/`)
-   **LoginForm:**
    -   Inputs for email and password.
    -   Submit button.
    -   Links/buttons for SSO (Google, Facebook).
    -   Displays error messages.

-   **RegistrationForm:**
    -   Inputs for email, password, role, and all `Fighter` profile fields (name, height, weight, gender, birthdate, experience, belt rank, max workout duration).
    -   BMI calculated or input.
    -   Submit button.
    -   Displays validation errors.
-   **SSOCallbackHandler:** (Page component - `pages/SSOCallback.tsx`) Handles redirection from SSO providers, extracts tokens, and updates auth state.

### 1.3 Class Session Management Components (`components/ClassSessionManagement/`)
-   **TrainingSessionCard:**
    -   Displays summary information for a single training session (date, time, instructor, capacity).
    -   Actions: "View Details", "Check-in" (for students), "Manage" (for instructors).
-   **TrainingSessionList:**
    -   Displays a list of `TrainingSessionCard` components.
    -   May include filtering/sorting options.
-   **TrainingSessionForm:** (Existing `TrainingSessionForm.tsx`)
    -   Inputs for session details (date, time, capacity, description, target level, martial art).
    -   Used for creating and editing sessions.
-   **TrainingSessionDetailsDisplay:** (Part of `pages/TrainingSessionDetails.tsx`)
    -   Displays full details of a session, including instructor info, student roster, generated pairings.
    -   Buttons for instructor actions: "Take Attendance", "Pair Up Students", "Generate Curriculum".
-   **AttendancePage:** (Existing `AttendancePage.tsx`)
    -   Editable table (`DataTable`) for instructors to input walk-in student details.
    -   Columns for name, birthdate, weight, height, belt, gender.
    -   "Finalize Attendance" button.
-   **StudentPairingDisplay:**
    -   Renders a list of student pairs.
-   **CurriculumDisplay:**
    -   Structured display of the AI-generated curriculum: Warm-Up, Techniques, Drills, Sparring, Cool-Down.
    -   Each section should be collapsible/expandable.
    -   Drill items include name, description, focus, duration, and a "Start Timer" button.
    -   Technique items include name, description, tips.
    -   Sparring section includes description, guidelines, duration.

### 1.4 Video Management & Analysis Components (`components/VideoAnalysisEditor/`)
-   **VideoUploadForm:** (Existing `VideoUploadForm.tsx`)
    -   File input for video (MP4, AVI).
    -   Text input for YouTube URL.
    -   Input for description, student identifier (for sparring), martial art selection.
    -   Upload progress bar.
-   **VideoPlayer:** (Existing `VideoPlayer.tsx`)
    -   HTML5 video player with custom controls.
    -   Timeline with markers for AI-identified techniques.
    -   Ability to select time segments on the timeline (drag and drop or click-to-set start/end).
    -   Seek functionality.
-   **TechniqueFeedback:** (Existing `TechniqueFeedback.tsx`)
    -   Tabbed interface for "Identified Techniques", "Suggested Drills", "Overall Analysis".
-   **TechniquesEditorial:** (Existing `TechniquesEditorial.tsx`)
    -   Displays a list of AI-identified or manually added techniques.
    -   Each technique item is collapsible and editable:
        -   Name (editable inline or via modal).
        -   Description (Textarea).
        -   Technique Type (Select).
        -   Positional Scenario (Select).
        -   Start/End Timestamps (Input, clickable to seek video).
        -   Delete button.
    -   "Create New Technique" form.
-   **DrillsEditorial:** (Existing `DrillsEditorial.tsx`)
    -   Displays a list of suggested or manually added drills.
    -   Each drill item is collapsible and editable:
        -   Name, Focus, Duration (Input).
        -   Description (Textarea).
        -   Related Technique (Select, populated from identified techniques).
        -   Delete button.
    -   "Create New Drill" form.
-   **OverallAnalysisEditorial:** (Existing `OverallAnalysisEditorial.tsx`)
    -   Textarea for "Overall Description".
    -   Editable lists for "Strengths" and "Areas for Improvement". Each item has a description (Textarea) and an optional related technique (Select).
    -   Add/Delete buttons for strengths/areas.
-   **StudentDetailsDisplay:** (Existing `StudentFighterDetails.tsx`)
    -   Displays read-only fighter profile information relevant to the video being reviewed.
-   **AiAnalysisResultsDisplay:** (Existing `AiAnalysisResults.tsx`)
    -   Displays raw AI JSON output for debugging or advanced review.

### 1.5 Shared Components
-   **Navbar:** (Existing `Navbar.tsx`) Main application navigation.
-   **NotificationPopup:** (Existing `NotificationPopup.tsx`) For real-time SignalR notifications.
-   **Layout:** (Existing `Layout.tsx`) Main page structure including Navbar and Outlet for page content.

## 2. State Management (Zustand)

-   **`authStore` (Existing):**
    -   `accessToken`, `refreshToken`: Store JWTs.
    -   `user`: Stores authenticated user's profile information (including `FighterInfo`).
    -   `loginStatus`: Tracks 'authenticated', 'unauthenticated', 'pending'.
    -   Actions: `setTokens`, `clearTokens`, `setUser`, `clearUser`, `setLoginStatus`, `login`, `registerFighter`, `logout`, `hydrate` (for token refresh), `getUserInfo`.
-   **`attendanceStore` (Existing):**
    -   `sessionRecords`: An object where keys are `sessionId` and values are arrays of `AttendanceRecordDto`.
    -   Actions: `setSessionRecords`, `updateRecord` (for a specific field in a record), `addEmptyRecords`, `clearSessionRecords`. This store persists to local storage to prevent data loss during walk-in attendance entry.
-   **`videoAnalysisStore` (New - Suggested):**
    -   `currentVideoDetails`: Stores `UploadedVideoDto` for the video being reviewed/edited.
    -   `currentAnalysisResult`: Stores `AnalysisResultDto` (the structured data being edited).
    -   `isLoading`, `error`: For fetching/saving analysis data.
    -   Actions:
        -   `fetchVideoAndAnalysis(videoId)`: Fetches both video details and its analysis.
        -   `updateAnalysisField(path, value)`: Updates a deeply nested field in `currentAnalysisResult` (e.g., `techniques[0].name`).
        -   `addTechnique(newTechnique)`, `deleteTechnique(index)`.
        -   `addDrill(newDrill)`, `deleteDrill(index)`.
        -   `addStrength()`, `deleteStrength(index)`.
        -   `addAreaForImprovement()`, `deleteAreaForImprovement(index)`.
        -   `saveCurrentAnalysis()`: Calls the API to persist changes.
        -   `setSelectedSegment(start, end)`: Stores the currently selected video segment from the player.
        -   `clearSelectedSegment()`.
-   **`uiStore` (Optional - for global UI states):**
    -   `isMobileMenuOpen`: If a global mobile menu state is needed.
    -   `activeModal`: To manage globally accessible modals (though often modals are component-local).

## 3. UI/UX Guidelines

-   **Consistency:** Maintain a consistent look and feel across the web and mobile applications, leveraging Shadcn UI and Tamagui's capabilities to adapt designs.
-   **Clarity:** Forms should have clear labels, placeholders, and validation messages. Important actions should be prominent.
-   **Responsiveness:**
    -   Web: Primarily designed for desktop/laptop use by instructors, but should be usable on tablets.
    -   Mobile: Optimized for touch interactions and smaller screens.
-   **Feedback:**
    -   Provide immediate visual feedback for user actions (e.g., button clicks, saving data).
    -   Use loading indicators (spinners, progress bars) for asynchronous operations.
    -   Employ toasts for success/error notifications.
-   **Accessibility:** Follow WCAG guidelines. Use semantic HTML, provide ARIA attributes where necessary, ensure keyboard navigability, and sufficient color contrast. Shadcn UI components generally have good accessibility built-in.
-   **Dark/Light Mode:** Support theme toggling using existing `ModeToggle` and `themeStore`.
-   **Internationalization:** Ensure all user-facing text is translatable using `i18next` and the JSON translation files.
-   **Video Player Interactivity:**
    -   The timeline should clearly indicate identified techniques and allow seeking.
    -   Segment selection should be intuitive (e.g., drag-to-select or click-start/click-end).
    -   Selected segments should be visually highlighted on the timeline.

## 4. Page Layouts (Key Screens)

### 4.1 Public Pages
-   **Landing Page (`pages/LandingPage.tsx`):**
    -   Hero section with value proposition.
    -   Feature highlights.
    -   Testimonials.
    -   Waitlist signup form (Email, Role, Region).
-   **Login Page (`pages/Login.tsx`):**
    -   `LoginForm` component.
    -   Link to Register page.
-   **Registration Page (`pages/Register.tsx`):**
    -   `RegistrationForm` component.
    -   Link to Login page.
-   **SSO Callback Page (`pages/SSOCallback.tsx`):**
    -   Minimal UI, primarily for processing SSO response and redirecting. Displays "Signing you in..."

### 4.2 Authenticated User Pages (General Access)
-   **Home/Dashboard (`pages/Home.tsx` or a dedicated dashboard):**
    -   Could display a welcome message, quick links, or a summary of recent activity.
    -   Currently, `Home.tsx` seems to be the main landing for logged-in users, might need refactoring if a true "dashboard" is desired.
-   **Share Video Page (`pages/SharingVideo.tsx`):**
    -   Contains `VideoSharingFormComponent` which includes:
        -   Form for sharing YouTube URLs.
        -   `VideoUploadForm` for direct GCS uploads (sparring/demonstration based on role).
-   **Video Listing (User's own uploads) (`pages/VideoStorageListing.tsx`):**
    -   Table displaying user's uploaded videos (from GCS).
    -   Columns: FilePath/Title, Description, Upload Date, Martial Art, Student Name (if applicable).
    -   Actions per video: "Review/Edit Analysis" (navigates to `VideoReview` page), "Delete", "View Raw AI Analysis" (opens modal with `AiAnalysisResults`).
-   **Shared Videos List (`pages/VideoShareList.tsx`):**
    -   Displays videos shared via YouTube URLs by all users (`SharedVideoList` component).
    -   Each item shows embedded player, title, description, sharer's username.

### 4.3 Instructor-Specific Pages
-   **Class Session Management (`pages/Dashboard.tsx` - acting as ClassSession page):**
    -   Displays a list/table of training sessions created by the instructor.
    -   "Create New Session" button (navigates to `TrainingSessionForm`).
    -   Actions per session: "Edit" (navigates to `TrainingSessionForm` with session ID), "Manage" (navigates to `TrainingSessionDetails`).
-   **Create/Edit Training Session Page (`components/ClassSessionManagement/TrainingSessionForm.tsx` - rendered by a route):**
    -   Form for all session details.
-   **Training Session Details Page (`pages/TrainingSessionDetails.tsx`):**
    -   Displays full session details (`TrainingSessionDetailsDisplay` component).
    -   Includes student roster, pairings.
    -   Buttons for "Take Attendance" (opens `AttendancePage` likely as a modal or overlay), "Pair Up Students", "Generate Today's Lessons".
    -   Displays the AI-generated curriculum (`CurriculumDisplay` component) once generated.
-   **Video Review Page (`pages/VideoReview.tsx`):**
    -   Two-column layout:
        -   Left: `VideoPlayer` and `StudentDetailsDisplay`.
        -   Right: `TechniqueFeedback` component (with tabs for Techniques, Drills, Overall Analysis).
-   **Take Attendance Page (`components/ClassSessionManagement/AttendancePage.tsx` - likely rendered as a modal/overlay from Session Details):**
    -   Editable table for walk-in student data entry.

### 4.4 Mobile Application Layouts (Conceptual)
-   **Login/Register Screens:** Simplified forms.
-   **Main Tab Navigator:**
    -   **Sessions Tab:** List of sessions; create/join options.
    -   **Videos Tab:** Upload video, view own analyses (students), view/edit student analyses (instructors).
    -   **Profile Tab:** User details, subscription status, logout.
-   **Modal Screens:**
    -   **Session Details Modal:** View session details, pairings, curriculum.
    -   **Video Analysis Modal (Instructor):** Simplified editor for key feedback points.
    -   **Curriculum Viewer Modal (Instructor/Student):** Display today's lesson plan with drill timers.

This structure provides a foundation for developing the frontend. Specific component implementations will involve fetching data using functions from `services/api.ts`, managing state with Zustand stores, and rendering UI elements with Shadcn components and custom styling.