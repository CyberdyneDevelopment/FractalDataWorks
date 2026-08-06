#!/usr/bin/env bash
# Pack the Source Generator template using Nerdbank.GitVersioning.
# Usage: ./pack-source-generator-template.sh [output-path]
# Default output: ./nupkg

set -e

OUTPUT_PATH="${1:-./nupkg}"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SOLUTION_ROOT="$SCRIPT_DIR/.."

# Ensure dotnet tools are discoverable
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.dotnet/tools:$PATH"

echo "Packing Source Generator Template..."

# Check if nbgv is installed
if ! command -v nbgv &> /dev/null; then
    echo "Installing Nerdbank.GitVersioning tool..."
    dotnet tool install --global nbgv
fi

# Get version from git (from solution root)
pushd "$SOLUTION_ROOT" > /dev/null
VERSION=$(nbgv get-version -v NuGetPackageVersion)
echo "Version: $VERSION (from solution root)"
popd > /dev/null

# Ensure output directory exists
mkdir -p "$OUTPUT_PATH"

# Pack using nuget
TEMPLATE_ROOT="$SCRIPT_DIR/SourceGeneratorSolution"
NUSPEC_PATH="$TEMPLATE_ROOT/.template.config/template.nuspec"
echo "Packing from: $NUSPEC_PATH"

nuget pack "$NUSPEC_PATH" \
    -OutputDirectory "$OUTPUT_PATH" \
    -Version "$VERSION" \
    -BasePath "$TEMPLATE_ROOT" \
    -NoPackageAnalysis

if [ $? -eq 0 ]; then
    PACKAGE_PATH="$OUTPUT_PATH/Fdw.Templates.SourceGenerator.$VERSION.nupkg"
    echo ""
    echo "Template packed successfully!"
    echo "Package: $PACKAGE_PATH"
    echo ""
    echo "To install:"
    echo "  dotnet new install $PACKAGE_PATH"
    echo ""
    echo "Or install from directory:"
    echo "  dotnet new install $TEMPLATE_ROOT"
else
    echo "ERROR: Failed to pack template"
    exit 1
fi
