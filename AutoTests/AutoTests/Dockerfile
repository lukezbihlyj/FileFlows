FROM mcr.microsoft.com/playwright:v1.26.0-focal AS base

# Install dotnet SDK
RUN wget https://dot.net/v1/dotnet-install.sh  && \
    bash dotnet-install.sh -c 8.0 --install-dir /dotnet && \
    rm -f dotnet-install.sh && \
    chmod 775 /dotnet    

# Copy the application files
COPY /FileFlows /app/FileFlows
COPY /AutoTests /app/AutoTests
COPY /docker-entrypoint.sh /app/docker-entrypoint.sh

EXPOSE 5276
EXPOSE 19222

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

RUN /dotnet/dotnet dev-certs https

# Create directories for logs and test results
RUN mkdir -p /app/FileFlows/Logs /app/tests-results

# Make the entrypoint script executable
RUN chmod +x /app/docker-entrypoint.sh

# set the working directory
WORKDIR /app

# Ensure the container exits after the tests are done
ENTRYPOINT ["/app/docker-entrypoint.sh"]