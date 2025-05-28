# Tech Stack Recommendations: CodeJitsu BJJ Martial Art Training Assistant

## 1. Introduction

This document outlines the recommended technology stack for the CodeJitsu BJJ Martial Art Training Assistant SaaS platform. The choices are based on the project's requirements for AI-driven video analysis, class management, curriculum recommendation, user authentication, and deployment across web and mobile platforms, as detailed in the provided Software Requirements Specification and codebase. The recommendations aim for practicality, maintainability, scalability, and alignment with current best practices, leveraging the existing technological foundations where appropriate.

## 2. Frontend Technologies

### 2.1 Web Application
-   **Framework/Library:** **React with Vite and TypeScript**
    -   **Justification:** React's component-based architecture facilitates modular and reusable UI development. Vite offers a significantly faster development experience (build times, HMR) compared to older bundlers. TypeScript adds static typing, improving code quality and maintainability, which is crucial for a growing SaaS platform. This aligns with the current client setup.
-   **Styling:** **Tailwind CSS with Shadcn UI**
    -   **Justification:** Tailwind CSS provides a utility-first approach for rapid UI development and easy customization. Shadcn UI, built on top of Radix UI and Tailwind, offers a set of accessible, well-designed, and customizable components that can accelerate development while ensuring a modern look and feel. This is consistent with the existing client setup.
-   **State Management:** **Zustand**
    -   **Justification:** Zustand is a lightweight, simple, and flexible state management solution for React. Its minimalistic API and good performance make it suitable for managing application-wide state without excessive boilerplate. This is the current choice and remains appropriate.
-   **Internationalization (i18n):** **i18next**
    -   **Justification:** A robust and widely adopted library for internationalization, allowing the platform to support multiple languages as it targets a global audience (US, Europe, Asia). This is currently in use.
-   **Routing:** **React Router**
    -   **Justification:** The de-facto standard for routing in React applications, providing a declarative way to manage navigation and application views.

### 2.2 Mobile Application
-   **Framework:** **React Native with Expo**
    -   **Justification:** Explicitly chosen for the mobile app. React Native allows for cross-platform development (iOS and Android) from a single codebase, leveraging existing React skills from the web development team (solo developer in this case). Expo simplifies the development, build, and deployment process, which is beneficial given the budget and timeline constraints.
-   **UI Library (Mobile):** **Tamagui**
    -   **Justification:** As mentioned in the Mobile SDS, Tamagui offers TailwindCSS-like styling and cross-platform components, aiming for UI consistency with the web application's Shadcn UI. This is a practical choice for maintaining a cohesive design language.
-   **State Management (Mobile):** **Zustand**
    -   **Justification:** Consistent with the web application, allowing for shared state management patterns and potentially easier context switching for the developer.
-   **Navigation (Mobile):** **React Navigation**
    -   **Justification:** The standard and most feature-rich navigation solution for React Native applications.

## 3. Backend Technologies

### 3.1 API Framework & Language
-   **Framework:** **.NET 8.0 Web API (using C#)**
    -   **Justification:** .NET 8 is a modern, high-performance, and cross-platform framework. C# provides strong typing and a rich ecosystem. The existing microservice-like architecture (FighterManager.Server, VideoSharing.Server, MatchMaker.Server) is a good approach for modularity, scalability, and independent deployment of services.
-   **Real-time Communication:** **ASP.NET Core SignalR**
    -   **Justification:** For features like real-time notifications (e.g., video upload/analysis completion, new shared videos). SignalR integrates seamlessly with the .NET ecosystem and provides efficient real-time bi-directional communication.

### 3.2 AI Services Integration
-   **Video Analysis & Curriculum Generation:** **Google Vertex AI (Gemini 2.5 Pro model)**
    -   **Justification:** Explicitly chosen and suitable for the platform's core AI requirements. Gemini's multimodal capabilities are well-suited for video content understanding (identifying techniques, strengths, weaknesses) and its generative capabilities can be leveraged for curriculum planning based on structured prompts.
-   **Cloud Storage for AI Models/Data (if applicable):** Google Cloud Storage (GCS)
    -   **Justification:** If custom model fine-tuning becomes a future requirement, GCS provides a convenient and integrated storage solution within the GCP ecosystem.

### 3.3 Authentication & Authorization
-   **Mechanism:** **JWT (JSON Web Tokens) with ASP.NET Core Identity**
    -   **Justification:** ASP.NET Core Identity provides a robust framework for user management. JWTs are a standard and stateless way to handle authentication for APIs, suitable for both web and mobile clients. This is the current approach and remains appropriate.
-   **SSO:** OAuth 2.0 integration with Google and Facebook identity providers.

## 4. Database

-   **Type:** **PostgreSQL**
    -   **Justification:** A powerful, open-source, and feature-rich relational database. Its strong support for JSONB data types is particularly beneficial for storing flexible and evolving AI analysis results and curriculum structures. It's also scalable and cost-effective. This is the current database and is well-suited.
-   **ORM:** **Entity Framework Core**
    -   **Justification:** The standard ORM for .NET applications, simplifying data access and providing features like migrations for schema management.

## 5. Infrastructure

### 5.1 Cloud Provider
-   **Primary:** **Google Cloud Platform (GCP)**
    -   **Justification:** The project already leverages GCP for key services like Vertex AI (Gemini) and Google Cloud Storage. Consolidating infrastructure on a single cloud provider often simplifies management, billing, and integration. GKE is also a strong Kubernetes offering.

### 5.2 Containerization & Orchestration
-   **Containerization:** **Docker**
    -   **Justification:** Standard for packaging applications and their dependencies, ensuring consistency across development, testing, and production environments. Already in use.
-   **Local Development Orchestration:** **Docker Compose**
    -   **Justification:** Simplifies the management of multi-container applications locally. Already in use.
-   **Production/Staging Orchestration:** **Kubernetes (specifically Google Kubernetes Engine - GKE)**
    -   **Justification:** Provides robust orchestration for scaling, resilience, automated deployments, service discovery, and load balancing of containerized applications in production. The project's `k8s/` directory and GKE deployment scripts indicate this is the intended path.

### 5.3 CI/CD
-   **Pipeline:** **Google Cloud Build**
    -   **Justification:** Integrates natively with GCP services (GCR, GKE), allowing for automated build, test, and deployment pipelines defined declaratively. The `cloudbuild-*.yaml` files confirm its usage.

### 5.4 Web Server / Reverse Proxy
-   **Frontend Serving & API Gateway (Local):** **Nginx** (within Docker Compose)
    -   **Justification:** Efficiently serves static frontend assets (React build) and can act as a reverse proxy to route API requests to the appropriate backend services during local development.
-   **Production Ingress:** **Kubernetes Ingress Controller (e.g., GCE Ingress or Nginx Ingress Controller on GKE)**
    -   **Justification:** Manages external access to services running in the Kubernetes cluster, providing load balancing, SSL termination, and path-based routing.

### 5.5 Storage
-   **Video File Storage:** **Google Cloud Storage (GCS)**
    -   **Justification:** Scalable, durable, and cost-effective object storage solution, well-suited for storing large video files. Integrated with other GCP services.

### 5.6 Monitoring & Logging
-   **Backend Application Logging:** **Serilog** (current)
    -   **Justification:** Flexible and powerful logging library for .NET applications.
-   **Cloud-Native Observability (Recommended for Production):** **Google Cloud Logging & Google Cloud Monitoring**
    -   **Justification:** For centralized logging, metrics collection, alerting, and dashboarding across all services deployed on GCP. Provides better operational insights in a production environment.

### 5.7 Payment Processing
-   **Gateway:** **Stripe**
    -   **Justification:** A widely adopted, developer-friendly payment processing platform with robust APIs for handling subscriptions and one-time payments, essential for the Freemium model.