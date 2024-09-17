FROM mcr.microsoft.com/playwright/dotnet:v1.47.0-noble

COPY /FileFlows /app/FileFlows
COPY /AutoTests /app/AutoTests

# Start the FileFlows server
# Ensure the server runs in the background
RUN dotnet /app/FileFlows/Server/FileFlows.dll &

# Wait for the server to fully start up (this could be replaced with a more reliable check)
RUN sleep 20

# Run the tests
RUN dotnet test /app/AutoTests/FileFlows.AutoTests.dll --filter FullyQualifiedName=FileFlowsTests.Tests.InitialTests

# Stop the server after tests are completed
RUN pkill -f 'dotnet /app/FileFlows/Server/FileFlows.dll'

# Ensure the container exits after the tests are done
CMD ["echo", "Tests completed"]