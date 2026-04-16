# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS restore
ARG GITHUB_TOKEN
ARG GITHUB_USER
ARG SERVICE_NAME

WORKDIR /app

# Copy NuGet configuration and build props
COPY nuget.config .
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

# Copy ONLY the project files for services we actually use
COPY ["src/Common/Core/Core.csproj", "src/Common/Core/"]
COPY ["src/Apps/RestAPI/${SERVICE_NAME}/${SERVICE_NAME}.csproj", "src/Apps/RestAPI/${SERVICE_NAME}/"]

# Restore dependencies for the specific service
ENV NUGET_XMLDOC_MODE=none
RUN echo ">>> Restoring NuGet packages for ${SERVICE_NAME}..." && \
    dotnet restore "src/Apps/RestAPI/${SERVICE_NAME}/${SERVICE_NAME}.csproj" /p:WarningLevel=0

# Stage 2: Build Common/Shared projects
FROM restore AS build-common
ARG SERVICE_NAME

# Copy Common source code
COPY src/Common/ src/Common/

# Build the shared libraries
RUN echo ">>> Building Common/Core..." && \
    dotnet build "src/Common/Core/Core.csproj" -c Release --property:WarningLevel=0

# Stage 3: Build and Publish Apps
FROM build-common AS publish
ARG SERVICE_NAME

# Copy all App source code
COPY src/Apps/ src/Apps/

# Build and publish only the service we actually use
WORKDIR /app
RUN echo ">>> Publishing ${SERVICE_NAME}..." && \
    dotnet publish "src/Apps/RestAPI/${SERVICE_NAME}/${SERVICE_NAME}.csproj" -c Release -o /app/publish /p:WarningLevel=0

# Stage 4: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
ARG SERVICE_NAME

# Runtime dependencies
RUN apt-get update -y \
    && apt-get install -y \
    tzdata ca-certificates dumb-init \
    libicu-dev krb5-user openssl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/* \
    && sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf \
    && printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf \
    && printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf \
    && printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf

# Copy startup script
COPY docker/scripts/startup.sh /startup.sh
RUN chmod 755 /startup.sh

# Copy appsettings for the service we actually use
RUN mkdir -p /appSettings && chown 1000:1000 /appSettings
COPY "src/Apps/RestAPI/${SERVICE_NAME}/appsettings.json" "/appSettings/${SERVICE_NAME}.dll.json"
RUN chown 1000:1000 /appSettings/*.json

# Set user permissions and working directory
RUN mkdir -p /app/log && chown -R 1000:1000 /app
USER 1000
WORKDIR /app/log

# Set environment variables for profiling
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
ENV DOTNET_gcServer=1
ENV DOTNET_GCDynamicAdaptationMode=1

# Final working directory and copy published files
WORKDIR /app
COPY --chown=1000:1000 --from=publish /app/publish .

# Define the entry point for the application
ENTRYPOINT ["../startup.sh"]
