﻿FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 7080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["FighterManager.Server/FighterManager.Server.csproj", "FighterManager.Server/"]
COPY ["SharedEntities/SharedEntities.csproj", "SharedEntities/"]
RUN dotnet restore "./FighterManager.Server/FighterManager.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./FighterManager.Server/FighterManager.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FighterManager.Server/FighterManager.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FighterManager.Server.dll"]