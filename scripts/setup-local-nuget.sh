#!/usr/bin/env bash
# Setup local NuGet folder for Fdw development
# Usage: ./setup-local-nuget.sh [path] [config-name]
# Defaults: ~/development/local-nuget Fdw.Local.nuget.config
#
# If LocalNugetFolder environment variable exists, uses that folder automatically.
# Otherwise uses the provided/default path.

set -e

# Use existing LocalNugetFolder if set, otherwise use default
DEFAULT_PATH="${LocalNugetFolder:-$HOME/development/local-nuget}"
TARGET_PATH="${1:-$DEFAULT_PATH}"
CONFIG_NAME="${2:-Fdw.Local.nuget.config}"

# Show if we're using existing env var
if [ -n "$LocalNugetFolder" ] && [ -z "$1" ]; then
    echo "Using existing LocalNugetFolder: $LocalNugetFolder"
fi

echo "=== Setting up Local NuGet Folder ==="

# Check if environment variable already exists
if [ -n "$LocalNugetFolder" ]; then
    TARGET_PATH="$LocalNugetFolder"
    echo "✓ Using existing LocalNugetFolder: $TARGET_PATH"
else
    echo "LocalNugetFolder not set, using: $TARGET_PATH"
fi

echo ""

# Create folder if it doesn't exist
if [ ! -d "$TARGET_PATH" ]; then
    echo ""
    echo "Creating folder: $TARGET_PATH"
    mkdir -p "$TARGET_PATH"
    echo "  ✓ Folder created"
else
    echo ""
    echo "Folder already exists: $TARGET_PATH"
fi

# Create config file
CONFIG_PATH="$TARGET_PATH/$CONFIG_NAME"
cat > "$CONFIG_PATH" << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalFdw" value="%LocalNugetFolder%" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="LocalFdw">
      <package pattern="Fdw.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

echo ""
echo "✓ Created: $CONFIG_PATH"
echo "   Config name: $CONFIG_NAME"

# Environment variable setup
echo ""
echo "=== Environment Variable ==="
if [ "$LocalNugetFolder" = "$TARGET_PATH" ]; then
    echo "✓ LocalNugetFolder is already set correctly: $LocalNugetFolder"
else
    echo "Setting LocalNugetFolder environment variable..."

    # Set for current session
    export LocalNugetFolder="$TARGET_PATH"
    echo "  ✓ Current session: $TARGET_PATH"

    # Determine shell config file
    if [ -n "$ZSH_VERSION" ]; then
        SHELL_RC="$HOME/.zshrc"
    elif [ -n "$BASH_VERSION" ]; then
        SHELL_RC="$HOME/.bashrc"
    else
        SHELL_RC="$HOME/.profile"
    fi

    # Add to shell config if not already present
    if ! grep -q "export LocalNugetFolder=" "$SHELL_RC" 2>/dev/null; then
        echo "export LocalNugetFolder=\"$TARGET_PATH\"" >> "$SHELL_RC"
        echo "  ✓ Added to $SHELL_RC (persistent)"
    else
        echo "  ✓ Already in $SHELL_RC (persistent)"
    fi

    echo ""
    echo "Note: Other open terminals need to be restarted to pick up the persistent setting."
fi

echo ""
echo "=== Next Steps ==="
STEP=1
if [ "$CONFIG_NAME" != "Fdw.Local.nuget.config" ]; then
    echo "$STEP. Set LocalNugetConfigFileName in your project:"
    echo "   <LocalNugetConfigFileName>$CONFIG_NAME</LocalNugetConfigFileName>"
    STEP=$((STEP + 1))
    echo "$STEP. Run: ./scripts/pack-local.sh \"$CONFIG_NAME\""
else
    echo "$STEP. Run: ./scripts/pack-local.sh"
fi
STEP=$((STEP + 1))
echo "$STEP. Build with a -Local configuration (e.g., Debug-Local, Develop-Local) to use local packages"
echo ""
echo "Setup complete! LocalNugetFolder is ready to use."
