using System.Collections.Generic;

namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// The capability manifest a VS Code shell host publishes at <c>GET /vscode/manifest</c>.
/// The bootstrap reads this once at startup and registers each command and webview with VS Code.
/// </summary>
public interface IVsCodeManifest
{
    /// <summary>Extension identifier (publisher.name) the bootstrap stamps into the staged package.json.</summary>
    string ExtensionId { get; }

    /// <summary>Human-readable name shown in the VS Code extensions surface.</summary>
    string DisplayName { get; }

    /// <summary>Commands the bootstrap registers with VS Code.</summary>
    IReadOnlyList<IVsCodeCommandDescriptor> Commands { get; }

    /// <summary>Webview panels the bootstrap opens when their <c>OpenCommandId</c> fires.</summary>
    IReadOnlyList<IVsCodeWebviewDescriptor> Webviews { get; }
}
