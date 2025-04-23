#!/bin/bash

echo "Building..."
dotnet build SharedEntities/SharedEntities.csproj
dotnet build FighterManager.Server/FighterManager.Server.csproj
dotnet build VideoSharing.Server/VideoSharing.Server.csproj
dotnet build MatchMaker.Server/MatchMaker.Server.csproj

# echo "Running backend services in background..."
# dotnet run --project FighterManager.Server/FighterManager.Server.csproj --launch-profile https &
# dotnet run --project VideoSharing.Server/VideoSharing.Server.csproj --launch-profile https &
# dotnet run --project MatchMaker.Server/MatchMaker.Server.csproj --launch-profile https &

# echo "All services started. Access at:"
# echo "FighterManager: http://localhost:7191/swagger"
# echo "VideoSharing: http://localhost:7192/swagger"
# echo "MatchMaker: http://localhost:7193/swagger"
# echo "React App: https://localhost:5173"