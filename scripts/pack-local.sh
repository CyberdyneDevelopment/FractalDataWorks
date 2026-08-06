#!/usr/bin/env bash
# Pack Fdw packages to local NuGet folder.
# To also push to the GitLab feed, run ./push-gitlab.sh afterwards.
# Requires: LocalNugetFolder environment variable
# Usage: ./pack-local.sh [-n|--no-build] [-c|--configuration Release] [config-name]

set -e

NO_BUILD=false
CONFIGURATION="Release"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$SCRIPT_DIR/.."
CONFIG_NAME="Fdw.Local.nuget.config"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--no-build)
            NO_BUILD=true
            shift
            ;;
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        *)
            CONFIG_NAME="$1"
            shift
            ;;
    esac
done

# Validate environment
if [ -z "$LocalNugetFolder" ]; then
    echo "ERROR: LocalNugetFolder environment variable not set. Set it to your local NuGet folder path (e.g., ~/development/local-nuget)" >&2
    exit 1
fi

LOCAL_NUGET="$LocalNugetFolder"

# Why: delete any prior pack-errors file up front so its presence after a run
# unambiguously means "this run produced error/warning summary lines".
PACK_ERR_LOG="$ROOT_DIR/../last-pack-errors.txt"
rm -f "$PACK_ERR_LOG"

# Why: stale MSBuild / VBCSCompiler / Razor build-server workers from prior runs
# linger as zombie processes — observed 11 leftover MSBuild nodes consuming 70%+
# of CPU between pack invocations. Shut them down so this run starts with a clean
# pool instead of competing with stale workers.
echo "=== Shutting down stale build servers ==="
dotnet build-server shutdown 2>/dev/null || true

echo "=== Packing to Local NuGet ==="
echo "Target: $LOCAL_NUGET"

# Clean bin, obj, and .vs folders
find "$ROOT_DIR" -type d \( -name bin -o -name obj -o -name .vs \) -exec rm -rf {} + 2>/dev/null || true

# Ensure folder exists
mkdir -p "$LOCAL_NUGET"

# Read the version straight out of Directory.Build.props.
# Why: <VersionPrefix> is the single source of truth — the same property `dotnet pack` stamps.
# There is no tag inference and no MinVer, so detection and stamping cannot drift apart.
echo ""
echo "=== Detecting version ==="
VERSION_PREFIX=$(grep -oP '<VersionPrefix>\K[^<]+' "$ROOT_DIR/Directory.Build.props" 2>/dev/null | head -1)
VERSION_SUFFIX=$(grep -oP '<VersionSuffix>\K[^<]+' "$ROOT_DIR/Directory.Build.props" 2>/dev/null | head -1 | tr -d '[:space:]')
if [ -z "$VERSION_PREFIX" ] || [[ "$VERSION_PREFIX" != [0-9]* ]]; then
    echo "ERROR: Failed to read <VersionPrefix> from $ROOT_DIR/Directory.Build.props." >&2
    exit 1
fi
# Why: the package version is VersionPrefix + optional -VersionSuffix (1.0.0 + rc.1 => 1.0.0-rc.1),
# exactly what `dotnet pack` stamps. Compose both here so detection and stamping cannot drift.
if [ -n "$VERSION_SUFFIX" ]; then
    VERSION="${VERSION_PREFIX}-${VERSION_SUFFIX}"
else
    VERSION="${VERSION_PREFIX}"
fi
echo "Current version: $VERSION"

# Escape version string for use in glob/regex (dots are literal in globs)
ESCAPED_VERSION=$(printf '%s' "$VERSION" | sed 's/[.[\*^$()+?{|\\]/\\&/g')

# Delete old packages with this version from local folder
find "$LOCAL_NUGET" -maxdepth 1 -name "Fdw.*.$VERSION.nupkg" -delete 2>/dev/null || true
find "$LOCAL_NUGET" -maxdepth 1 -name "Fdw.*.$VERSION.snupkg" -delete 2>/dev/null || true

# Delete from NuGet cache
CACHE_ROOT="$HOME/.nuget/packages"
if [ -d "$CACHE_ROOT" ]; then
    for pkg_dir in "$CACHE_ROOT"/fdw.*; do
        if [ -d "$pkg_dir/$VERSION" ]; then
            rm -rf "$pkg_dir/$VERSION"
        fi
    done
fi

# Create config file in the local folder
cat > "$LOCAL_NUGET/$CONFIG_NAME" << 'NUGETEOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalFdw" value="%LocalNugetFolder%" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="LocalFdw">
      <!-- Why: every locally-produced ecosystem package prefix is routed to the
           local feed. Fdw.* also covers Fdw.Pidgin.*.
           Without the CyberdyneDevelopment.* entries, Mc3Po/DeveloperTools packages
           fall through to nuget.org and resolve stale/wrong versions on -Local builds. -->
      <package pattern="Fdw.*" />
      <package pattern="CyberdyneDevelopment.Mc3Po.*" />
      <package pattern="CyberdyneDevelopment.DeveloperTools.*" />
      <!-- Why: the reference implementations ship as ReferenceConnections.*,
           ReferenceSecretManagers.*, ReferenceNotifications.* and so on - no dot after
           "Reference" - so the older "Reference.*" pattern never matched any of them and
           they fell through to nuget.org, where they do not exist (NU1101). -->
      <package pattern="Reference*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
NUGETEOF

# Generate Tailwind safelist before build
echo ""
echo "=== Generating Tailwind safelist ==="
"$SCRIPT_DIR/generate-tailwind-safelist.sh"

# Why: pack-local skips test projects to keep dev iteration fast. CI/CD builds the
# full slnx (tests included) via its own pipeline — the slnx itself isn't changed.
# We collect every csproj under public/src and build them in one msbuild invocation
# so the dependency graph is evaluated once.
echo ""
SRC_PROJECTS=()
while IFS= read -r f; do SRC_PROJECTS+=("$f"); done < <(find "$ROOT_DIR/src" -name "*.csproj" -not -path "*/obj/*" -not -path "*/bin/*" | sort)

if [ ${#SRC_PROJECTS[@]} -eq 0 ]; then
    echo "ERROR: No src csproj files found under $ROOT_DIR/src" >&2
    exit 1
fi

if [ "$NO_BUILD" = true ]; then
    # Why: single slnx-level pack instead of a per-project loop. The root
    # Directory.Build.props gates IsPackable=true only for src/* projects, so tests,
    # samples, vsix, and reference apps are skipped automatically. One MSBuild
    # bootstrap instead of one-per-project — drops the pack phase from minutes to seconds.
    MAIN_SLN="$ROOT_DIR/Fdw.DeveloperKit.slnx"
    echo "=== Packing solution ($CONFIGURATION) [no-build, $MAIN_SLN] ==="
    if ! dotnet pack "$MAIN_SLN" -c "$CONFIGURATION" -o "$LOCAL_NUGET" --no-build --nologo -v q; then
        echo "ERROR: Pack failed" >&2
        exit 1
    fi
else
    # Why: build the main DeveloperKit slnx directly. Skips the per-pack temp-slnx machinery
    # (~1 min of serial `dotnet sln add` per project) at the cost of also building test
    # projects in the build phase. On this VM the trade-off is favorable.
    MAIN_SLN="$ROOT_DIR/Fdw.DeveloperKit.slnx"
    if [ ! -f "$MAIN_SLN" ]; then
        echo "ERROR: Main solution not found at $MAIN_SLN" >&2
        exit 1
    fi

    echo "=== Building solution ($CONFIGURATION) [$MAIN_SLN] ==="
    # Why: MSBuild file logger writes errors+warnings (with file:line detail) to disk
    # natively. Use a temp path so last-pack-errors.txt is only present if anything
    # was logged. set +e around the call so we can inspect rc and tidy up.
    PACK_TMP_OUT=$(mktemp)
    set +e
    # Why: -m:1 (serial) avoids a non-deterministic Razor/StaticWebAssets codegen race in the
    # parallel build — the Blazor projects (UI.Blazor.Authentication, Etl.Projects.UI) intermittently
    # fail CS0103/missing-project-reference when their generators run before dependencies are built.
    # Each builds clean alone; only the parallel full-solution pack races. Serial is reliable.
    DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
        dotnet build "$MAIN_SLN" -c "$CONFIGURATION" --nologo -m:1 \
        -fl "-flp:logfile=$PACK_TMP_OUT;errorsonly;warningsonly;verbosity=normal"
    BUILD_RC=$?
    set -e
    if [ -s "$PACK_TMP_OUT" ]; then
        mv "$PACK_TMP_OUT" "$PACK_ERR_LOG"
        echo "  wrote error/warning detail to $PACK_ERR_LOG" >&2
    else
        rm -f "$PACK_TMP_OUT"
    fi
    if [ "$BUILD_RC" -ne 0 ]; then
        echo "ERROR: Build failed (see $PACK_ERR_LOG if present)" >&2
        exit 1
    fi

    # Why: single slnx-level pack instead of a per-project loop. The root
    # Directory.Build.props gates IsPackable=true only for src/* projects, so tests,
    # samples, vsix, and reference apps are skipped automatically. One MSBuild
    # bootstrap instead of one-per-project — drops the pack phase from minutes to seconds.
    echo "=== Packing solution ($CONFIGURATION) [no-build, $MAIN_SLN] ==="
    if ! dotnet pack "$MAIN_SLN" -c "$CONFIGURATION" -o "$LOCAL_NUGET" --no-build --nologo -v q; then
        echo "ERROR: Pack failed" >&2
        exit 1
    fi
fi

# Generate Directory.Packages.props with all Fdw packages
PACKAGES=()
while IFS= read -r pkg; do
    NAME=$(basename "$pkg" .nupkg | sed "s/\\.${ESCAPED_VERSION}$//")
    PACKAGES+=("$NAME")
done < <(find "$LOCAL_NUGET" -maxdepth 1 -name "Fdw.*.$VERSION.nupkg" | sort)

PROPS_PATH="$LOCAL_NUGET/Directory.Packages.props"
{
    echo "<Project>"
    echo "  <!--"
    echo "    Auto-generated by pack-local.sh"
    echo "    Contains Fdw package versions for local development."
    echo "    Consumer projects import this when using -Local configurations."
    echo "  -->"
    echo "  <ItemGroup>"
    for NAME in "${PACKAGES[@]}"; do
        echo "    <PackageVersion Include=\"$NAME\" Version=\"$VERSION\" />"
    done
    echo "  </ItemGroup>"
    echo "</Project>"
} > "$PROPS_PATH"

# Why: consumer projects' Directory.Packages.props imports FdwVersion.props from
# LocalNugetFolder when Configuration ends with -Local. That file pins the FdwVersion
# property — if it goes stale, every Debug-Local build resolves to an older pack
# even when local-nuget has newer nupkgs. Always rewrite it to match the current pack.
FDW_VERSION_PROPS="$LOCAL_NUGET/FdwVersion.props"
echo "<Project><PropertyGroup><FdwVersion>$VERSION</FdwVersion></PropertyGroup></Project>" > "$FDW_VERSION_PROPS"

# Update versions.json
VERSIONS_PATH="$LOCAL_NUGET/versions.json"
if [ -f "$VERSIONS_PATH" ] && command -v python3 &>/dev/null; then
    python3 -c "
import json
with open('$VERSIONS_PATH', 'r') as f:
    data = json.load(f)
data['Fdw'] = '$VERSION'
with open('$VERSIONS_PATH', 'w') as f:
    json.dump(data, f, indent=2)
"
else
    echo "{\"Fdw\": \"$VERSION\"}" > "$VERSIONS_PATH"
fi

echo ""
echo "=== Success ==="
echo "${#PACKAGES[@]} packages (v$VERSION) published to: $LOCAL_NUGET"
echo "Run ./push-gitlab.sh to also publish to the GitLab feed."
