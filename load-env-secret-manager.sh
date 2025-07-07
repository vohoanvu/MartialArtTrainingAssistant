#!/bin/bash
# Create secrets from .env file

# Set your GCP project ID
PROJECT_ID="codejitsu"

# Read .env file and create secrets
while IFS= read -r line; do
  # Skip comments and empty lines
  if [[ $line =~ ^#.*$ ]] || [[ -z "$line" ]]; then
    continue
  fi
  
  # Extract key and value
  if [[ $line =~ ^([^=]+)=(.*)$ ]]; then
    key="${BASH_REMATCH[1]}"
    value="${BASH_REMATCH[2]}"
    
    # Remove quotes if present
    value=$(echo "$value" | sed 's/^"\(.*\)"$/\1/' | sed "s/^'\(.*\)'$/\1/")
    
    # Create secret
    echo "Creating secret: $key"
    echo "$value" | gcloud secrets create "$key" --data-file=- --project="$PROJECT_ID"
  fi
done < .env