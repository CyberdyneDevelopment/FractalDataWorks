#!/usr/bin/env bash
# Uninstall Fdw Item Templates

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Uninstalling Fdw Item Templates..."

# Uninstall dotnet template
echo ""
echo "Uninstalling MessageLogger template..."
dotnet new uninstall "$SCRIPT_DIR/MessageLogger"

echo ""
echo "Template uninstallation complete!"
