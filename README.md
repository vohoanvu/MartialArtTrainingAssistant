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
This web application provides a dynamic platform for users to share and discover their favorite YouTube videos. It is designed with a focus on user engagement and real-time interactions. Below are the key features:
- **User Registration & Authentication**: Users can sign up and log in using their email and password, ensuring secure access to their accounts.
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

### Running the Application

You can run the application without Docker or as a collection of Docker containers. Generally, the docker-compose file was designed to run the application in a production environment, but it can also be used for development purposes.

Below are the steps to run the application in both environments:

#### Running the Application without Docker

To run the application without Docker, you can run the .Net application and the React application separately. To run the .Net application, open the project in Visual Studio 2022 and run the application. The React client application should automatically start by default, but if it does not, you can navigate to the `SampleAspNetReactDockerApp.Client` directory and run the following command:

```bash
npm install
npm run dev
```

**Create a `.env` file in the project root directory with the content of the `.env.example` file.**

**NOTE:** You will need to have Node.js installed to run the client application. The application requires connection to a PostgreSQL database, so you will need to have a PostgreSQL database running. You can configure the connection string in the `appsettings.json` file in the `SampleAspNetReactDockerApp.Server` directory, or you can set the `ASPNETCORE_CONNECTIONSTRING` environment variable to the connection string. You can also run the database in a Docker container (from docker-compose) by running the following command:
**NOTE:** For this youtube video sharing app, you will have to configure your `YOUTUBE_API_KEY` value in `.env` (for Docker host) or `appsettings.json` (for localhost)

```bash
docker compose --env-file ./.env up -d app-db
```

#### Running the Application with Docker

To run the application with Docker, you can run the following command in the root directory of the project:

** Update the `.env` file from the project's root directory with your own Youtube API KEY from the [Google Cloud Console Dashboard](https://cloud.google.com/docs/authentication/api-keys), or you can keep it the same as the content of the `.env.example` file.**

```bash
docker compose --env-file ./.env up -d
```

The above command will start the .Net application, the React-Vite application and the database in separate Docker containers. You can access the application at `http://localhost:8080`.

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