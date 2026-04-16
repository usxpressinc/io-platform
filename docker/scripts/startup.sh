#!/bin/bash

# Get the entrypoint from environment variable or use SERVICE_NAME if set, otherwise default to IO.Proxy
if [ -n "$APPLICATION_ENTRYPOINT" ]; then
    ENTRYPOINT=$APPLICATION_ENTRYPOINT
elif [ -n "$SERVICE_NAME" ]; then
    ENTRYPOINT=${SERVICE_NAME}.dll
else
    ENTRYPOINT=IO.Proxy.dll
fi

echo "Starting application: $ENTRYPOINT"

# Start the application
exec dotnet "$ENTRYPOINT"
