using Fdw.Conventions;
using Fdw.VsCodeShell.Abstractions;
namespace Fdw.VsCodeShell;

/// <summary>
/// Editor state the bootstrap captures from VS Code at command-invocation time and POSTs
/// to the host as the command body. Fields are populated according to the command's
/// <see cref="IVsCodeCommandDescriptor.ContextKind"/>; unused fields are <c>null</c>.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record EditorContext(
    string? DocumentUri,
    string? LanguageId,
    int? CursorLine,
    int? CursorCharacter,
    string? SelectionText,
    string? WordUnderCursor)
{
    /// <summary>The context of a command invoked with no editor state: every field <c>null</c>, as when the bootstrap posts no body.</summary>
    [PlatformDefault]
    public static EditorContext None { get; } = new(null, null, null, null, null, null);
}
