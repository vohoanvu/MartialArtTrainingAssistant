# Software Design Specification (SDS)

## Martial Art Training Assistant Mobile App

**Version:** 1.0  
**Date:** May 16, 2025  
**Author:** Vo Hoan Vu (Solo Developer)  
**Prepared for:** Internal Development and Stakeholders

---

## 1. Introduction

### 1.1 Purpose
This Software Design Specification (SDS) outlines the design and implementation details for the mobile application of the Martial Art Training Assistant, a SaaS platform enhancing Brazilian Jiu-Jitsu (BJJ) training. The mobile app, developed using **React Native with Expo**, will deliver the Minimum Viable Product (MVP) features outlined in the Software Requirements Specification (SRS), optimized for in-class use by instructors and students. This document provides a blueprint for cross-platform development on iOS and Android, ensuring integration with existing backend APIs and consistency with the web app’s UI/UX.

### 1.2 Scope
The mobile app will support:
- **User Authentication**: Secure login/registration with email/password and SSO (Google, Facebook).
- **Class Session Management**: Create, join, and manage training sessions with AI-driven student pairing.
- **Student Sparring Video Upload**: Upload videos to Google Cloud Storage (GCS) or submit YouTube URLs for AI analysis.
- **Video Analysis Editor**: View and edit AI-generated video analysis (instructor-only).
- **Single-Session Curriculum Recommendation**: Generate and view AI-driven drill plans.
- **Freemium Limits**: Enforce free-tier restrictions (e.g., 1 video upload/month).
- **Stripe Payments**: Handle subscriptions and one-time payments.

The app will leverage **React Native Expo** for rapid development and deployment, integrating with the existing .NET 8.0 backend (`FighterManager.Server`, `VideoSharing.Server`).

### 1.3 Definitions, Acronyms, and Abbreviations
- **BJJ**: Brazilian Jiu-Jitsu
- **SaaS**: Software-as-a-Service
- **MVP**: Minimum Viable Product
- **React Native**: Cross-platform framework for iOS/Android
- **Expo**: Framework and platform for React Native development
- **GCS**: Google Cloud Storage
- **Gemini Vision**: Google Vertex AI Gemini 2.5 Pro for video analysis
- **SSO**: Single Sign-On
- **JWT**: JSON Web Token
- **ShadCN UI**: Component library used in web app

### 1.4 References
- React Native: https://reactnative.dev/
- Expo: https://docs.expo.dev/

---

## 2. System Overview

### 2.1 System Architecture
The mobile app follows a client-server architecture, integrating with the existing backend services:
- **Frontend (Mobile Client)**:
  - Built with **React Native** and **Expo** for cross-platform compatibility.
  - Uses **Tamagui** (a TailwindCSS-compatible UI library) for styling, ensuring consistency with web app’s ShadCN UI.
  - Manages state with **Zustand** (aligned with `authStore.ts`, `langStore.ts` in web app).
  - Handles API requests via **Axios** (mirroring `api.ts`).
- **Backend**:
  - .NET 8.0 Web APIs (`FighterManager.Server`, `VideoSharing.Server`).
  - PostgreSQL database with JSONB for analysis storage.
  - Google Cloud services (GCS, Vertex AI Gemini Vision).
- **Communication**:
  - RESTful APIs with JWT authentication.
  - SignalR for real-time notifications (e.g., video upload confirmations).
- **Deployment**:
  - Expo EAS (Expo Application Services) for build and distribution.
  - Hosted on App Store and Google Play post-MVP.

### 2.2 System Components
- **UI Components**: Reusable components (e.g., Button, Input, Card) adapted from web app’s ShadCN UI, implemented via Tamagui.
- **Navigation**: **React Navigation** for stack and tab navigation.
- **State Management**: Zustand for authentication, session, and feature state.
- **API Client**: Axios for HTTP requests to backend endpoints.
- **File Upload**: Expo’s `expo-document-picker` and `expo-image-picker` for video uploads.
- **SSO**: Expo’s `expo-auth-session` for Google/Facebook authentication.
- **Payments**: Stripe SDK for React Native (`@stripe/stripe-react-native`).

---

## 3. Design Considerations

### 3.1 Assumptions
- Users prefer mobile apps for in-class activities due to portability (per SRS update).
- Backend APIs are stable and support mobile client requirements.
- Expo’s managed workflow meets MVP needs without native module complexity.

### 3.2 Constraints
- **Budget**: Limited solo developer resources; prioritize Expo’s managed workflow to reduce costs.
- **Timeline**: 6–8 weeks post-backend completion for mobile MVP launch.
- **Compatibility**: Support iOS 14+ and Android 10+ (covering ~90% of devices as of 2025).
- **Styling**: Must align with web app’s TailwindCSS/ShadCN UI aesthetic.

---

## 4. System Design

### 4.1 Component Diagram
```
[Mobile App (React Native)]
  ├── [UI Components]
  │    ├── Button, Input, Card (Tamagui)
  │    ├── VideoPlayer (expo-av)
  │    └── Toast (react-native-toast-message)
  ├── [Navigation]
  │    ├── StackNavigator (Auth, Main)
  │    └── TabNavigator (Sessions, Videos, Profile)
  ├── [State Management]
  │    ├── authStore (Zustand)
  │    ├── sessionStore (Zustand)
  │    └── videoStore (Zustand)
  ├── [API Client]
  │    ├── Axios (REST API calls)
  │    └── SignalR Client (real-time)
  ├── [Services]
  │    ├── AuthService (SSO, JWT)
  │    ├── FileUploadService (expo-document-picker)
  │    └── PaymentService (Stripe)
  └── [External Integrations]
       ├── GCS (Video storage)
       ├── Google/Facebook SSO (expo-auth-session)
       └── Stripe (Payments)
       └── [Backend APIs]
            ├── FighterManager.Server
            ├── VideoSharing.Server
```

### 4.2 Data Flow
1. **User Authentication**:
   - User logs in via SSO or email/password (`POST /api/fighter/login`).
   - JWT stored securely using `expo-secure-store`.
   - Auth state managed in `authStore`.
2. **Class Session Management**:
   - Instructor creates session (`POST /api/trainingsession`).
   - Student checks in (`PUT /api/trainingsession/{id}`).
   - Pairing data fetched (`GET /api/session/{id}/pairs`).
3. **Video Upload**:
   - User selects video (`expo-document-picker`).
   - Video uploaded to GCS (`POST /api/video/upload`).
   - AI analysis results stored (`ai_analysis_result` table).
4. **Video Analysis Editor**:
   - Instructor views analysis (`GET /api/video/{id}/analysis`).
   - Edits saved (`PUT /api/video/feedback`).
5. **Curriculum Recommendation**:
   - Generated via `GET /api/session/{id}/curriculum`.
   - Displayed as a list of drills.

### 4.3 Database Interaction
- Reuse existing PostgreSQL schema (`SharedEntities/Data/DatabaseContext.cs`):
  - `users`, `fighter`, `training_session`, `video_metadata`, `ai_analysis_result`, `drills`.
- New table: `waitlist` for beta sign-ups (already proposed).
- Mobile app interacts via REST APIs, no direct database access.

---

## 5. User Interface Design

### 5.1 UI Framework
- **Tamagui**: Provides TailwindCSS-like styling and cross-platform components, mimicking ShadCN UI’s aesthetic (`button.tsx`, `input.tsx`).
- **Expo AV**: For video playback in the analysis editor.
- **React Native Toast Message**: For notifications (success/error toasts).

### 5.2 Screen Hierarchy
- **Auth Stack**:
  - LoginScreen: Email/password + Google/Facebook SSO buttons.
  - RegisterScreen: Form for email, password, role, with SSO option.
- **Main Tab Navigator**:
  - SessionsScreen: List of active sessions; create/join options.
  - VideosScreen: Upload video, view analysis (instructor: edit analysis).
  - ProfileScreen: User details, subscription status, logout.
- **Modal Screens**:
  - SessionDetailsModal: View session details, pairings, curriculum.
  - VideoAnalysisModal: Edit analysis, specify segments.

### 5.3 UI Mockups
- **LoginScreen**: Centered form with SSO buttons, branded logo, primary color (`bg-primary`).
- **SessionsScreen**: Scrollable list of session cards (`Card`), with "Create Session" FAB (Floating Action Button).
- **VideosScreen**: Grid of video thumbnails, upload button, instructor-only edit mode.
- **ProfileScreen**: User info card, Stripe payment button for premium.

### 5.4 Styling
- **Colors**: Reuse web app’s TailwindCSS palette (`bg-primary`, `text-foreground`, `bg-muted`).
- **Typography**: System fonts (San Francisco for iOS, Roboto for Android) via Tamagui.
- **Components**: Map ShadCN UI to Tamagui (e.g., `Button` with `bg-primary hover:bg-accent`).

---

## 6. API Integration

### 6.1 API Client Implementation sample code
- **Axios Setup** (`src/services/api.ts`):
  ```tsx
  import axios from 'axios';
  import * as SecureStore from 'expo-secure-store';

  const api = axios.create({
    baseURL: 'http://localhost:8081', // Update for production
    headers: { 'Content-Type': 'application/json' },
  });

  api.interceptors.request.use(async (config) => {
    const token = await SecureStore.getItemAsync('jwt');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  });

  export const joinWaitlist = (data: { email: string; role: string; region: string }) =>
    api.post('/api/waitlist', data);
  export const login = (data: { email: string; password: string }) =>
    api.post('/api/fighter/login', data);
  // Add other endpoints
  ```
- **SignalR**: Use `@microsoft/signalr` for real-time notifications:
  ```tsx
  import * as SignalR from '@microsoft/signalr';

  const hubConnection = new SignalR.HubConnectionBuilder()
    .withUrl('http://localhost:8082/videoShareHub')
    .build();

  hubConnection.on('ReceiveVideoSharedNotification', (message, title, user) => {
    // Show toast
  });
  ```

---

## 7. Implementation Details

### 7.1 Project Structure
```
mobile-app/
├── src/
│   ├── components/
│   │   ├── Button.tsx
│   │   ├── Input.tsx
│   │   └── Card.tsx
│   ├── screens/
│   │   ├── LoginScreen.tsx
│   │   ├── SessionsScreen.tsx
│   │   ├── VideosScreen.tsx
│   │   └── ProfileScreen.tsx
│   ├── services/
│   │   ├── api.ts
│   │   └── signalr.ts
│   ├── stores/
│   │   ├── authStore.ts
│   │   ├── sessionStore.ts
│   │   └── videoStore.ts
│   ├── navigation/
│   │   └── AppNavigator.tsx
│   ├── assets/
│   │   └── logo.png
│   └── theme/
│       └── tamagui.config.ts
├── app.json
├── package.json
└── README.md
```

### 7.2 Key Dependencies
- `react-native`, `expo` (~0.73.0)
- `@tamagui/core`, `@tamagui/react-native`
- `zustand`
- `axios`
- `@microsoft/signalr`
- `expo-document-picker`, `expo-image-picker`, `expo-av`
- `expo-auth-session`
- `@stripe/stripe-react-native`
- `react-native-toast-message`
- `@react-navigation/native`, `@react-navigation/stack`, `@react-navigation/bottom-tabs`
- `expo-secure-store`

### 7.3 Sample Component (Button.tsx)
```tsx
import { Button as TamaguiButton } from '@tamagui/core';
import { styled } from '@tamagui/core';

const Button = styled(TamaguiButton, {
  backgroundColor: '$primary',
  color: '$primary-foreground',
  paddingVertical: 12,
  paddingHorizontal: 24,
  borderRadius: 8,
  variants: {
    disabled: {
      true: { backgroundColor: '$muted', color: '$muted-foreground' },
    },
  },
});

export default Button;
```

### 7.4 Sample Screen (LoginScreen.tsx)
```tsx
import React, { useState } from 'react';
import { YStack, XStack } from '@tamagui/core';
import Button from '../components/Button';
import Input from '../components/Input';
import { login, googleSSO } from '../services/api';
import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from '../stores/authStore';

const LoginScreen: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const setAuth = useAuthStore((state) => state.setAuth);

  const handleLogin = async () => {
    try {
      const response = await login({ email, password });
      await SecureStore.setItemAsync('jwt', response.data.accessToken);
      setAuth({ token: response.data.accessToken, user: response.data.user });
    } catch (error) {
      // Show toast
    }
  };

  return (
    <YStack flex={1} justifyContent="center" padding={16} backgroundColor="$background">
      <Input
        placeholder="Email"
        value={email}
        onChangeText={setEmail}
        marginBottom={12}
      />
      <Input
        placeholder="Password"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        marginBottom={12}
      />
      <Button onPress={handleLogin}>Login</Button>
      <XStack justifyContent="center" marginTop={16}>
        <Button onPress={googleSSO}>Google SSO</Button>
        {/* Add Facebook SSO */}
      </XStack>
    </YStack>
  );
};

export default LoginScreen;
```

---

## 8. Implementation Plan

### 8.1 Development Phases
- **Week 1–2**: Set up Expo project, configure Tamagui, implement auth screens (Login, Register).
- **Week 3–4**: Develop SessionsScreen, VideosScreen, and ProfileScreen with API integration.
- **Week 5**: Implement video upload and analysis editor with Expo AV and file picker.
- **Week 6**: Add curriculum recommendation display and Stripe payments.
- **Week 7–8**: Testing (unit, integration), UI polish, and EAS build for App Store/Google Play.

### 8.2 Testing Strategy
- **Unit Tests**: Use Jest for components and stores (`src/__tests__/Button.test.tsx`).
- **Integration Tests**: Test API flows (e.g., login, video upload) with mocked backend.
- **E2E Tests**: Use Detox for key user flows (login, session creation).
- **Manual Testing**: Verify UI on iOS 14+ and Android 10+ devices via Expo Go.

### 8.3 Deployment
- Use Expo EAS for building and submitting to App Store/Google Play.
- Configure OTA (Over-the-Air) updates for post-launch bug fixes.
- Monitor crashes via Expo’s error tracking and backend logs (`Serilog`).

---

## 9. Risks and Mitigation

- **Risk**: Expo’s managed workflow limitations (e.g., native modules).
  - **Mitigation**: Use Expo’s dev-client for custom native code if needed.
- **Risk**: UI inconsistencies across platforms.
  - **Mitigation**: Test extensively on iOS/Android; leverage Tamagui’s platform-specific styling.
- **Risk**: Performance issues with video playback.
  - **Mitigation**: Optimize with `expo-av` caching and low-resolution previews.
- **Risk**: Delayed App Store approval.
  - **Mitigation**: Submit early, ensure GDPR/CCPA compliance in privacy policy.

---

## 10. Appendices

### 10.1 Development Environment
- **Tools**: VS Code, Expo CLI, Xcode (for iOS simulator), Android Studio.
- **Node.js**: v18+.
- **Expo SDK**: 51+.

### 10.2 Sample API Request
```tsx
const createSession = async (data: { trainingDate: string; capacity: number; duration: number }) => {
  const response = await api.post('/api/trainingsession', data);
  return response.data;
};
```

-----------------------
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
