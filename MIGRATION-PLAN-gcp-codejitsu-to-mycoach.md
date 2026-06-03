# Infrastructure Migration Plan — `codejitsu` → `MyCoach`

> **Objective:** Migrate the entire VM-hosted CodeJitsu stack from the personal GCP project **`codejitsu`** into the company GCP project **`project-afa815fe-26c6-40c3-a8b`** (display name **MyCoach**), so the workload bills to the company account. The application keeps the same domain (`thecodejitsu.com`), the same external Supabase database, and the same GCS bucket name.
>
> **Audience:** DevOps agents executing the migration end-to-end. Steps are ordered; do not skip the pre-flight gates.
>
> **Author/Owner:** Vu Vo (`vohoanvu96@gmail.com`)
> **Drafted:** 2026-06-03

---

## 0. Locked decisions (from migration kickoff)

| Decision | Choice | Consequence for this plan |
|---|---|---|
| **Domain** | Keep `thecodejitsu.com` | Re-point DNS A record only. **Copy** the existing Let's Encrypt cert directory rather than re-issuing. No changes to OAuth redirect URIs, JWT issuer/audience, CORS, or Nginx `server_name`. |
| **Downtime** | Short maintenance window (15–60 min) acceptable | Enables the clean "reuse the same global bucket name" procedure (delete-old → recreate-in-new). |
| **GCS bucket** | Reuse the name `martial-art-demo-vids` | **No** Supabase DB rewrite needed. The DB stores full `gs://martial-art-demo-vids/...` URIs (see [GoogleCloudStorageService.cs:38](VideoAnalysis.Server/Domain/GoogleCloudStorageService/GoogleCloudStorageService.cs#L38)), so the bucket name must stay identical. |
| **Database** | Keep Supabase as-is | No DB data migration. Only copy the `SUPABASE_APP_DB` connection-string secret into the new project's Secret Manager. Supabase billing is separate from GCP and unaffected. |

---

## 1. Constants (use these throughout)

```bash
# ── Projects ──────────────────────────────────────────────
export SRC_PROJECT="codejitsu"
export DST_PROJECT="project-afa815fe-26c6-40c3-a8b"   # MyCoach
export ACCOUNT="vohoanvu96@gmail.com"

# ── Region / placement (keep identical to source) ────────
export REGION="us-central1"
export ZONE="us-central1-c"

# ── Resource names ───────────────────────────────────────
export AR_REPO="codejitsu-repo"
export VM_NAME="thecodejitsu-app-vm"          # keep same name to minimize doc/script churn
export BUCKET="martial-art-demo-vids"          # MUST stay identical (DB stores gs:// paths)
export STAGE_BUCKET="martial-art-demo-vids-migrate"  # temporary staging bucket in DST
export DOMAIN="thecodejitsu.com"
export STATIC_IP_NAME="thecodejitsu-vm-ip"

# ── Service accounts to create in DST ────────────────────
export VM_SA="codejitsu-vm-runtime"            # attached to the VM: reads Secret Manager + pulls images
export APP_SA="codejitsu-storage-vertex"       # GCS + Vertex AI; its JSON key is mounted into video-analysis
export CICD_SA="github-actions-deployer"       # used by GitHub Actions via Workload Identity Federation
```

> **Execution environment note:** All `gcloud`/`gsutil` commands below are written for **bash**. The owner's workstation is Windows/PowerShell — run these in **Google Cloud Shell** or **Git Bash**, where line continuations (`\`) and heredocs work. In PowerShell, collapse to one line or use backtick continuations.

> **Authentication:** `gcloud auth login` as `vohoanvu96@gmail.com`. This single identity owns `codejitsu` and has **Editor + Project IAM Admin** on MyCoach — enough for everything in this plan **except** billing linkage and org policies (see §2).

---

## 2. Pre-flight gates (STOP if any fail)

These are hard prerequisites. Do not start Phase 3 onward until all are green.

### 2.1 Billing is linked to the destination project
Creating Compute Engine, Artifact Registry, GCS, and Vertex resources **requires an active billing account** on MyCoach. The owner has Editor + Project IAM Admin but likely **not** Billing Account Admin, so a company billing admin may need to link it.

```bash
gcloud billing projects describe "$DST_PROJECT"
# Expect: billingEnabled: true  and a billingAccountName.
# If billingEnabled is false → escalate to the company GCP billing admin BEFORE proceeding.
```

### 2.2 Confirm effective permissions on the destination project
```bash
gcloud projects get-iam-policy "$DST_PROJECT" \
  --flatten="bindings[].members" \
  --filter="bindings.members:user:$ACCOUNT" \
  --format="table(bindings.role)"
# Expect at least: roles/editor and roles/resourcemanager.projectIamAdmin
```

### 2.3 Access caveats to anticipate (Editor + Project IAM Admin, NOT Owner)
| Operation | Likely allowed? | Mitigation if blocked |
|---|---|---|
| Enable APIs, create AR/SA/GCS/VM/IP/firewall | ✅ Editor | — |
| Set IAM bindings on the project | ✅ Project IAM Admin | — |
| Create service-account keys | ✅ Editor | If org policy `iam.disableServiceAccountKeyCreation` is on → use Workload Identity / attached SA instead of a key (see §11 alternative) |
| Link billing account | ❌ usually | Company billing admin links it (§2.1) |
| Configure OAuth consent screen / brand | ⚠️ sometimes restricted | Escalate to a project Owner if brand creation is denied |
| Set org policies | ❌ | Escalate to org admin |

### 2.4 Lower DNS TTL now (preparation, zero-downtime)
At the current DNS provider for `thecodejitsu.com`, set the TTL on the `@` and `www` A records to **300 seconds** at least 24–48h before the cutover window. This shortens propagation when the IP flips in Phase 6.

### 2.5 Capture the full source inventory
Run the discovery block in [Appendix A](#appendix-a--source-inventory-discovery) and save the output. You will need exact values (OAuth redirect URIs, firewall rule definitions, secret names, VM machine type, disk size, renewal mechanism).

---

## 3. Resource inventory & migration matrix

| # | Source resource (`codejitsu`) | Type | Migration action | Phase |
|---|---|---|---|---|
| 1 | Enabled APIs | Service Usage | Re-enable the same set in MyCoach | 4 |
| 2 | `codejitsu-repo` Docker registry | Artifact Registry | Recreate repo; rebuild or copy 3 images | 5 |
| 3 | `fighter-manager`, `video-analysis`, `app-client` images | Container images | Re-push via CI **or** `gcloud artifacts docker images copy` | 5 |
| 4 | ~17 secrets | Secret Manager | Recreate in MyCoach; rotate the 4 that change | 4 |
| 5 | OAuth 2.0 Web client (`GOOGLE_CLIENT_ID/SECRET`) | API Credentials | Recreate consent screen + client in MyCoach; mirror redirect URIs | 4 |
| 6 | YouTube Data API v3 key (`YOUTUBE_API_KEY`) | API key | Create new restricted key in MyCoach | 4 |
| 7 | GCS service-account key (`GCS_SERVICE_ACCOUNT_KEY`) | SA + JSON key | New `APP_SA` + new key | 4 |
| 8 | `martial-art-demo-vids` bucket + objects | Cloud Storage | Stage → delete old → recreate same name in MyCoach → restore | 7 |
| 9 | `thecodejitsu-app-vm` | Compute Engine VM | Provision new VM in MyCoach | 6 |
| 10 | VM external IP | Static IP | Reserve new static IP in MyCoach | 6 |
| 11 | `allow-http-80`, `allow-https-443` | Firewall rules | Recreate in MyCoach `default` network | 6 |
| 12 | Let's Encrypt cert for `thecodejitsu.com` | TLS cert (in `~/app/letsencrypt`) | **Copy** cert dir to new VM; verify renewal | 6, 8 |
| 13 | Vertex AI (Gemini) usage | Vertex AI API | Enable API + grant `APP_SA` `aiplatform.user`; update `GOOGLE_CLOUD_PROJECT_ID` | 4 |
| 14 | GitHub Actions WIF (3 repo secrets) | Workload Identity Federation | New pool/provider/SA in MyCoach; update repo secrets | 9 |
| 15 | Supabase Postgres | External (Supabase) | **No migration** — copy connection secret only | 4 |
| 16 | XAI Grok (`XAIGROK_*`) | External (x.ai) | **No migration** — copy secret values only | 4 |
| — | `k8s/`, `cloudbuild-test.yaml`, `build-and-push-to-acr.sh` | Legacy GKE/Azure experiments | **Not in production** — ignore; do not migrate | — |

---

## 4. Phase 1 — Foundation in MyCoach (no downtime, do this first)

All of Phase 1–6 can be done **while the old site stays fully live**. Only Phase 7 (bucket) needs the maintenance window.

### 4.1 Set the working project & enable APIs
```bash
gcloud config set project "$DST_PROJECT"
gcloud config set account "$ACCOUNT"

gcloud services enable \
  compute.googleapis.com \
  artifactregistry.googleapis.com \
  secretmanager.googleapis.com \
  storage.googleapis.com \
  iam.googleapis.com \
  iamcredentials.googleapis.com \
  sts.googleapis.com \
  cloudresourcemanager.googleapis.com \
  aiplatform.googleapis.com \
  youtube.googleapis.com \
  serviceusage.googleapis.com \
  --project="$DST_PROJECT"
```

### 4.2 Create the runtime & app service accounts
```bash
# VM runtime SA: reads Secret Manager + pulls from Artifact Registry
gcloud iam service-accounts create "$VM_SA" \
  --display-name="CodeJitsu VM runtime" --project="$DST_PROJECT"

# App SA: GCS object access + Vertex AI (its JSON key is mounted into video-analysis)
gcloud iam service-accounts create "$APP_SA" \
  --display-name="CodeJitsu storage + Vertex" --project="$DST_PROJECT"

export VM_SA_EMAIL="${VM_SA}@${DST_PROJECT}.iam.gserviceaccount.com"
export APP_SA_EMAIL="${APP_SA}@${DST_PROJECT}.iam.gserviceaccount.com"
```

Grant project-level roles:
```bash
# VM runtime SA
gcloud projects add-iam-policy-binding "$DST_PROJECT" \
  --member="serviceAccount:$VM_SA_EMAIL" --role="roles/secretmanager.secretAccessor"
gcloud projects add-iam-policy-binding "$DST_PROJECT" \
  --member="serviceAccount:$VM_SA_EMAIL" --role="roles/artifactregistry.reader"

# App SA: Vertex AI (Gemini). Storage access is granted at the bucket level in Phase 7.
gcloud projects add-iam-policy-binding "$DST_PROJECT" \
  --member="serviceAccount:$APP_SA_EMAIL" --role="roles/aiplatform.user"
```

> The app reads/writes GCS and calls Vertex with the **mounted key** of `APP_SA` (cloud-platform scope; see [GeminiVisionService.cs:72-84](VideoAnalysis.Server/Domain/AIServices/GeminiVisionService.cs#L72-L84) and [GoogleCloudStorageService.cs:21-26](VideoAnalysis.Server/Domain/GoogleCloudStorageService/GoogleCloudStorageService.cs#L21-L26)). The VM's *attached* SA (`VM_SA`) is only used by the host scripts to fetch secrets and pull images.

### 4.3 Create the App SA key → store as `GCS_SERVICE_ACCOUNT_KEY`
```bash
gcloud iam service-accounts keys create /tmp/app-sa-key.json \
  --iam-account="$APP_SA_EMAIL" --project="$DST_PROJECT"
```
This file becomes the `GCS_SERVICE_ACCOUNT_KEY` secret in §4.6 and is later written to `~/app/secrets/gcs-key.json` on the VM. **Delete `/tmp/app-sa-key.json` after uploading it to Secret Manager.**

> If org policy blocks SA key creation (§2.3), switch the app to **Application Default Credentials**: attach `APP_SA` to the VM as its service account and remove the key-file mount — the code already falls back to ADC ([GeminiVisionService.cs:79-84](VideoAnalysis.Server/Domain/AIServices/GeminiVisionService.cs#L79-L84)). `GoogleCloudStorageService` would need a small change to use `GoogleCredential.GetApplicationDefault()` when the key path is absent.

### 4.4 Artifact Registry repository
```bash
gcloud artifacts repositories create "$AR_REPO" \
  --repository-format=docker \
  --location="$REGION" \
  --description="CodeJitsu container images" \
  --project="$DST_PROJECT"
```

### 4.5 OAuth client + YouTube API key + Vertex (Console + CLI)
**Read the source values first**, then recreate in MyCoach.

1. **OAuth consent screen** (MyCoach → APIs & Services → OAuth consent screen): External, app name "CodeJitsu", support email = owner, add scopes `email`, `profile`, `openid`. Publish (or add test users) to match the source app's publishing status.
2. **OAuth 2.0 Web client** (Credentials → Create credentials → OAuth client ID → Web application):
   - **Authorized JavaScript origins** and **Authorized redirect URIs** must exactly mirror the source client. Read them from the source project first:
     - Source console: `codejitsu` → APIs & Services → Credentials → the existing OAuth Web client.
     - Expected redirect URI (confirm): `https://thecodejitsu.com/signin-google-callback` (matches the Nginx route at [default.conf:131](CodeJitsu.Client/default.conf#L131)). Include `https://www.thecodejitsu.com/...` if present in source.
   - Capture the new **Client ID** and **Client secret** → these replace `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET`.
3. **YouTube Data API v3 key** (Credentials → Create credentials → API key): restrict it to the **YouTube Data API v3** only. Capture value → replaces `YOUTUBE_API_KEY`. (Quota is per-project and resets fresh in MyCoach.)
4. **Vertex AI**: already enabled in §4.1; `APP_SA` already has `aiplatform.user` (§4.2). The model IDs come from the `GEMINI_VISION_*` secrets — copy values unchanged.

### 4.6 Recreate Secret Manager secrets
Read each secret from source, transform the four that change, and write to MyCoach. **You are owner of `codejitsu`, so you can read its secret values.**

```bash
# Secrets to COPY UNCHANGED from source → destination
COPY_UNCHANGED=(
  SUPABASE_APP_DB
  ASPNETCORE_APP_PORT_1
  ASPNETCORE_APP_PORT_2
  CLIENT_APP_PORTS
  ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION
  JWT_AUDIENCE
  JWT_ISSUER
  JWT_KEY
  GOOGLE_CLOUD_BUCKET_NAME
  GEMINI_VISION_VIDEO_ANALYSIS_PROMPT
  GEMINI_VISION_LOCATION
  GEMINI_VISION_MODEL
  XAIGROK_API_KEY
  XAIGROK_ENDPOINT
)

for name in "${COPY_UNCHANGED[@]}"; do
  val=$(gcloud secrets versions access latest --secret="$name" --project="$SRC_PROJECT" --quiet)
  printf '%s' "$val" | gcloud secrets create "$name" --data-file=- --project="$DST_PROJECT" 2>/dev/null \
    || printf '%s' "$val" | gcloud secrets versions add "$name" --data-file=- --project="$DST_PROJECT"
  echo "✓ $name"
done
```

Now create/overwrite the **four that change**:
```bash
# 1. New project ID (was 'codejitsu')
printf '%s' "$DST_PROJECT" | gcloud secrets create GOOGLE_CLOUD_PROJECT_ID --data-file=- --project="$DST_PROJECT"

# 2. New OAuth client (from §4.5)
printf '%s' "<NEW_GOOGLE_CLIENT_ID>"     | gcloud secrets create GOOGLE_CLIENT_ID     --data-file=- --project="$DST_PROJECT"
printf '%s' "<NEW_GOOGLE_CLIENT_SECRET>" | gcloud secrets create GOOGLE_CLIENT_SECRET --data-file=- --project="$DST_PROJECT"

# 3. New YouTube API key (from §4.5)
printf '%s' "<NEW_YOUTUBE_API_KEY>"      | gcloud secrets create YOUTUBE_API_KEY      --data-file=- --project="$DST_PROJECT"

# 4. New GCS service-account key (from §4.3)
gcloud secrets create GCS_SERVICE_ACCOUNT_KEY --data-file=/tmp/app-sa-key.json --project="$DST_PROJECT"
rm -f /tmp/app-sa-key.json   # do not leave keys on disk
```

> **Verify `SUPABASE_APP_DB`** still uses the Supabase **session pooler (port 5432)** with `No Reset On Close=true` (per CLAUDE.md). The connection target is unchanged by this migration.

> **`GOOGLE_CLOUD_PROJECT_ID` matters at runtime:** Vertex AI calls build the URL `projects/{GOOGLE_CLOUD_PROJECT_ID}/locations/{location}/...` ([GeminiVisionService.cs:298-302](VideoAnalysis.Server/Domain/AIServices/GeminiVisionService.cs#L298-L302)). It must equal `$DST_PROJECT` or Gemini analysis breaks.

---

## 5. Phase 2 — Container images into MyCoach (no downtime)

Two options. **Option A (recommended)** rebuilds via CI so images are fresh and reproducible; it depends on Phase 9 WIF being set up first. **Option B** copies the existing images immediately with no CI dependency.

### Option A — Rebuild via GitHub Actions
Do Phase 9 (WIF + repo secrets) first, then push to the `feature/gcp-vm-deploy` branch (or re-run the workflow). The workflow already parameterizes the registry by `secrets.GCP_PROJECT_ID` ([deploy-to-vm.yml:37-54](.github/workflows/deploy-to-vm.yml#L37-L54)), so updating that secret to `$DST_PROJECT` routes images to MyCoach.

### Option B — Server-side copy of existing images (fast, no rebuild)
```bash
gcloud auth configure-docker "$REGION-docker.pkg.dev" --quiet
for img in fighter-manager video-analysis app-client; do
  gcloud artifacts docker images copy \
    "$REGION-docker.pkg.dev/$SRC_PROJECT/$AR_REPO/$img:latest" \
    "$REGION-docker.pkg.dev/$DST_PROJECT/$AR_REPO/$img:latest" \
    --quiet
  echo "✓ copied $img"
done
```

Verify:
```bash
gcloud artifacts docker images list "$REGION-docker.pkg.dev/$DST_PROJECT/$AR_REPO" --include-tags
```

---

## 6. Phase 3 — Network, VM, firewall, and SSL cert (no downtime)

Build the new VM fully in parallel with the live old VM. It will not receive public traffic until DNS flips in Phase 8.

### 6.1 Reserve a static external IP
```bash
gcloud compute addresses create "$STATIC_IP_NAME" --region="$REGION" --project="$DST_PROJECT"
export NEW_IP=$(gcloud compute addresses describe "$STATIC_IP_NAME" --region="$REGION" \
  --project="$DST_PROJECT" --format='get(address)')
echo "New static IP: $NEW_IP"   # record this — DNS will point here in Phase 8
```

### 6.2 Firewall rules (mirror source)
First confirm the source definitions (Appendix A), then recreate. Defaults from `manual-vm-deployment-steps.md`:
```bash
gcloud compute firewall-rules create allow-http-80 \
  --network=default --allow=tcp:80 --source-ranges=0.0.0.0/0 \
  --target-tags=http-server --description="HTTP + Let's Encrypt" --project="$DST_PROJECT"

gcloud compute firewall-rules create allow-https-443 \
  --network=default --allow=tcp:443 --source-ranges=0.0.0.0/0 \
  --target-tags=https-server --description="HTTPS/TLS" --project="$DST_PROJECT"
```
> Note: the source used `--target-tags=http-server` for *both* rules. Standard GCP convention is `http-server` (80) and `https-server` (443). Whichever tags you choose, the VM in §6.3 must carry the **same** tags.

### 6.3 Provision the VM
Match the source VM's machine type, disk size, and image (read from Appendix A). Example with sensible defaults — **override to match source**:
```bash
gcloud compute instances create "$VM_NAME" \
  --project="$DST_PROJECT" \
  --zone="$ZONE" \
  --machine-type="e2-medium" \
  --image-family="ubuntu-2404-lts-amd64" \
  --image-project="ubuntu-os-cloud" \
  --boot-disk-size="30GB" \
  --boot-disk-type="pd-balanced" \
  --address="$NEW_IP" \
  --service-account="$VM_SA_EMAIL" \
  --scopes="https://www.googleapis.com/auth/cloud-platform" \
  --tags="http-server,https-server"
```

### 6.4 Bootstrap the VM host (Docker, Compose, Certbot)
SSH in and install the runtime (mirrors the "whenever VM restarts" block in `manual-vm-deployment-steps.md`):
```bash
gcloud compute ssh "vohoanvu@$VM_NAME" --zone="$ZONE" --project="$DST_PROJECT"

# On the VM:
sudo apt-get update
sudo apt-get install -y docker.io docker-compose certbot python3-certbot-nginx
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"   # log out/in for group to take effect
gcloud auth configure-docker "us-central1-docker.pkg.dev" --quiet
```

### 6.5 Lay down the application directory `~/app`
On the VM, create `~/app` with these files (sourced from the repo, with project-ID edits — see §10):
```
~/app/
├── docker-compose.yml                 # = repo docker-compose.prod.yml, registry → DST_PROJECT
├── refresh-env-secret-manager.sh      # PROJECT_ID → DST_PROJECT (see §10.3)
├── deploy-app.sh                      # = repo deploy-app-updates-in-vm.sh
├── CodeJitsu.Client/
│   ├── default.conf                   # unchanged (server_name thecodejitsu.com)
│   └── nginx.conf                     # unchanged
├── secrets/                           # created/filled by refresh script
└── letsencrypt/
    ├── config/                        # ← copied from old VM in §6.6
    └── webroot/                        # ACME http-01 challenge docroot
```

Fetch secrets into `.env` + write the GCS key file:
```bash
cd ~/app
bash ./refresh-env-secret-manager.sh   # PROJECT_ID must be DST_PROJECT inside the script
cat .env        # sanity check: ASPNETCORE_APP_DB present, GOOGLE_CLOUD_PROJECT_ID = DST_PROJECT
ls -l secrets/gcs-key.json
```

### 6.6 The "network certificate" — migrate Let's Encrypt SSL  ⚠️ KEY ITEM

This is the certificate the owner remembers configuring for HTTPS. It is a **Let's Encrypt** cert for `thecodejitsu.com` + `www.thecodejitsu.com`, referenced by Nginx at [default.conf:22-23](CodeJitsu.Client/default.conf#L22-L23) and mounted from `~/app/letsencrypt/config` → `/etc/letsencrypt` in the container ([docker-compose.prod.yml:31](docker-compose.prod.yml#L31)).

Because the **domain is unchanged**, the cleanest path is to **copy the live cert** from the old VM to the new one. A Let's Encrypt cert is bound to the domain name, **not** the IP, so it remains valid after the IP changes.

```bash
# From your workstation / Cloud Shell — copy the cert tree old VM → local → new VM.
# (Direct VM-to-VM scp is awkward; stage through your machine.)

# 1. Pull from OLD VM
gcloud compute scp --recurse \
  "vohoanvu@$VM_NAME:~/app/letsencrypt" ./letsencrypt-backup \
  --zone="$ZONE" --project="$SRC_PROJECT"

# 2. Push to NEW VM
gcloud compute scp --recurse \
  ./letsencrypt-backup/* "vohoanvu@$VM_NAME:~/app/letsencrypt/" \
  --zone="$ZONE" --project="$DST_PROJECT"
```

> **Renewal mechanism:** Inspect the old VM for how renewal runs (Appendix A: `systemctl list-timers | grep certbot`, `crontab -l`, `docker ps | grep certbot`). Replicate it on the new VM. The Nginx config already serves the ACME http-01 challenge from the webroot ([default.conf:6-8](CodeJitsu.Client/default.conf#L6-L8)), so the standard renewal is:
> ```bash
> # Host-certbot webroot renewal (run AFTER DNS points to the new VM, Phase 8):
> sudo certbot certonly --webroot -w ~/app/letsencrypt/webroot \
>   -d thecodejitsu.com -d www.thecodejitsu.com \
>   --config-dir ~/app/letsencrypt/config --deploy-hook "docker-compose -f ~/app/docker-compose.yml restart app-client"
> ```
> Copying the cert first means HTTPS works **immediately** at cutover; you then fix renewal at leisure (the cert is valid ~90 days). **Do not** run `certbot` against the new IP before DNS points to it — the http-01 challenge would fail and you risk hitting Let's Encrypt rate limits.

### 6.7 First boot of the new stack (still on the old IP for the public)
```bash
cd ~/app
sudo docker-compose pull
sudo docker-compose up -d
sudo docker ps      # all 4 containers Up
```
At this point the new VM serves the app on `$NEW_IP`, but the public DNS still points at the old VM. **Smoke-test against the raw IP** before the bucket migration and cutover:
```bash
# From your machine — bypass DNS using a Host header (cert will mismatch on IP; that's expected pre-DNS)
curl -k --resolve thecodejitsu.com:443:$NEW_IP https://thecodejitsu.com/swagger -I
curl --resolve thecodejitsu.com:80:$NEW_IP  http://thecodejitsu.com/ -I
```
> The fighter-manager may log a Supabase `MaxClients` warning if both old and new VMs hold pooled connections simultaneously. This is fine temporarily; it resolves when the old VM is stopped at cutover. If it blocks startup, use an alternate Supabase pooler connection string for the test, or proceed straight to the window.

---

## 7. Phase 4 — GCS bucket migration (reuse same name) ⚠️ MAINTENANCE WINDOW

This is the **only** step needing downtime, because the global bucket name `martial-art-demo-vids` must be released from `codejitsu` and recreated in MyCoach. Pre-stage data **before** the window to keep the window short.

### 7.1 BEFORE the window — pre-stage objects (no downtime)
```bash
# Create a temporary staging bucket in DST and bulk-copy everything into it.
gcloud storage buckets create "gs://$STAGE_BUCKET" \
  --project="$DST_PROJECT" --location="$REGION" --uniform-bucket-level-access

gcloud storage cp --recursive "gs://$BUCKET/*" "gs://$STAGE_BUCKET/" --project="$SRC_PROJECT"

# Record object count for later verification
gcloud storage ls --recursive "gs://$BUCKET/**" | wc -l
gcloud storage ls --recursive "gs://$STAGE_BUCKET/**" | wc -l   # should match
```

### 7.2 START the window — quiesce writes
- Announce maintenance.
- On the **old** VM, stop the app so no new uploads land in the old bucket:
  ```bash
  gcloud compute ssh "vohoanvu@$VM_NAME" --zone="$ZONE" --project="$SRC_PROJECT" \
    --command "cd ~/app && sudo docker-compose stop"
  ```
- Final incremental sync (catch any objects uploaded after pre-staging):
  ```bash
  gcloud storage rsync --recursive "gs://$BUCKET" "gs://$STAGE_BUCKET" --project="$SRC_PROJECT"
  ```

### 7.3 Release & recreate the bucket name
```bash
# Delete objects then bucket in SOURCE (releases the global name)
gcloud storage rm --recursive "gs://$BUCKET/**" --project="$SRC_PROJECT"
gcloud storage buckets delete "gs://$BUCKET" --project="$SRC_PROJECT"

# Recreate with the SAME name in DESTINATION (match source location/settings)
gcloud storage buckets create "gs://$BUCKET" \
  --project="$DST_PROJECT" --location="$REGION" --uniform-bucket-level-access

# Restore objects from staging (same-region, same-project → fast server-side copy)
gcloud storage cp --recursive "gs://$STAGE_BUCKET/*" "gs://$BUCKET/" --project="$DST_PROJECT"

# Verify counts match the pre-window number
gcloud storage ls --recursive "gs://$BUCKET/**" | wc -l
```

### 7.4 Grant the app SA access to the bucket + mirror CORS
```bash
gcloud storage buckets add-iam-policy-binding "gs://$BUCKET" \
  --member="serviceAccount:$APP_SA_EMAIL" --role="roles/storage.objectAdmin" \
  --project="$DST_PROJECT"
```
- If the source bucket had a **CORS** policy (browser uploads/signed-URL playback), export it from source first and apply: `gcloud storage buckets update gs://$BUCKET --cors-file=cors.json`.
- Confirm uniform bucket-level access / public-access settings match source (check Appendix A).

> **Why no DB rewrite:** objects are restored under identical keys and the bucket name is identical, so every stored `gs://martial-art-demo-vids/<guid>_<file>` URI in Supabase still resolves. `GenerateSignedUrlAsync` strips the `gs://{bucket}/` prefix it computes from config ([GoogleCloudStorageService.cs:44](VideoAnalysis.Server/Domain/GoogleCloudStorageService/GoogleCloudStorageService.cs#L44)) — identical bucket name ⇒ identical behavior. Vertex AI reads the same `gs://` URIs, now served from the MyCoach-owned bucket the `APP_SA` can access.

---

## 8. Phase 5 — Cutover (within / at end of the window)

### 8.1 Restart the new stack with the final bucket in place
```bash
gcloud compute ssh "vohoanvu@$VM_NAME" --zone="$ZONE" --project="$DST_PROJECT" \
  --command "cd ~/app && sudo docker-compose up -d && sudo docker ps"
```

### 8.2 Flip DNS
At the `thecodejitsu.com` DNS provider, update the **A records** for `@` and `www` to **`$NEW_IP`** (recorded in §6.1). TTL was lowered to 300s in §2.4, so propagation is quick.

### 8.3 Issue/confirm SSL on the new VM
Once DNS resolves to `$NEW_IP`:
```bash
# Verify the copied cert is being served
echo | openssl s_client -connect thecodejitsu.com:443 -servername thecodejitsu.com 2>/dev/null \
  | openssl x509 -noout -dates -subject

# Confirm renewal works end-to-end via the live http-01 path
sudo certbot renew --dry-run --config-dir ~/app/letsencrypt/config
```

### 8.4 End the window
Validate (§11), then announce service restored. Leave the **old VM stopped but not deleted** for rollback (§12).

---

## 9. Phase 6 — CI/CD repointing (GitHub Actions Workload Identity Federation)

The deploy workflow authenticates to GCP via **WIF** using three repo secrets ([deploy-to-vm.yml:22-33](.github/workflows/deploy-to-vm.yml#L22-L33)). Recreate the WIF chain in MyCoach.

### 9.1 Create the deployer SA + grant push rights
```bash
gcloud iam service-accounts create "$CICD_SA" \
  --display-name="GitHub Actions deployer" --project="$DST_PROJECT"
export CICD_SA_EMAIL="${CICD_SA}@${DST_PROJECT}.iam.gserviceaccount.com"

gcloud projects add-iam-policy-binding "$DST_PROJECT" \
  --member="serviceAccount:$CICD_SA_EMAIL" --role="roles/artifactregistry.writer"
```

### 9.2 Create the Workload Identity Pool + GitHub provider
```bash
gcloud iam workload-identity-pools create "github-pool" \
  --location="global" --project="$DST_PROJECT" --display-name="GitHub Actions pool"

export POOL_ID=$(gcloud iam workload-identity-pools describe "github-pool" \
  --location="global" --project="$DST_PROJECT" --format='get(name)')

gcloud iam workload-identity-pools providers create-oidc "github-provider" \
  --location="global" --workload-identity-pool="github-pool" --project="$DST_PROJECT" \
  --display-name="GitHub provider" \
  --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository" \
  --attribute-condition="assertion.repository=='vohoanvu/MartialArtTrainingAssistant'" \
  --issuer-uri="https://token.actions.githubusercontent.com"
```

### 9.3 Allow the GitHub repo to impersonate the deployer SA
```bash
gcloud iam service-accounts add-iam-policy-binding "$CICD_SA_EMAIL" \
  --project="$DST_PROJECT" --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/${POOL_ID}/attribute.repository/vohoanvu/MartialArtTrainingAssistant"

# Provider resource name for the GitHub secret:
gcloud iam workload-identity-pools providers describe "github-provider" \
  --location="global" --workload-identity-pool="github-pool" \
  --project="$DST_PROJECT" --format='get(name)'
```

### 9.4 Update GitHub repository secrets
In `vohoanvu/MartialArtTrainingAssistant` → Settings → Secrets and variables → Actions:
| Secret | New value |
|---|---|
| `GCP_PROJECT_ID` | `project-afa815fe-26c6-40c3-a8b` |
| `GCP_SERVICE_ACCOUNT` | `github-actions-deployer@project-afa815fe-26c6-40c3-a8b.iam.gserviceaccount.com` |
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | the provider resource name from §9.3 |

---

## 10. Phase 7 — Repository file changes (commit these)

Update hardcoded `codejitsu` references. Do these on a branch and PR them.

### 10.1 `docker-compose.prod.yml` — registry project ID
Change all three image paths from `us-central1-docker.pkg.dev/codejitsu/codejitsu-repo/...` → `.../project-afa815fe-26c6-40c3-a8b/codejitsu-repo/...` ([docker-compose.prod.yml:3,12,23](docker-compose.prod.yml#L3)).

### 10.2 `manual-vm-deployment-steps.md` & `deploy-app-updates-in-vm.sh`
- Replace `--project=codejitsu` and `PROJECT_ID="codejitsu"` with the MyCoach ID.
- Update the SSH example: `gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-c --project=project-afa815fe-26c6-40c3-a8b`.
- Update firewall-rule snippets `--project`.

### 10.3 `refresh-env-secret-manager.sh` (lives on the VM; keep a copy in repo)
- `PROJECT_ID="project-afa815fe-26c6-40c3-a8b"`.
- Verify `SECRETS_TO_FETCH` includes every key the compose file consumes: `SUPABASE_APP_DB, ASPNETCORE_APP_PORT_1/2, CLIENT_APP_PORTS, ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION, JWT_*, GOOGLE_CLIENT_ID/SECRET, YOUTUBE_API_KEY, GOOGLE_CLOUD_PROJECT_ID, GOOGLE_CLOUD_BUCKET_NAME, GEMINI_VISION_*, XAIGROK_*` plus `GCS_SERVICE_ACCOUNT_KEY`.
- The static line `GoogleCloud__ServiceAccountKeyPath=/app/secrets/codejitsu-cloud-storage-service-account.json` must match the compose mount target ([docker-compose.prod.yml:19](docker-compose.prod.yml#L19)).

### 10.4 `CLAUDE.md` — Deployment section
Update project ID, Artifact Registry path, and SSH command to MyCoach.

### 10.5 No change needed
- `CodeJitsu.Client/default.conf`, `nginx.conf` — domain unchanged.
- `GOOGLE_CLOUD_BUCKET_NAME` — bucket name unchanged.
- Legacy `k8s/`, `cloudbuild-test.yaml`, `build-and-push-to-acr.sh`, `docs/deploy-to-gke.sh` — not in production; leave as-is or delete in a separate cleanup PR.

---

## 11. Phase 8 — Validation & smoke tests

Run after cutover (§8). All must pass before declaring success.

- [ ] `https://thecodejitsu.com` loads the React app over HTTPS with a **valid** cert (correct dates, issuer Let's Encrypt).
- [ ] `https://thecodejitsu.com/swagger` and `/vid/swagger` reachable (if `ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION=true`).
- [ ] **Auth:** email/password login works; **Google sign-in** completes through `/signin-google-callback` (validates new OAuth client + redirect URI).
- [ ] **YouTube:** homepage video list loads and search returns results (validates new `YOUTUBE_API_KEY`).
- [ ] **SignalR:** real-time video-share notification fires to a second logged-in session (validates `/videoShareHub` proxy).
- [ ] **GCS playback:** an **existing** (pre-migration) video plays via signed URL (validates bucket-name reuse + `APP_SA` access + DB `gs://` paths intact).
- [ ] **GCS upload:** upload a **new** video → object appears in `gs://martial-art-demo-vids` in MyCoach.
- [ ] **Vertex/Gemini:** trigger a video analysis (Hangfire job) → completes, returns JSON (validates `GOOGLE_CLOUD_PROJECT_ID`=MyCoach + `aiplatform.user`). Check `/hangfire` dashboard.
- [ ] `sudo docker ps` on the new VM: all 4 containers `Up`; no crash loops in `sudo docker-compose logs --tail=100`.
- [ ] `sudo certbot renew --dry-run` succeeds (renewal path healthy).
- [ ] **Billing:** after 24h, MyCoach billing shows Compute/Storage/AR/Vertex usage; `codejitsu` usage trends to zero.
- [ ] **CI/CD:** push a trivial commit to `feature/gcp-vm-deploy` → workflow authenticates via WIF and pushes images to the **MyCoach** registry.

---

## 12. Phase 9 — Rollback & decommission

### Rollback (if validation fails within the window)
1. Re-point DNS A records back to the **old VM IP**.
2. Recreate the bucket name in `codejitsu` only if §7.3 was executed — **this is the point of no return**, so do **not** proceed past §7.3 until §6.7 smoke tests pass. Keep `gs://$STAGE_BUCKET` until success is confirmed; it is the recovery copy of all objects.
3. Restart the old VM stack: `cd ~/app && sudo docker-compose up -d`.

### Decommission source (only after ~1 week stable on MyCoach)
1. Stop, then delete the old VM `thecodejitsu-app-vm` in `codejitsu`.
2. Release the old static IP (if any) and delete old firewall rules.
3. Delete old Artifact Registry images/repo.
4. Disable/delete the old OAuth client and YouTube API key in `codejitsu`.
5. **Disable** (don't immediately destroy) old Secret Manager secrets; destroy after a grace period.
6. Delete `gs://$STAGE_BUCKET` staging bucket in MyCoach.
7. Optionally schedule shutdown/deletion of the `codejitsu` project once billing confirms no further charges.

---

## 13. Google Cloud Remote MCP Servers (for the executing agents)

Per the request, configure the **official Google Cloud remote MCP servers** so agents can drive GCP directly. These are fully-managed remote endpoints following the pattern `https://<service>.googleapis.com/mcp`, authenticated via **OAuth 2.0 + IAM** (no API keys).

### 13.1 Availability (as of the docs check)
| Service | Remote MCP endpoint | Useful for this migration |
|---|---|---|
| Compute Engine | `https://compute.googleapis.com/mcp` | VM, static IP, firewall inspection |
| Cloud Storage | `https://storage.googleapis.com/storage/mcp` | Bucket/object verification |
| Resource Manager | `https://cloudresourcemanager.googleapis.com/mcp` | Project/IAM inspection |
| Cloud Logging | `https://logging.googleapis.com/mcp` | Post-cutover log triage |
| Cloud Monitoring | `https://monitoring.googleapis.com/mcp` | Health/metrics |

> **Not yet available as MCP servers:** Secret Manager, Artifact Registry, IAM/Service Accounts, Cloud DNS. For those, agents must use the **`gcloud` CLI via Bash** (as written throughout this plan). Re-check the [supported products page](https://docs.cloud.google.com/mcp/supported-products) as Google adds more.

### 13.2 IAM prerequisites for MCP access
Grant the executing identity (`vohoanvu96@gmail.com`) the MCP tool-user role plus the relevant service roles **in MyCoach**:
```bash
gcloud projects add-iam-policy-binding "$DST_PROJECT" \
  --member="user:$ACCOUNT" --role="roles/mcp.toolUser"
# Plus service roles as needed, e.g. roles/compute.viewer, roles/storage.objectViewer,
# roles/monitoring.viewer, roles/logging.viewer (read-only) or admin roles for mutations.
```

### 13.3 Add to `.claude/.mcp.json`
Append these to the existing `mcpServers` block (alongside `github` and `supabase`):
```json
{
  "mcpServers": {
    "gcp-compute":         { "type": "http", "url": "https://compute.googleapis.com/mcp" },
    "gcp-storage":         { "type": "http", "url": "https://storage.googleapis.com/storage/mcp" },
    "gcp-resourcemanager": { "type": "http", "url": "https://cloudresourcemanager.googleapis.com/mcp" },
    "gcp-logging":         { "type": "http", "url": "https://logging.googleapis.com/mcp" },
    "gcp-monitoring":      { "type": "http", "url": "https://monitoring.googleapis.com/mcp" }
  }
}
```
Then authenticate each server with `/mcp` in Claude Code (OAuth browser flow). Ensure local ADC is present for any tooling that needs it: `gcloud auth application-default login`.

---

## 14. ⚠️ Security finding (act before/independently of migration)

A **GitHub Personal Access Token is hardcoded and committed** in both [.mcp.json:7](.mcp.json#L7) and [.claude/.mcp.json:7](.claude/.mcp.json#L7) (`ghp_...`). This token is exposed in git history.

**Remediate now:**
1. **Revoke** the token at https://github.com/settings/tokens immediately.
2. Replace MCP auth with a non-committed mechanism (env var / local-only `.claude/settings.local.json`, which is gitignored).
3. Consider history scrubbing (e.g. `git filter-repo`) if the repo is or was ever public.

This is independent of the GCP migration but should not ship to the company project as-is.

---

## Appendix A — Source inventory discovery

Run against `codejitsu` and save output before starting. Fills in the exact values referenced above.

```bash
P=codejitsu; Z=us-central1-c; R=us-central1

# VM spec (machine type, disk, tags, attached SA, image)
gcloud compute instances describe thecodejitsu-app-vm --zone="$Z" --project="$P" \
  --format="yaml(machineType,disks,tags,serviceAccounts,networkInterfaces)"

# Current public IP + whether it's a reserved static address
gcloud compute instances describe thecodejitsu-app-vm --zone="$Z" --project="$P" \
  --format='get(networkInterfaces[0].accessConfigs[0].natIP)'
gcloud compute addresses list --project="$P"

# Firewall rules
gcloud compute firewall-rules list --project="$P" \
  --format="table(name,sourceRanges.list(),allowed[].map().firewall_rule().list(),targetTags.list())"

# Enabled APIs
gcloud services list --enabled --project="$P" --format="value(config.name)"

# Secret names (values fetched in §4.6)
gcloud secrets list --project="$P" --format="value(name)"

# Artifact Registry images
gcloud artifacts docker images list "$R-docker.pkg.dev/$P/codejitsu-repo" --include-tags

# GCS bucket settings (location, UBLA, CORS) + object count
gcloud storage buckets describe gs://martial-art-demo-vids --project="$P" \
  --format="yaml(location,uniform_bucket_level_access,cors_config,iam_configuration)"
gcloud storage ls --recursive "gs://martial-art-demo-vids/**" | wc -l

# On the VM: how is the cert renewed?
gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone="$Z" --project="$P" --command "
  sudo certbot certificates --config-dir ~/app/letsencrypt/config 2>/dev/null;
  systemctl list-timers 2>/dev/null | grep -i certbot;
  crontab -l 2>/dev/null | grep -i certbot;
  docker ps --format '{{.Names}} {{.Image}}' | grep -i certbot;
  ls -la ~/app/letsencrypt/config/live/thecodejitsu.com/ 2>/dev/null"

# OAuth client redirect URIs / API key restrictions → read in Cloud Console:
#   codejitsu → APIs & Services → Credentials
```

---

## Execution order summary

```
2  Pre-flight gates (billing, perms, DNS TTL↓, inventory)        ── blocking
4  Foundation: APIs, SAs, IAM, AR repo, OAuth, API key, secrets  ── no downtime
5  Images into MyCoach (CI rebuild or image copy)                ── no downtime
6  Static IP, firewall, VM, host bootstrap, app dir, COPY CERT   ── no downtime
   └─ 6.7 smoke-test new VM via --resolve (point of no return NOT yet crossed)
7  ⚠ WINDOW: pre-stage bucket → stop old app → delete+recreate   ── downtime
   bucket name in MyCoach → restore objects                         (POINT OF NO RETURN at 7.3)
8  Cutover: start new stack → flip DNS → confirm SSL → window end ── downtime tail
9  CI/CD: WIF pool/provider/SA → update 3 GitHub secrets         ── no downtime
10 Repo edits: project-ID references → PR                         ── no downtime
11 Validation & smoke tests                                       ── verify
12 Rollback ready / decommission source after ~1 week stable      ── cleanup
```
