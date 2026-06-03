#!/bin/bash
# Decommission the OLD personal GCP project `codejitsu` after the MyCoach migration.
#
# RUN THIS ONLY AFTER a stability window (recommended: a few days to ~1 week) during which
# thecodejitsu.com runs healthily on MyCoach. Everything below is IRREVERSIBLE and removes the
# rollback path (the stopped old VM + old static IP + old images).
#
# Safety: requires an explicit confirmation token to run.
#   CONFIRM=DECOMMISSION bash decommission-codejitsu.sh
#
# Prereqs: gcloud authenticated as an owner of `codejitsu` (vohoanvu96@gmail.com).

set -uo pipefail

SRC="codejitsu"
DST="project-afa815fe-26c6-40c3-a8b"
ZONE="us-central1-c"
REGION="us-central1"
STAGE_BUCKET="martial-art-demo-vids-migrate"

if [[ "${CONFIRM:-}" != "DECOMMISSION" ]]; then
  echo "Refusing to run without confirmation."
  echo "This will PERMANENTLY delete the old VM, static IP, Artifact Registry repo, and secrets in '$SRC',"
  echo "and the MyCoach staging bucket. It removes the rollback path."
  echo ""
  echo "Re-run with:  CONFIRM=DECOMMISSION bash $0"
  exit 1
fi

echo "############################################################"
echo "# Decommissioning '$SRC' — $(printf 'IRREVERSIBLE')"
echo "############################################################"

echo ""
echo "===== 1. Delete old VM (codejitsu) ====="
gcloud compute instances delete thecodejitsu-app-vm --zone="$ZONE" --project="$SRC" --quiet \
  || echo "  (skip: VM already gone)"

echo ""
echo "===== 2. Release old static external IP ====="
gcloud compute addresses delete codejitsu-static-ip --region="$REGION" --project="$SRC" --quiet \
  || echo "  (skip: address already released)"

echo ""
echo "===== 3. Delete old Artifact Registry repo (all images) ====="
gcloud artifacts repositories delete codejitsu-repo --location="$REGION" --project="$SRC" --quiet \
  || echo "  (skip: repo already gone)"

echo ""
echo "===== 4. Delete old firewall rules (codejitsu) ====="
for fw in allow-http-80 allow-https-443 allow-http-https-all allow-outbound-supabase; do
  gcloud compute firewall-rules delete "$fw" --project="$SRC" --quiet 2>/dev/null \
    && echo "  deleted $fw" || echo "  (skip $fw)"
done

echo ""
echo "===== 5. Destroy old Secret Manager secrets (codejitsu) ====="
# These are obsolete now that MyCoach holds its own copies. Comment out any you want to keep.
SECRETS=(
  ASPNETCORE_APP_PORT ASPNETCORE_APP_PORT_1 ASPNETCORE_APP_PORT_2 ASPNETCORE_APP_PORT_3
  ASPNETCORE_ENVIRONMENT ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION CLIENT_APP_PORTS
  GCS_SERVICE_ACCOUNT_KEY GEMINI_VISION_LOCATION GEMINI_VISION_MODEL GEMINI_VISION_VIDEO_ANALYSIS_PROMPT
  GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET GOOGLE_CLOUD_BUCKET_NAME GOOGLE_CLOUD_PROJECT_ID
  JWT_AUDIENCE JWT_ISSUER JWT_KEY SUPABASE_APP_DB XAIGROK_API_KEY XAIGROK_ENDPOINT YOUTUBE_API_KEY
)
for s in "${SECRETS[@]}"; do
  gcloud secrets delete "$s" --project="$SRC" --quiet 2>/dev/null \
    && echo "  deleted $s" || echo "  (skip $s)"
done

echo ""
echo "===== 6. Delete MyCoach staging bucket (migration backup) ====="
gcloud storage rm --recursive "gs://$STAGE_BUCKET/**" --project="$DST" 2>/dev/null || true
gcloud storage buckets delete "gs://$STAGE_BUCKET" --project="$DST" --quiet \
  || echo "  (skip: staging bucket already gone)"

echo ""
echo "############################################################"
echo "# MANUAL steps (Console UI — cannot be done via gcloud):"
echo "#  - Delete the OLD OAuth 2.0 client in codejitsu (APIs & Services > Credentials)"
echo "#  - Delete the OLD YouTube API key in codejitsu (APIs & Services > Credentials)"
echo "#  - Revoke the old GitHub PAT: https://github.com/settings/tokens"
echo "#  - Optionally shut down the whole '$SRC' project:"
echo "#      gcloud projects delete $SRC"
echo "############################################################"
echo "DECOMMISSION COMPLETE"
