FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8082
EXPOSE 7082

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MatchMaker.Server/MatchMaker.Server.csproj", "MatchMaker.Server/"]
COPY ["SharedEntities/SharedEntities.csproj", "SharedEntities/"]
RUN dotnet restore "./MatchMaker.Server/MatchMaker.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./MatchMaker.Server/MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MatchMaker.Server/MatchMaker.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaker.Server.dll"]