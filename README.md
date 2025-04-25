# Youtube Video Sharing Web App

## Table of Contents

- [About the Project](#about-the-project)
  - [App overview](#app-overview)
  - [Credit Acknowledgement](#credit-acknowledgement)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running the Application](#running-the-application)
    - [Running the Application without Docker](#running-the-application-without-docker)
    - [Running the Application with Docker](#running-the-application-with-docker)
  - [Running Unit Test suite](#running-unit-test-suite)
- [Tech stack](#tech-stack)

## About the Project

### App Overview
This web application facilitates better training curriculum for Martial Artist. It is designed with a focus on user engagement and real-time interactions. Below are the key features:
- **User Registration & Authentication**: Users (Instructor or Student) can sign up and log in using their email and password, ensuring secure access to their accounts.
- **Fighter Registration**: Student and Instructor can create their 'Fighter' profile by signing up.
- **Training Session**: Instructor can create new training session for students to join.
- **Video Sharing & Discovery**: The homepage showcases a curated list of YouTube videos shared by the community. Users can explore new content and get inspired by others' favorites.
- **Real-Time Notifications**: When a user shares a new YouTube video, all registered users receive immediate notifications, fostering a lively and connected user experience.

This project is a sample .Net 8.0 web API with React and Docker support project for demonstration purposes and as a starting point for a fullstack application. The project is designed to run as a collection of Docker containers, with the .Net application running in a container, the React application running in a container (via Nginx), and a PostgreSQL database running in a container. The project also includes an Nginx to route traffic between the React and .Net applications on the same port, but different paths.

It was made to demonstrate how to create a fullstack application with .Net and React, and how to run the application in a Docker environment.

### Credit Acknowledgement:
The original starting template was forked from this public repository **[SampleAspNetReactDockerApp](https://github.com/SirCypkowskyy/SampleAspNetReactDockerApp)** by **Cyprian Gburek**

## Getting Started

To get started, clone the repository and open the project in Visual Studio 2022. The project is configured to run in a Docker container, so you will need to have Docker Desktop installed. Once you have the project open, you can run the project in Visual Studio 2022 and the application will start in a Docker container.

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org/en/)
- [React](https://reactjs.org/)
- [.Net 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### .env Configuration

Open the `.env` file in the project root and configure the following variables. These are essential for the Docker Compose setup and define ports and connection details:

-   `ASPNETCORE_APP_DB`: The connection string used by the API containers to connect to the PostgreSQL container. The default value usually works if the database service name in `docker-compose.yml` is `app-db` and the port is 5430.
-   `CLIENT_APP_PORTS`: Defines the host-to-container port mapping for the frontend Nginx container (e.g., `3000:80` maps host port 3000 to container port 80).
-   `POSTGRES_PORT`: Sets the host port mapping for the PostgreSQL database container (e.g., `5430:5430`).
-   `ASPNETCORE_APP_PORT_1`: Host port for the `FighterManager.Server` API container (e.g., `8081`). This will map to the container's internal port (7080/8080).
-   `ASPNETCORE_APP_PORT_2`: Host port for the `VideoSharing.Server` API container (e.g., `8082`). This will map to the container's internal port (7081/8081).
-   `ASPNETCORE_APP_PORT_3`: Host port for the `MatchMaker.Server` API container (e.g., `8083`). This will map to the container's internal port (7082/8082).
-   `YOUTUBE_API_KEY`: **Required** if using YouTube video sharing features. Obtain from [Google Cloud Console](https://cloud.google.com/docs/authentication/api-keys).
-   `GOOGLE_CLOUD_PROJECT_ID`: Your Google Cloud Project ID (if using GCS features).
-   `GOOGLE_CLOUD_BUCKET_NAME`: Your Google Cloud Storage bucket name (if using GCS features).
-   `ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION`: Set to `true` if you want Swagger UI accessible when running via Docker Compose.

**Note:** `GoogleCloud__ServiceAccountKeyPath` is configured differently, see the dedicated section below.

### Running the Application

Choose one of the following approaches:

#### Approach 1: Running with Docker Compose (Recommended)

This approach runs the entire application stack (APIs, React Client via Nginx, Database) within Docker containers.

1.  **Ensure Docker Desktop is Running.**
2.  **Build and Run Containers:** Open a terminal in the project root directory and run:
    ```bash
    docker compose --env-file ./.env up -d --build
    ```
    * `--env-file ./.env`: Specifies the environment variables file.
    * `--build`: Rebuilds images if Dockerfiles have changed.
    * `-d`: Runs containers in detached mode.
3.  **Database Initialization:** The first time, migrations and seeding should run automatically. Monitor logs if needed: `docker compose logs -f app-api video-api pair-api`.
4.  **Access the Application:**
    * **Frontend:** `http://localhost:<HOST_PORT>` (where `<HOST_PORT>` is the host port specified in `CLIENT_APP_PORTS`, e.g., `http://localhost:3000`).
    * **Swagger UIs** (if `ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION=true`):
        * FighterManager API: `http://localhost:<HOST_PORT>/swagger`
        * VideoSharing API: `http://localhost:<HOST_PORT>/vid/swagger`
        * MatchMaker API: `http://localhost:<HOST_PORT>/pair/swagger`
5.  **Stopping:**
    ```bash
    docker compose down
    ```

#### Approach 2: Hybrid (Dockerized DB, Local Apps)

Run the database in Docker, but the .NET APIs and React client directly on your machine. Useful for direct debugging.

1.  **Ensure Docker Desktop is Running.**
2.  **Start Database Container:** In the project root terminal:
    ```bash
    docker compose --env-file ./.env up -d app-db
    ```
3.  **Verify API Connection Strings:** Ensure `AppDb` connection string in `FighterManager.Server/appsettings.json`, `VideoSharing.Server/appsettings.json`, `MatchMaker.Server/appsettings.json`, and `SharedEntities/appsettings.json` points to `localhost:<POSTGRES_PORT>` (e.g., `localhost:5430`).
4.  **Run Backend APIs (.NET):**
  - Since SharedEntities is a class library, migrations require a startup project (e.g., FighterManager.Server) that configures the DbContext and provides the runtime environment.
    * **Create/Update Db Schema** Run the following command from the root directory to create a new migration:
        ```bash
        dotnet ef migrations add NewMigrationName --project SharedEntities --startup-project FighterManager.Server
        ```
    * **Apply Db Migrations:** Run the following command from the root directory to apply migrations:
        ```bash
        dotnet ef database update --project SharedEntities --startup-project FighterManager.Server
        ```
    * Open **separate terminals** for each API and run them:
        ```bash
        # Terminal 1: FighterManager API
        cd FighterManager.Server
        dotnet run --launch-profile http # Runs on http://localhost:5136

        # Terminal 2: VideoSharing API
        cd VideoSharing.Server
        dotnet run --launch-profile http # Runs on http://localhost:5137

        # Terminal 3: MatchMaker API
        cd MatchMaker.Server
        dotnet run --launch-profile http # Runs on http://localhost:5138
        ```
5.  **Run Frontend (React):**
    * Open another terminal:
        ```bash
        cd SampleAspNetReactDockerApp.Client
        npm install
        npm run dev # Runs on https://localhost:5173 by default
        ```
6.  **Access the Application:**
    * **Frontend:** `https://localhost:5173` (or the URL provided by `npm run dev`).
    * **Swagger UIs:**
        * FighterManager API: `http://localhost:5136/swagger`
        * VideoSharing API: `http://localhost:5137/swagger`
        * MatchMaker API: `http://localhost:5138/swagger`
7.  **Stopping:**
    * Stop each `dotnet run` and `npm run dev` process (`Ctrl+C`).
    * Stop the database container: `docker compose down`.

### Google Cloud Service Account Key Configuration

If using Google Cloud Storage features in the `VideoSharing.Server`, you need to configure the path to your service account key JSON file.

1.  **Obtain Key File:**
    * You cannot re-download an existing key file's private key.
    * If you don't have the file, create a *new* key for your service account in the Google Cloud Console (IAM & Admin -> Service Accounts -> Select Account -> Keys -> Add Key -> Create new key -> JSON). See [Google Cloud Docs](https://cloud.google.com/iam/docs/keys-create-delete).
    * **Important:** Store this downloaded `.json` file securely and **do not commit it to Git**. Add its location to your `.gitignore` file.

2.  **Configuration for Local Setup (Approach 2):**
    * **Place the key file** securely outside your project directory (e.g., `~/secrets/gcp-key.json`).
    * **Configure the path** in `VideoSharing.Server/appsettings.Development.json` (create this file if it doesn't exist; it overrides `appsettings.json` locally):
        ```json
        {
          // ... other settings ...
          "GoogleCloud": {
            "ServiceAccountKeyPath": "/Users/<your-username>/secrets/gcp-key.json" // <-- Use absolute path
          }
        }
        ```
        (Replace `<your-username>` with your macOS username)
    * Alternatively, set an environment variable in your terminal *before* running `dotnet run` for `VideoSharing.Server`:
        ```bash
        export GOOGLECLOUD__SERVICEACCOUNTKEYPATH="/Users/<your-username>/secrets/gcp-key.json"
        dotnet run --launch-profile http
        ```

3.  **Configuration for Docker Setup (Approach 1):**
    * Place the key file in a `secrets` directory in your project root: `./secrets/gcp-key.json`.
    * Ensure the `video-api` service in `docker-compose.yml` correctly mounts this file and sets the environment variable to the *container path*
        ```yaml
        services:
          video-api:
            # ... other config ...
            environment:
              # ... other env vars ...
              GoogleCloud__ServiceAccountKeyPath: "/app/secrets/gcp-key.json" # <-- Path INSIDE container
            volumes:
              - ./secrets/gcp-key.json:/app/secrets/gcp-key.json:ro # <-- Mounts host path to container path
            # ... potentially a secrets block pointing to the same file ...
        ```

### Running Unit Test suite
#### backend unit test
- From the repository root:
```bash
dotnet test
```
Or you run test suite via Visual Studio GUI

#### frontend unit test
- From the repository root:
```bash
cd SampleAspNetReactDockerApp.Client
```
```bash
npm test
```

## Tech stack
- Backend
  - [.Net 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) - for the server application
  - [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - for the database ORM
  - [PostgreSQL](https://www.postgresql.org/) - for the database
  - [Npgsql](https://www.npgsql.org/) - for the PostgreSQL database provider
  - [Swagger](https://swagger.io/) - for API documentation
  - [Microsoft Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity) - for user authentication and authorization
  - [xUnit](https://xunit.net/) - for backend unit testing
  - [SignalR](https://learn.microsoft.com/en-us/aspnet/signalr/overview/getting-started/introduction-to-signalr) - for real-time notifications system
- Frontend
  - [React](https://reactjs.org/) - for the client application
  - [Vite](https://vitejs.dev/) - for the client application
  - [Tailwind CSS](https://tailwindcss.com/) - for styling
  - [React Router](https://reactrouter.com/) - for routing
- [Docker](https://www.docker.com/) - for containerization
- [Nginx](https://www.nginx.com/) - for routing between the React and .Net applications
- [PostgreSQL](https://www.postgresql.org/) - for the database used by the .Net application
- [Jest](https://jestjs.io/docs/getting-started) - for frontend unit testing
