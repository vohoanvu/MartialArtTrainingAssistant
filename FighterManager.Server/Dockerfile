# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 7080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY FighterManager.Server/FighterManager.Server.csproj FighterManager.Server/
COPY SharedEntities/SharedEntities.csproj SharedEntities/
RUN dotnet restore FighterManager.Server/FighterManager.Server.csproj

# Copy all source files
COPY . .
WORKDIR /src/FighterManager.Server
RUN dotnet build FighterManager.Server.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish FighterManager.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FighterManager.Server.dll"]