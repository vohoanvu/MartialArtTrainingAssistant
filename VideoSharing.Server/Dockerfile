FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["VideoSharing.Server.csproj", "."]
RUN dotnet restore "./VideoSharing.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoSharing.Server.dll"]

#COPY ["VideoSharing.Server.csproj", "VideoSharing.Server/"]
#COPY ["SampleAspNetReactDockerApp.Client/SampleAspNetReactDockerApp.Client.esproj", "SampleAspNetReactDockerApp.Client/"]
#RUN dotnet restore "VideoSharing.Server/VideoSharing.Server.csproj"
#COPY . .
#WORKDIR "/src/VideoSharing.Server"
#RUN dotnet build "VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "VideoSharing.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "VideoSharing.Server.dll"]
