docker compose --env-file ./.env down -v app-api video-api pair-api app-client
docker compose --env-file ./.env up -d --build app-api video-api pair-api app-client