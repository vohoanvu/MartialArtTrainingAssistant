#!/bin/bash

# Script to build Docker images locally and push them to Azure Container Registry (ACR)
# for the MartialArtTrainingAssistant project.

# Configuration variables
ACR_NAME="codejitsuregistry"
ACR_LOGIN_SERVER="$ACR_NAME.azurecr.io"
TAG="2ec73e4ffe87bf6f7577d700709fa6d0f12b83b6"
REPO_DIR="." # Replace with your repository path
SERVICES=(
  "fightermanager:FighterManager.Server"
  "videosharing:VideoSharing.Server"
  "matchmaker:MatchMaker.Server"
  "client:SampleAspNetReactDockerApp.Client"
)
AZURE_RESOURCE_GROUP="vu-test-resource-group" # Replace with your resource group
AZURE_APP_SERVICE="mycodejitsu"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to log messages
log() {
  echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

# Function to handle errors
handle_error() {
  log "${RED}Error: $1${NC}"
  exit 1
}

# Step 1: Verify prerequisites
log "${YELLOW}Verifying prerequisites...${NC}"
command -v docker >/dev/null 2>&1 || handle_error "Docker is not installed. Please install Docker."
command -v az >/dev/null 2>&1 || handle_error "Azure CLI is not installed. Please install Azure CLI."
[[ -d "$REPO_DIR" ]] || handle_error "Repository directory $REPO_DIR does not exist."
cd "$REPO_DIR" || handle_error "Failed to change to repository directory $REPO_DIR"

# Step 2: Clean Docker build cache
log "${YELLOW}Cleaning Docker build cache...${NC}"
docker builder prune -f || handle_error "Failed to clean Docker build cache"

# Step 3: Authenticate with Azure Container Registry
log "${YELLOW}Authenticating with Azure Container Registry ($ACR_NAME)...${NC}"
az login || handle_error "Azure CLI login failed. Please ensure you are logged in."
az acr login --name "$ACR_NAME" || handle_error "Failed to log in to ACR $ACR_NAME"

# Step 4: Build and push Docker images
for service in "${SERVICES[@]}"; do
  IMAGE_NAME="${service%%:*}"
  DOCKERFILE_DIR="${service##*:}"
  IMAGE_FULL_NAME="$ACR_LOGIN_SERVER/$IMAGE_NAME:$TAG"
  DOCKERFILE_PATH="$REPO_DIR/$DOCKERFILE_DIR/Dockerfile"

  log "${YELLOW}Building Docker image for $IMAGE_NAME...${NC}"
  if [[ ! -f "$DOCKERFILE_PATH" ]]; then
    handle_error "Dockerfile not found at $DOCKERFILE_PATH"
  fi

  docker build -t "$IMAGE_FULL_NAME" --no-cache -f "$DOCKERFILE_PATH" . || handle_error "Failed to build image $IMAGE_NAME"
  log "${GREEN}Successfully built $IMAGE_FULL_NAME${NC}"

  log "${YELLOW}Pushing Docker image $IMAGE_NAME to ACR...${NC}"
  docker push "$IMAGE_FULL_NAME" || handle_error "Failed to push image $IMAGE_NAME to ACR"
  log "${GREEN}Successfully pushed $IMAGE_FULL_NAME to ACR${NC}"
done

# Step 5: Verify images in ACR
log "${YELLOW}Verifying images in ACR...${NC}"
for service in "${SERVICES[@]}"; do
  IMAGE_NAME="${service%%:*}"
  log "Checking tags for $IMAGE_NAME..."
  az acr repository show-tags --name "$ACR_NAME" --repository "$IMAGE_NAME" --output table || handle_error "Failed to verify tags for $IMAGE_NAME"
done

# Step 6: Restart Azure App Service
log "${YELLOW}Restarting Azure App Service ($AZURE_APP_SERVICE)...${NC}"
az webapp stop --resource-group "$AZURE_RESOURCE_GROUP" --name "$AZURE_APP_SERVICE" || handle_error "Failed to stop App Service $AZURE_APP_SERVICE"
sleep 10 # Wait for stop to complete
az webapp start --resource-group "$AZURE_RESOURCE_GROUP" --name "$AZURE_APP_SERVICE" || handle_error "Failed to start App Service $AZURE_APP_SERVICE"
log "${GREEN}Azure App Service $AZURE_APP_SERVICE restarted successfully${NC}"

log "${GREEN}All steps completed successfully!${NC}"