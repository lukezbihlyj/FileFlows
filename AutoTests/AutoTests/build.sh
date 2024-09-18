#!/bin/bash

rm -rf test-results > /dev/null 2>&1 &
rm -rf logs > /dev/null 2>&1 &

echo "Current directory: $(pwd)"
echo "Current directory: /home/john/appdata/felix$(pwd)"
mkdir -p "$(pwd)/logs"
mkdir -p "$(pwd)/test-results"

# Build the Docker image
echo Building Docker Image
docker build -f Dockerfile -t fileflows-autotests --build-arg TZ=Pacific/Auckland .

# Run the container
echo Running Docker image
docker run --rm \
    -p 19222:5276 \
    -v "/home/john/appdata/felix$(pwd)/logs:/app/FileFlows/Logs" \
    -v "/home/john/appdata/felix$(pwd)/test-results:/app/tests-results" \
    -e FF_TEMP_PATH=/app/tests-results \
    fileflows-autotests
