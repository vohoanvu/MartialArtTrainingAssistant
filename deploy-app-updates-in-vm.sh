#!/bin/bash
set -e # Exit immediately if a command exits with a non-zero status

echo "--- 1. Navigating to App Directory ---"
cd ~/app

echo "--- 2. Authenticating Docker ---"
gcloud auth configure-docker us-central1-docker.pkg.dev --quiet

echo "--- 3. Pulling Latest Images ---"
sudo docker-compose pull

echo "--- 4. Refreshing Environment Variables & Secrets ---"
chmod +x ./refresh-env-secret-manager.sh
./refresh-env-secret-manager.sh

echo "--- 5. Stopping Old Containers (Clean State) ---"
# Using 'down' and 'prune' to avoid the 'ContainerConfig' bug
sudo docker-compose down --remove-orphans
sudo docker container prune -f

echo "--- 6. Starting New Containers ---"
sudo docker-compose up -d

echo "--- 7. Verification ---"
sudo docker ps

echo "--- 8. Cleaning up old images ---"
sudo docker image prune -a -f

echo "--- DEPLOYMENT COMPLETE ---"