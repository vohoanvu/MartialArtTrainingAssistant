#!/bin/bash

# Configuration
GCR_HOST="us-central1-docker.pkg.dev"
PROJECT_ID="${PROJECT_ID:-your-gcp-project-id}" # Replace with your GCP project ID
CLUSTER_NAME="codejitsu-cluster"
ZONE="us-central1-a"
NAMESPACE="prod"
REPO_DIR="."
TAG=$(git rev-parse --short HEAD) # Use short SHA as tag
SERVICES=(
  "fighter-manager:FighterManager.Server"
  "video-sharing:VideoSharing.Server"
  "match-maker:MatchMaker.Server"
  "frontend:SampleAspNetReactDockerApp.Client"
)
K8S_DIR="k8s/prod"
GENERATED_DIR="k8s/prod/generated"

# Colors for logging
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Logging function
log() {
  echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

# Error handling function
handle_error() {
  log "${RED}Error: $1${NC}"
  exit 1
}

# Verify prerequisites
log "${YELLOW}Verifying prerequisites...${NC}"
command -v gcloud >/dev/null 2>&1 || handle_error "Google Cloud SDK is not installed. Please install it."
command -v docker >/dev/null 2>&1 || handle_error "Docker is not installed. Please install Docker."
command -v kubectl >/dev/null 2>&1 || handle_error "kubectl is not installed. Please install it."
[[ -d "$REPO_DIR" ]] || handle_error "Repository directory $REPO_DIR does not exist."

# Change to repository directory
cd "$REPO_DIR" || handle_error "Failed to change to repository directory $REPO_DIR"

# Authenticate with Google Cloud
log "${YELLOW}Authenticating with Google Cloud...${NC}"
gcloud auth login || handle_error "Google Cloud login failed. Please ensure you have valid credentials."
gcloud config set project "$PROJECT_ID" || handle_error "Failed to set Google Cloud project $PROJECT_ID"
gcloud auth configure-docker "$GCR_HOST" || handle_error "Failed to configure Docker for GCR"

# Clean Docker build cache
log "${YELLOW}Cleaning Docker build cache...${NC}"
docker builder prune -f || handle_error "Failed to clean Docker build cache"

# Build and push Docker images
for service in "${SERVICES[@]}"; do
  IMAGE_NAME="${service%%:*}"
  DOCKERFILE_DIR="${service##*:}"
  IMAGE_FULL_NAME="$GCR_HOST/$PROJECT_ID/codejitsu-repo/$IMAGE_NAME:$TAG"
  DOCKERFILE_PATH="$REPO_DIR/$DOCKERFILE_DIR/Dockerfile"

  log "${YELLOW}Building Docker image for $IMAGE_NAME...${NC}"
  if [[ ! -f "$DOCKERFILE_PATH" ]]; then
    handle_error "Dockerfile not found at $DOCKERFILE_PATH"
  fi
  docker build -t "$IMAGE_FULL_NAME" --no-cache -f "$DOCKERFILE_PATH" . || handle_error "Failed to build image $IMAGE_NAME"
  log "${GREEN}Successfully built $IMAGE_FULL_NAME${NC}"

  log "${YELLOW}Pushing Docker image $IMAGE_NAME to GCR...${NC}"
  docker push "$IMAGE_FULL_NAME" || handle_error "Failed to push image $IMAGE_NAME to GCR"
  log "${GREEN}Successfully pushed $IMAGE_FULL_NAME to GCR${NC}"
done

# Generate Kubernetes manifests
log "${YELLOW}Generating Kubernetes manifests...${NC}"
mkdir -p "$GENERATED_DIR"
for file in "$K8S_DIR"/*.yaml; do
  filename=$(basename "$file")
  output_file="$GENERATED_DIR/$filename"
  log "Processing $file -> $output_file"
  sed "s|\${PROJECT_ID}|$PROJECT_ID|g" "$file" |
    sed "s|\${SHORT_SHA}|$TAG|g" >"$output_file" || handle_error "Failed to generate $output_file"
done
log "${GREEN}Kubernetes manifests generated at $GENERATED_DIR${NC}"

# Configure kubectl to use GKE cluster
log "${YELLOW}Configuring kubectl for GKE cluster $CLUSTER_NAME...${NC}"
gcloud container clusters get-credentials "$CLUSTER_NAME" --zone "$ZONE" --project "$PROJECT_ID" || handle_error "Failed to configure kubectl for cluster $CLUSTER_NAME"

# Create namespace if it doesn't exist
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f - || handle_error "Failed to create namespace $NAMESPACE"

# Apply Kubernetes manifests
log "${YELLOW}Applying Kubernetes manifests...${NC}"
kubectl apply -f "$GENERATED_DIR/secrets.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply secrets.yaml"
kubectl apply -f "$GENERATED_DIR/db-migration-job.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply db-migration-job.yaml"
kubectl apply -f "$GENERATED_DIR/fighter-manager-deployment.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply fighter-manager-deployment.yaml"
kubectl apply -f "$GENERATED_DIR/video-sharing-deployment.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply video-sharing-deployment.yaml"
kubectl apply -f "$GENERATED_DIR/match-maker-deployment.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply match-maker-deployment.yaml"
kubectl apply -f "$GENERATED_DIR/frontend-deployment.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply frontend-deployment.yaml"
kubectl apply -f "$GENERATED_DIR/ingress.yaml" --namespace="$NAMESPACE" || handle_error "Failed to apply ingress.yaml"
log "${GREEN}All Kubernetes manifests applied successfully${NC}"

# Verify deployment
log "${YELLOW}Verifying deployment...${NC}"
kubectl get pods --namespace="$NAMESPACE" -o wide
kubectl get services --namespace="$NAMESPACE"
kubectl get ingress --namespace="$NAMESPACE"

# Wait for pods to be ready
log "${YELLOW}Waiting for pods to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=fighter-manager --namespace="$NAMESPACE" --timeout=300s || handle_error "Fighter-manager pods not ready"
kubectl wait --for=condition=ready pod -l app=video-sharing --namespace="$NAMESPACE" --timeout=300s || handle_error "Video-sharing pods not ready"
kubectl wait --for=condition=ready pod -l app=match-maker --namespace="$NAMESPACE" --timeout=300s || handle_error "Match-maker pods not ready"
kubectl wait --for=condition=ready pod -l app=frontend --namespace="$NAMESPACE" --timeout=300s || handle_error "Frontend pods not ready"
log "${GREEN}All pods are ready${NC}"

# Get Ingress URL
log "${YELLOW}Retrieving Ingress URL...${NC}"
INGRESS_IP=$(kubectl get ingress codejitsu-ingress --namespace="$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
if [ -z "$INGRESS_IP" ]; then
  log "${YELLOW}Ingress IP not yet assigned. Check again later with: kubectl get ingress --namespace=$NAMESPACE${NC}"
else
  log "${GREEN}Application is accessible at http://$INGRESS_IP${NC}"
fi

log "${GREEN}Deployment completed successfully!${NC}"