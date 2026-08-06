#!/usr/bin/env bash
# Install Fdw Item Templates
# Run this script from the templates directory

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Installing Fdw Item Templates..."

# Install dotnet template
echo ""
echo "Installing MessageLogger template for dotnet CLI..."
dotnet new install "$SCRIPT_DIR/MessageLogger"

echo ""
echo "Template installation complete!"
echo ""
echo "Usage:"
echo "  dotnet new fdw-logger --help"
echo "  dotnet new fdw-logger --loggerName MyServiceLogger"
echo "  dotnet new fdw-logger --namespace MyCompany.Services --loggerName ApiLogger"

# Verify installation
echo ""
echo "Verifying installation..."
dotnet new list | grep -i "fdw-logger" || echo "WARNING: fdw-logger template not found in list"
