#!/bin/bash
# Fetches the latest secret values from GCP Secret Manager into a fresh .env file on the VM.
# Project: MyCoach (company). Auth: the VM's attached service account (codejitsu-vm-runtime),
# which has per-secret roles/secretmanager.secretAccessor on the secrets listed below.
#
# KEYLESS: there is no GCS service-account key. The app authenticates to GCS and Vertex AI via
# Application Default Credentials (the VM's attached SA), because the MyCoach org policy
# constraints/iam.disableServiceAccountKeyCreation forbids downloadable SA keys.
set -euo pipefail

PROJECT_ID="project-afa815fe-26c6-40c3-a8b"
ENV_FILE=".env"

SECRETS_TO_FETCH=(
  "SUPABASE_APP_DB"
  "ASPNETCORE_APP_PORT_1"
  "ASPNETCORE_APP_PORT_2"
  "CLIENT_APP_PORTS"
  "ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION"
  "JWT_AUDIENCE"
  "JWT_ISSUER"
  "JWT_KEY"
  "GOOGLE_CLIENT_ID"
  "GOOGLE_CLIENT_SECRET"
  "YOUTUBE_API_KEY"
  "GOOGLE_CLOUD_PROJECT_ID"
  "GOOGLE_CLOUD_BUCKET_NAME"
  "GEMINI_VISION_VIDEO_ANALYSIS_PROMPT"
  "GEMINI_VISION_LOCATION"
  "GEMINI_VISION_MODEL"
)

echo "Creating a fresh .env file..."
> "$ENV_FILE"

for secret_name in "${SECRETS_TO_FETCH[@]}"; do
  echo "Fetching secret: $secret_name..."
  secret_value=$(gcloud secrets versions access latest --secret="$secret_name" --project="$PROJECT_ID" --quiet)
  # Single-quote the value so connection strings / prompts with spaces survive docker-compose parsing.
  printf "%s='%s'\n" "$secret_name" "$secret_value" >> "$ENV_FILE"
done

# Optional (non-fatal if the secret does not exist yet): AI Console admin allow-list.
OPTIONAL_SECRETS=(
  "AI_CONSOLE_ADMIN_EMAILS"
)
for secret_name in "${OPTIONAL_SECRETS[@]}"; do
  if secret_value=$(gcloud secrets versions access latest --secret="$secret_name" --project="$PROJECT_ID" --quiet 2>/dev/null); then
    echo "Fetching optional secret: $secret_name..."
    printf "%s='%s'\n" "$secret_name" "$secret_value" >> "$ENV_FILE"
  else
    echo "Optional secret $secret_name not found; skipping."
  fi
done

echo ""
echo ".env file has been refreshed with the latest secrets from project $PROJECT_ID."
echo "Keyless auth: no GCS key file is fetched; the app uses the VM's attached service account (ADC)."
