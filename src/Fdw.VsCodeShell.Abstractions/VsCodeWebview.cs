namespace Fdw.VsCodeShell;

/// <summary>
/// A webview panel owned by a single command. The bootstrap opens it when that command fires.
/// </summary>
/// <remarks>
/// Deliberately carries no <c>OpenCommandId</c>: the owning command is the
/// <see cref="VsCodeCommandTypeBase{THandler}"/> that declares this webview, so the relationship is
/// structural rather than a string join between two parallel manifest arrays. The wire-format
/// <see cref="VsCodeWebviewDescriptor"/> still carries <c>OpenCommandId</c> — it is projected from the
/// owning command when the manifest is serialized.
/// </remarks>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record VsCodeWebview(
    string ViewType,
    string Title,
    string Path,
    bool RetainContextWhenHidden = true);
