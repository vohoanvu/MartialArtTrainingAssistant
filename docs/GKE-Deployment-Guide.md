# Google Kubernetes Engine (GKE) Deployment Guide

## Overview

This guide describes the process to deploy the Martial Art Training Assistant application to a Google Kubernetes Engine (GKE) cluster. The application consists of four services: `fighter-manager`, `video-sharing`, `match-maker`, and `frontend`, each running in a separate pod. The database is hosted externally on Supabase (PostgreSQL), and the deployment uses Google Container Registry (GCR) for image storage and Kubernetes for orchestration.

## Prerequisites

Before starting, ensure you have the following:

- **Google Cloud Account**: With billing enabled and a project created (e.g., `your-gcp-project-id`).
- **Google Cloud SDK**: Installed and configured (`gcloud init`).
- **Docker**: Installed for building images.
- **kubectl**: Installed for interacting with the Kubernetes cluster.
- **Git**: Installed to clone the repository.
- **Repository**: Cloned from `https://github.com/vohoanvu/MartialArtTrainingAssistant.git`.
- **Supabase Account**: With a PostgreSQL database set up and connection string available.
- **Environment Variables**: Configured in a `.env` file or as environment variables:
  - `PROJECT_ID`: Your Google Cloud project ID.
  - `ASPNETCORE_APP_DB`: Supabase PostgreSQL connection string (e.g., `User ID=postgres;Password=your-password;Server=aws-0-us-west-1.pooler.supabase.com;Port=5432;Database=postgres;Pooling=true;`).
  - `YOUTUBE_API_KEY`: Google API key for YouTube features.
  - `GOOGLE_CLOUD_PROJECT_ID`: Google Cloud project ID.
  - `GOOGLE_CLOUD_BUCKET_NAME`: Google Cloud Storage bucket name.
  - `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`: For Google OAuth.
  - `JWT_AUDIENCE`, `JWT_ISSUER`, `JWT_KEY`: For JWT authentication.
  - `GEMINI_VISION_LOCATION`, `GEMINI_VISION_MODEL`, `GEMINI_VISION_VIDEO_ANALYSIS_PROMPT`: For Vertex AI Gemini Vision.
  - `GoogleCloud__ServiceAccountKeyPath`: Path to the Google Cloud service account key JSON file (e.g., `/app/secrets/gcp-key.json` in the container).

## Deployment Process

### Step 1: Set Up Google Cloud Environment

1. **Log in to Google Cloud**:
   ```bash
   gcloud auth login
   gcloud config set project your-gcp-project-id
   ```

2. **Enable Required APIs**:
   ```bash
   gcloud services enable container.googleapis.com
   gcloud services enable containerregistry.googleapis.com
   gcloud services enable cloudbuild.googleapis.com
   ```

3. **Create a GKE Cluster** (if not already created):
   ```bash
   gcloud container clusters create codejitsu-cluster \
     --zone us-central1-a \
     --machine-type e2-standard-4 \
     --num-nodes 3
   ```
   - Adjust `--machine-type` and `--num-nodes` based on your needs.
   - Replace `us-central1-a` with your preferred zone.

4. **Authenticate Docker for GCR**:
   ```bash
   gcloud auth configure-docker us-central1-docker.pkg.dev
   ```

### Step 2: Configure Secrets

1. **Create a Google Cloud Service Account Key**:
   - In Google Cloud Console, navigate to IAM & Admin > Service Accounts.
   - Create or select a service account with permissions for Google Cloud Storage and Vertex AI.
   - Generate a JSON key and save it securely (e.g., `./secrets/gcp-key.json`).
   - **Do not commit this file to Git**. Ensure it’s in `.gitignore`.

2. **Create Kubernetes Secrets**:
   - Update `k8s/prod/secrets.yaml` with base64-encoded values for sensitive data (e.g., `ASPNETCORE_APP_DB`, `JWT_KEY`, etc.).
   - Example for encoding:
     ```bash
     echo -n 'your-supabase-connection-string' | base64
     ```
   - Place the service account key in `secrets.yaml`:
     ```yaml
     apiVersion: v1
     kind: Secret
     metadata:
       name: codejitsu-secrets
       namespace: prod
     type: Opaque
     data:
       gcp-key.json: <base64-encoded-gcp-key.json>
       ASPNETCORE_APP_DB: <base64-encoded-supabase-connection-string>
       JWT_KEY: <base64-encoded-jwt-key>
       # Add other secrets as needed
     ```
   - Mount the secret in `video-sharing-deployment.yaml`:
     ```yaml
     volumes:
       - name: gcp-key
         secret:
           secretName: codejitsu-secrets
           items:
             - key: gcp-key.json
               path: gcp-key.json
     volumeMounts:
       - name: gcp-key
         mountPath: /app/secrets/gcp-key.json
         subPath: gcp-key.json
         readOnly: true
     ```

### Step 3: Run the Deployment Script

1. **Save the Script**:
   - Save the `deploy-to-gke.sh` script from the provided artifact to the repository root.
   - Make it executable:
     ```bash
     chmod +x deploy-to-gke.sh
     ```

2. **Set Environment Variables**:
   - Either export variables:
     ```bash
     export PROJECT_ID=your-gcp-project-id
     export ASPNETCORE_APP_DB="User ID=postgres;Password=your-password;Server=aws-0-us-west-1.pooler.supabase.com;Port=5432;Database=postgres;Pooling=true;"
     # Add other variables as needed
     ```
   - Or create a `.env` file and source it:
     ```bash
     source .env
     ```

3. **Execute the Script**:
   ```bash
   ./deploy-to-gke.sh
   ```
   - The script will:
     - Authenticate with Google Cloud.
     - Build and push Docker images to `us-central1-docker.pkg.dev/$PROJECT_ID/codejitsu-repo`.
     - Generate Kubernetes manifests in `k8s/prod/generated/`.
     - Apply Secrets, Deployments, and Ingress to the `prod` namespace.
     - Verify pod status and provide the Ingress URL.

### Step 4: Verify the Deployment

1. **Check Pods**:
   ```bash
   kubectl get pods --namespace=prod -o wide
   ```
   - Ensure all pods are in `Running` status.

2. **Check Services**:
   ```bash
   kubectl get services --namespace=prod
   ```

3. **Get Ingress URL**:
   ```bash
   kubectl get ingress codejitsu-ingress --namespace=prod -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
   ```
   - Access the application at `http://<ingress-ip>`.
   - Routes:
     - `/` → Frontend
     - `/swagger` → FighterManager API
     - `/vid/swagger` → VideoSharing API
     - `/pair/swagger` → MatchMaker API

4. **Monitor Logs** (if needed):
   ```bash
   kubectl logs -l app=fighter-manager --namespace=prod
   ```

### Step 5: Post-Deployment

1. **Database Migration**:
   - The `db-migration-job.yaml` applies migrations to the Supabase database.
   - Verify the job completed:
     ```bash
     kubectl get jobs --namespace=prod
     ```
   - Check job logs if issues arise:
     ```bash
     kubectl logs -l job-name=db-migration-job --namespace=prod
     ```

2. **Update DNS** (Optional):
   - Assign a domain to the Ingress IP using your DNS provider.
   - Update `ingress.yaml` to include the domain:
     ```yaml
     spec:
       rules:
         - host: your-domain.com
           http:
             paths:
               # Existing paths
     ```

3. **Scaling** (if needed):
   - Adjust replicas in deployment YAMLs (e.g., `replicas: 2`) and reapply:
     ```bash
     kubectl apply -f k8s/prod/generated/fighter-manager-deployment.yaml --namespace=prod
     ```

## Troubleshooting

- **Pods Not Starting**:
  - Check pod logs: `kubectl logs <pod-name> --namespace=prod`.
  - Verify environment variables and secrets are correctly set.
- **Ingress Not Assigning IP**:
  - Wait a few minutes and check again: `kubectl get ingress --namespace=prod`.
  - Ensure the Ingress controller is running: `kubectl get pods -n ingress-nginx`.
- **Database Connection Issues**:
  - Verify the Supabase connection string in `secrets.yaml`.
  - Ensure Supabase allows connections from the GKE cluster’s IP range.
- **Image Pull Errors**:
  - Confirm images exist in GCR: `gcloud container images list-tags us-central1-docker.pkg.dev/$PROJECT_ID/codejitsu-repo`.
  - Check GKE service account permissions for GCR.

## Cleanup

To delete the deployment:
```bash
kubectl delete namespace prod
```

To delete the GKE cluster:
```bash
gcloud container clusters delete codejitsu-cluster --zone us-central1-a
```

## Notes

- **Security**: Ensure `secrets.yaml` is not committed to Git. Use a secret management tool (e.g., Google Secret Manager) for production.
- **Cost**: Monitor GKE and GCR usage to avoid unexpected charges. Use Google Cloud’s cost calculator.
- **Scaling**: For high traffic, consider horizontal pod autoscaling or increasing node count.
- **Mobile Application**: The deployment supports backend APIs for both web and mobile (React Native Expo) applications, with the same Ingress routing.