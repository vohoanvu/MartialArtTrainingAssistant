#!/bin/bash
set -e

# Ensure required variables are set
if [ -z "$SHORT_SHA" ] || [ -z "$PROJECT_ID" ]; then
    echo "Error: SHORT_SHA and PROJECT_ID must be set as environment variables."
    exit 1
fi

# Directory containing YAML templates
TEMPLATE_DIR="k8s/test"
OUTPUT_DIR="k8s/test/generated"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Process each YAML file
for file in "$TEMPLATE_DIR"/*.yaml; do
    filename=$(basename "$file")
    output_file="$OUTPUT_DIR/$filename"
    echo "Processing $file -> $output_file"

    # Substitute variables using sed
    sed "s|\${PROJECT_ID}|$PROJECT_ID|g" "$file" |
        sed "s|\${SHORT_SHA}|$SHORT_SHA|g" >"$output_file"
done

echo "YAML files generated at $OUTPUT_DIR"
