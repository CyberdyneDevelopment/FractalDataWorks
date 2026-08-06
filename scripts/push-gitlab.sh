#!/usr/bin/env bash
# Push Fdw .nupkg files from the local NuGet folder to the GitLab feed.
# Run this after pack-local.sh when you want the packages on GitLab too.
#
# Requires:
#   - LocalNugetFolder environment variable (same as pack-local.sh)
#   - ~/.tokens/gitlab-token containing a GitLab personal access token with api scope
#
# Usage: ./push-gitlab.sh [version]
#   version: optional, defaults to <VersionPrefix> in Directory.Build.props

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$SCRIPT_DIR/.."
GITLAB_NUGET_URL="http://10.10.10.100/api/v4/projects/1/packages/nuget/index.json"

if [ -z "$LocalNugetFolder" ]; then
    echo "ERROR: LocalNugetFolder environment variable not set." >&2
    exit 1
fi

LOCAL_NUGET="$LocalNugetFolder"

# Resolve version: explicit arg wins, else read <VersionPrefix> from Directory.Build.props.
if [ -n "$1" ]; then
    VERSION="$1"
else
    VERSION=$(grep -oP '<VersionPrefix>\K[^<]+' "$ROOT_DIR/Directory.Build.props" 2>/dev/null | head -1)
    if [ -z "$VERSION" ] || [[ "$VERSION" != [0-9]* ]]; then
        echo "ERROR: Failed to read <VersionPrefix> from $ROOT_DIR/Directory.Build.props. Pass the version as the first argument." >&2
        exit 1
    fi
fi

GITLAB_TOKEN_FILE="$HOME/.tokens/gitlab-token"
if [ ! -f "$GITLAB_TOKEN_FILE" ]; then
    echo "ERROR: $GITLAB_TOKEN_FILE not found." >&2
    exit 1
fi

echo "=== Pushing Fdw.* v$VERSION to GitLab NuGet feed ==="
echo "Source: $LOCAL_NUGET"
echo "Target: $GITLAB_NUGET_URL"

PUSHED=0
FAILED=0
SHOPT_RESTORE=$(shopt -p nullglob || true)
shopt -s nullglob
for pkg in "$LOCAL_NUGET"/Fdw.*."$VERSION".nupkg; do
    if dotnet nuget push "$pkg" --source "$GITLAB_NUGET_URL" --api-key "$(cat "$GITLAB_TOKEN_FILE")" --skip-duplicate --allow-insecure-connections 2>&1; then
        PUSHED=$((PUSHED + 1))
    else
        echo "WARNING: Failed to push $(basename "$pkg")" >&2
        FAILED=$((FAILED + 1))
    fi
done
eval "$SHOPT_RESTORE"

echo "Pushed $PUSHED packages to GitLab ($FAILED failed)"
[ "$FAILED" -eq 0 ]
