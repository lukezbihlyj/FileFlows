#!/bin/bash

# Function to check if the server is up
wait_for_server() {
    local url="http://localhost:5276/index.html"
    local max_attempts=30
    local attempt=1
    local delay=5  # Delay in seconds between checks

    while [ $attempt -le $max_attempts ]; do
        if curl --silent --fail --head "$url" | grep "200 OK" > /dev/null; then
            echo "Server is up and running."
            return 0
        fi
        echo "Waiting for server to start ($attempt/$max_attempts)..."
        sleep $delay
        attempt=$((attempt + 1))
    done

    echo "Server did not start within the expected time."
    return 1
}

# Start the FileFlows server in the background
pushd /app/FileFlows/Server
nohup /dotnet/dotnet FileFlows.Server.dll --urls=http://*:5276/ --docker > /dev/null 2>&1 &
#nohup /dotnet/dotnet FileFlows.Server.dll --docker > /dev/null 2>&1 &

# Capture the server PID
SERVER_PID=$!

popd

# Wait for the server to fully start up
wait_for_server

# Run the tests
/dotnet/dotnet test /app/AutoTests/FileFlows.AutoTests.dll \
  --filter FullyQualifiedName=FileFlowsTests.Tests.InitialTests \
  --logger "trx;LogFileName=/app/tests-results/InitialTests.trx"

# Check if InitialTests passed
if [ $? -eq 0 ]; then
    echo "InitialTests passed. Running other tests..."

    # Step 2: Run all other tests excluding InitialTests and append to the same log file
    /dotnet/dotnet test /app/AutoTests/FileFlows.AutoTests.dll \
        --filter "FullyQualifiedName!=FileFlowsTests.Tests.InitialTests" \
        --logger "trx;LogFileName=/app/tests-results/AutoTests.trx"
else
    echo "InitialTests failed. Not running other tests."
fi

# Stop the server after tests are completed
kill $SERVER_PID || true