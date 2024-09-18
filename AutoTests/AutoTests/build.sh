#!/bin/bash

rm -rf test-results > /dev/null 2>&1 &
rm -rf logs > /dev/null 2>&1 &

# Build the Docker image
echo Building Docker Image
docker build -f Dockerfile -t fileflows-autotests --build-arg TZ=Pacific/Auckland .

# Run the container
echo Running Docker image
docker run --rm -p 19222:5276 -v "$(pwd)/logs:/app/FileFlows/Logs" -v "$(pwd)/test-results:/app/tests-results" fileflows-autotests
