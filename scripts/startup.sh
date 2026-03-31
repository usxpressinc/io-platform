#!/bin/bash

# Get the entrypoint from environment variable or default to IO.Proxy
ENTRYPOINT=${APPLICATION_ENTRYPOINT:-IO.Proxy.dll}

echo "Starting application: $ENTRYPOINT"

# Start the application
exec dotnet "$ENTRYPOINT"
