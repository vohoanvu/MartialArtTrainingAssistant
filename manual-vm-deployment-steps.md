### **Report: Manual Deployment & Troubleshooting Guide for TheCodeJitsu App to GCP VM**

*   **Date:** 2025-07-07
*   **Author:** VU VO
*   **Objective:** To provide a standardized workflow for manually deploying new versions of the application to the Google Cloud VM and a checklist for troubleshooting common deployment issues.

#### **Assumptions**

This guide assumes the following conditions are met:
1.  Application code changes have been pushed to your GitHub repository.
2.  Your GitHub Actions workflow has successfully built the new Docker images and pushed them to Google Artifact Registry (`us-central1-docker.pkg.dev/codejitsu/codejitsu-repo`).
3.  You have SSH access to the production VM (`thecodejitsu-app-vm`).

---

### **Part 1: Standard Deployment Workflow (The "Happy Path")**

Follow these steps each time you want to deploy an update.

**Step 1: Connect to your VM**

Open a terminal on your local machine and SSH into the server.

```bash
gcloud compute ssh vohoanvu@thecodejitsu-app-vm --zone=us-central1-c --project=codejitsu
```

Ensure these steps are completed whenever VM restarts:
```bash
sudo apt-get update
sudo apt-get install -y docker.io
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker ${USER}
sudo apt-get install -y docker-compose
sudo apt install certbot python3-certbot-nginx 
sudo certbot --nginx
```

**Step 2: Navigate to the Application Directory**

All subsequent commands should be run from this directory.

```bash
cd ~/app
```

**Step 3: Authenticate Docker with Google Artifact Registry**

This command configures Docker to securely connect to your private image registry. This token expires, so it's good practice to run it occasionally, especially if you encounter pull errors.

```bash
gcloud auth configure-docker us-central1-docker.pkg.dev
```

**Step 4: Pull the Latest Container Images**

This command checks your `docker-compose.yml` file and downloads the newest versions of the images from the registry.

```bash
sudo docker-compose pull
```
*You will see Docker pulling the new layers for the images that have changed.*

- if needed, refresh the latest values from Secret Manager with this script:

```bash
#!/bin/bash
PROJECT_ID="codejitsu"
ENV_FILE=".env"
# Add or remove secret names from this list as needed.
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
  "XAIGROK_API_KEY"
)
# --- End of Configuration ---


# Start with a clean .env file by deleting it if it exists.
echo "Creating a fresh .env file..."
> "$ENV_FILE"

# Loop through each secret name in the array.
for secret_name in "${SECRETS_TO_FETCH[@]}"; do
  echo "Fetching secret: $secret_name..."
  # Fetch the latest version of the secret's value.
  # The --quiet flag suppresses the "Accessed version..." message.
  secret_value=$(gcloud secrets versions access latest --secret="$secret_name" --project="$PROJECT_ID" --quiet)

  # Write the KEY=VALUE pair to the .env file.
  echo "$secret_name=$secret_value" >> "$ENV_FILE"
done

# --- Static & Hardcoded Values ---
# Add any variables that are not stored in Secret Manager here.
echo "Adding static environment variables..."
echo "GoogleCloud__ServiceAccountKeyPath=/app/secrets/codejitsu-cloud-storage-service-account.json" >> "$ENV_FILE"


echo ""
echo ".env file has been successfully updated with the latest secrets."
```

**Step 5: Restart the Application Services**

This is the core deployment command. It gracefully stops the running containers and starts new ones using the freshly pulled images. It's smart enough to only restart services whose images were updated. The `-d` flag runs them in the background.

```bash
sudo docker-compose up -d
```
*You will see lines like `Recreating app_fighter-manager_1 ... done`.*

**Step 6: Verify the Deployment**

Check that all containers started correctly and are in an "Up" state.

```bash
sudo docker ps
```
*Pay attention to the `CREATED` and `STATUS` columns. The `CREATED` time should be just a few seconds or minutes ago.*

**Step 7: Clean Up Old Images (Optional but Recommended)**

After a successful deployment, you can remove the old, unused Docker images to free up disk space.

```bash
sudo docker image prune -a -f
```

Your deployment is now complete. Your site is running the new code.

---

### **Part 2: Troubleshooting Workflow (When Things Go Wrong)**

If the site is down or not working correctly after Step 6, follow this diagnostic process.

**Symptom 1: One or more containers are missing from `sudo docker ps` output.**

This means a container is crashing immediately on startup. This is the most common failure mode.

*   **Action:** The first and most important step is to check the logs. The error message will almost always be here.

    ```bash
    # This shows the last 100 log lines from ALL containers (running or stopped)
    sudo docker-compose logs --tail=100
    ```

**Symptom 2: The logs show `Connection refused` or `Failed to connect to ...`**

This indicates a networking or configuration error. The application cannot reach the database or another service.

*   **Action 1: Verify the Connection String in the `.env` file.** Check for typos, incorrect passwords, or wrong hostnames.

    ```bash
    cat .env
    ```

*   **Action 2: Verify the correct environment variable is being set.** Based on our experience, this is the most critical check. Open your `docker-compose.yml` and ensure the `environment` section is setting the variable name that your application code is actually reading.

    ```bash
    # Open the file for viewing/editing
    sudo nano docker-compose.yml
    ```
    *Correct section from our final working file:*
    ```yaml
    environment:
      - ASPNETCORE_APP_DB=${SUPABASE_APP_DB}
    ```

**Symptom 3: The logs show `nginx: [emerg] "..." is not allowed here` or other NGINX errors.**

This is a syntax error in your web server's configuration file.

*   **Action:** Edit the NGINX configuration file and look for syntax mistakes. The most common error is forgetting to wrap your `server` block inside an `http` block.

    ```bash
    sudo nano ./SampleAspNetReactDockerApp.Client/nginx.conf
    ```

**Symptom 4: The `docker-compose pull` command fails with "pull access denied".**

This is an authentication error.

*   **Action:** Re-run the `gcloud auth` command from Step 3 of the deployment workflow.

    ```bash
    gcloud auth configure-docker us-central1-docker.pkg.dev
    ```