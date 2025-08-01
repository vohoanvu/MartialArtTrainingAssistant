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
