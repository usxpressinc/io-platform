# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS restore
ARG GITHUB_TOKEN
ARG GITHUB_USER

WORKDIR /app

# Copy NuGet configuration and build props
COPY nuget.config .
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["io-platform.sln", "."]

# Copy ALL project files for restore
COPY ["src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj", "src/Common/Models/IO.Standard.Types/"]
COPY ["src/Common/Core/Core.csproj", "src/Common/Core/"]
COPY ["src/Common/Infrastructure/Infrastructure.csproj", "src/Common/Infrastructure/"]
COPY ["src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj", "src/Apps/RestAPI/IO.Proxy/"]
COPY ["src/Apps/RestAPI/IO.Common/IO.Common.csproj", "src/Apps/RestAPI/IO.Common/"]
COPY ["src/Apps/RestAPI/IO.Cass/IO.Cass.csproj", "src/Apps/RestAPI/IO.Cass/"]
COPY ["src/Apps/RestAPI/IO.Elsa/IO.Elsa.csproj", "src/Apps/RestAPI/IO.Elsa/"]
COPY ["src/Apps/RestAPI/IO.Larry/IO.Larry.csproj", "src/Apps/RestAPI/IO.Larry/"]
COPY ["src/Apps/RestAPI/IO.Lea/IO.Lea.csproj", "src/Apps/RestAPI/IO.Lea/"]
COPY ["src/Apps/Handlers/IO.Larry/VendorHandler/IO.Larry.VendorHandler.csproj", "src/Apps/Handlers/IO.Larry/VendorHandler/"]
COPY ["src/Apps/Handlers/IO.Lea/JobsHandler/IO.Lea.JobsHandler.csproj", "src/Apps/Handlers/IO.Lea/JobsHandler/"]
COPY ["src/Apps/Jobs/IO.Larry/VendorSync/IO.Larry.VendorSync.csproj", "src/Apps/Jobs/IO.Larry/VendorSync/"]
COPY ["src/Apps/Jobs/IO.Lea/GoogleJobs/IO.Lea.GoogleJobs.csproj", "src/Apps/Jobs/IO.Lea/GoogleJobs/"]

# Restore all dependencies in one command
ENV NUGET_XMLDOC_MODE=none
RUN echo ">>> Restoring NuGet packages..." && \
    dotnet restore io-platform.sln /p:WarningLevel=0

# Stage 2: Build Common/Shared projects
FROM restore AS build-common

# Copy style folder for StyleCop analyzers
COPY style/ style/

# Copy ONLY the Common source code
COPY src/Common/ src/Common/

# Build the shared libraries
RUN echo ">>> Building Common/Models..." && \
    dotnet build "src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj" -c Release --no-restore && \
    echo ">>> Building Common/Core..." && \
    dotnet build "src/Common/Core/Core.csproj" -c Release --no-restore && \
    echo ">>> Building Common/Infrastructure..." && \
    dotnet build "src/Common/Infrastructure/Infrastructure.csproj" -c Release --no-restore

# Stage 3: Build and Publish Apps
FROM build-common AS publish

# Copy all App source code
COPY src/Apps/ src/Apps/

# Build and publish all applications
WORKDIR /app
RUN echo ">>> Publishing IO.Proxy..." && \
    dotnet publish "src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Common..." && \
    dotnet publish "src/Apps/RestAPI/IO.Common/IO.Common.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Cass..." && \
    dotnet publish "src/Apps/RestAPI/IO.Cass/IO.Cass.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Elsa..." && \
    dotnet publish "src/Apps/RestAPI/IO.Elsa/IO.Elsa.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry..." && \
    dotnet publish "src/Apps/RestAPI/IO.Larry/IO.Larry.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea..." && \
    dotnet publish "src/Apps/RestAPI/IO.Lea/IO.Lea.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry.VendorHandler..." && \
    dotnet publish "src/Apps/Handlers/IO.Larry/VendorHandler/IO.Larry.VendorHandler.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea.JobsHandler..." && \
    dotnet publish "src/Apps/Handlers/IO.Lea/JobsHandler/IO.Lea.JobsHandler.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry.VendorSync..." && \
    dotnet publish "src/Apps/Jobs/IO.Larry/VendorSync/IO.Larry.VendorSync.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea.GoogleJobs..." && \
    dotnet publish "src/Apps/Jobs/IO.Lea/GoogleJobs/IO.Lea.GoogleJobs.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0

# Stage 4: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS final

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
COPY scripts/startup.sh /startup.sh
RUN chmod 755 /startup.sh

# Copy all appsettings for various entrypoints
RUN mkdir -p /appSettings && chown 1000:1000 /appSettings
COPY src/Apps/RestAPI/IO.Proxy/appsettings.json /appSettings/IO.Proxy.dll.json
COPY src/Apps/RestAPI/IO.Common/appsettings.json /appSettings/IO.Common.dll.json
COPY src/Apps/RestAPI/IO.Cass/appsettings.json /appSettings/IO.Cass.dll.json
COPY src/Apps/RestAPI/IO.Elsa/appsettings.json /appSettings/IO.Elsa.dll.json
COPY src/Apps/RestAPI/IO.Larry/appsettings.json /appSettings/IO.Larry.dll.json
COPY src/Apps/RestAPI/IO.Lea/appsettings.json /appSettings/IO.Lea.dll.json
COPY src/Apps/Handlers/IO.Larry/VendorHandler/appsettings.json /appSettings/IO.Larry.VendorHandler.dll.json
COPY src/Apps/Handlers/IO.Lea/JobsHandler/appsettings.json /appSettings/IO.Lea.JobsHandler.dll.json
COPY src/Apps/Jobs/IO.Larry/VendorSync/appsettings.json /appSettings/IO.Larry.VendorSync.dll.json
COPY src/Apps/Jobs/IO.Lea/GoogleJobs/appsettings.json /appSettings/IO.Lea.GoogleJobs.dll.json
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
