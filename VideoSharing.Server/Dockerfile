FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8081
EXPOSE 7081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["VideoSharing.Server/VideoSharing.Server.csproj", "VideoSharing.Server/"]
COPY ["SharedEntities/SharedEntities.csproj", "SharedEntities/"]
RUN dotnet restore "./VideoSharing.Server/VideoSharing.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./VideoSharing.Server/VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoSharing.Server/VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoSharing.Server.dll"]