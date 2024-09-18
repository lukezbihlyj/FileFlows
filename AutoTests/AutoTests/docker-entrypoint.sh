#!/bin/bash

# Start the FileFlows server in the background
nohup /dotnet/dotnet /app/FileFlows/Server/FileFlows.Server.dll --urls=http://*:5276 --docker > /dev/null 2>&1 &

# Capture the server PID
SERVER_PID=$!

# Wait for the server to fully start up
sleep 20

# Run the tests
/dotnet/dotnet test /app/AutoTests/FileFlows.AutoTests.dll --filter FullyQualifiedName=FileFlowsTests.Tests.InitialTests --logger "trx;LogFileName=/app/tests-results/test-results.trx"

# Stop the server after tests are completed
kill $SERVER_PID || true

# Indicate that tests have completed
echo "Tests complete"