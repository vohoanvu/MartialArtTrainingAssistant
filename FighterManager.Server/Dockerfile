FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 7080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

COPY ["FighterManager.Server/FighterManager.Server.csproj", "FighterManager.Server/"]
COPY ["CodeJitsu.Client/CodeJitsu.Client.esproj", "CodeJitsu.Client/"]
COPY ["SharedEntities/SharedEntities.csproj", "SharedEntities/"]
RUN dotnet restore "./FighterManager.Server/FighterManager.Server.csproj"
COPY . .
WORKDIR "/src/FighterManager.Server"
RUN dotnet build "./FighterManager.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FighterManager.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FighterManager.Server.dll"]