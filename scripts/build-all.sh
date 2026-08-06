#!/usr/bin/env bash
# Build and pack Fdw framework
#
# Usage: ./build-all.sh [-c|--configuration Debug|Release]
# Requires: LocalNugetFolder environment variable

set -e

CONFIGURATION="Debug"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$SCRIPT_DIR/.."

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        *)
            shift
            ;;
    esac
done

# Ensure dotnet tools and runtime are discoverable
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.dotnet/tools:$PATH"

# Validate environment
if [ -z "$LocalNugetFolder" ]; then
    echo "ERROR: LocalNugetFolder environment variable not set." >&2
    echo "Run: source scripts/setup-local-nuget.sh" >&2
    exit 1
fi

mkdir -p "$LocalNugetFolder"

echo "============================================"
echo "  Fdw - Build All"
echo "  Configuration: $CONFIGURATION"
echo "  LocalNugetFolder: $LocalNugetFolder"
echo "============================================"
echo ""

# =============================================
# Phase 1: Pack FDW Framework (builds only packable src/ projects, skips tests)
# =============================================
echo "=== Phase 1: Pack FDW Framework ==="

# Detect version via nbgv (preferred)
VERSION=$(nbgv get-version -v NuGetPackageVersion 2>/dev/null || true)
if [ -z "$VERSION" ] || [[ "$VERSION" != [0-9]* ]]; then
    VERSION=""
    echo "nbgv not available - will detect version from packed packages"
fi

# Clean old FDW packages from local folder
find "$LocalNugetFolder" -maxdepth 1 -name "Fdw.*.nupkg" -delete 2>/dev/null || true
find "$LocalNugetFolder" -maxdepth 1 -name "Fdw.*.snupkg" -delete 2>/dev/null || true

# Clean NuGet cache for FDW packages
CACHE_ROOT="$HOME/.nuget/packages"
if [ -d "$CACHE_ROOT" ]; then
    for pkg_dir in "$CACHE_ROOT"/fractaldataworks.*; do
        [ -d "$pkg_dir" ] && rm -rf "$pkg_dir"
    done
fi

dotnet pack "$ROOT_DIR/Fdw.DeveloperKit.slnx" -c "$CONFIGURATION" -o "$LocalNugetFolder"
if [ $? -ne 0 ]; then
    echo "ERROR: FDW pack failed" >&2
    exit 1
fi

# Detect version from packed package filename (most reliable when nbgv unavailable)
if [ -z "$VERSION" ]; then
    ACTUAL_PKG=$(ls "$LocalNugetFolder"/Fdw.Abstractions.*.nupkg 2>/dev/null | head -1)
    if [ -n "$ACTUAL_PKG" ]; then
        VERSION=$(basename "$ACTUAL_PKG" .nupkg | sed 's/^Fdw\.Abstractions\.//')
    fi
fi
if [ -z "$VERSION" ]; then
    echo "ERROR: Could not detect FDW version from packed packages" >&2
    exit 1
fi
echo "FDW Version: $VERSION"
echo "FDW packages packed to $LocalNugetFolder"

# =============================================
# Summary
# =============================================
echo ""
echo "============================================"
echo "  BUILD ALL: SUCCESS"
echo "  FDW Version: $VERSION"
echo "  Configuration: $CONFIGURATION"
echo ""
echo "  Reference Solutions have moved to separate repos:"
echo "    - reference-api"
echo "    - reference-etl"
echo "    - reference-scheduler"
echo "    - reference-ui"
echo "============================================"
