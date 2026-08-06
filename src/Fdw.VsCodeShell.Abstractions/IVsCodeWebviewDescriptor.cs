namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// Declares a webview panel the bootstrap should create when its <see cref="OpenCommandId"/>
/// fires. The webview iframes <c>{hostBaseUrl}{Path}</c> served by the .NET host.
/// </summary>
public interface IVsCodeWebviewDescriptor
{
    /// <summary>Webview view type id (e.g. <c>pidginCanvas</c>).</summary>
    string ViewType { get; }

    /// <summary>Tab title shown in VS Code.</summary>
    string Title { get; }

    /// <summary>Command id that opens this webview when invoked.</summary>
    string OpenCommandId { get; }

    /// <summary>Relative URL path on the host the iframe loads.</summary>
    string Path { get; }

    /// <summary>If true, keep the panel state when hidden.</summary>
    bool RetainContextWhenHidden { get; }
}
