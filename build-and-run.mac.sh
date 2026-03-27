#!/bin/bash

echo "Building..."
dotnet build SharedEntities/SharedEntities.csproj
dotnet build FighterManager.Server/FighterManager.Server.csproj
dotnet build VideoAnalysis.Server/VideoAnalysis.Server.csproj
# dotnet build MatchMaker.Server/MatchMaker.Server.csproj

echo "Running backend services in background..."
dotnet run --project FighterManager.Server/FighterManager.Server.csproj --launch-profile https &
dotnet run --project VideoAnalysis.Server/VideoAnalysis.Server.csproj --launch-profile https --watch
# dotnet run --project MatchMaker.Server/MatchMaker.Server.csproj --launch-profile https &

# echo "All services started. Access at:"
# echo "FighterManager: http://localhost:7191/swagger"
# echo "VideoAnalysis: http://localhost:7192/swagger"
# echo "MatchMaker: http://localhost:7193/swagger"
# echo "React App: https://localhost:5173"
