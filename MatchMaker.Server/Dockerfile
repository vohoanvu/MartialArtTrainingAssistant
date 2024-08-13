FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MatchMaker.Server.csproj", "."]
RUN dotnet restore "./MatchMaker.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaker.Server.dll"]

#COPY ["MatchMaker.Server.csproj", "MatchMaker.Server/"]
#COPY ["SampleAspNetReactDockerApp.Client/SampleAspNetReactDockerApp.Client.esproj", "SampleAspNetReactDockerApp.Client/"]
#RUN dotnet restore "MatchMaker.Server/MatchMaker.Server.csproj"
#COPY . .
#WORKDIR "/src/MatchMaker.Server"
#RUN dotnet build "MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "MatchMaker.Server.dll"]
