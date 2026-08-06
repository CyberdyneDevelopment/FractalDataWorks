using Fdw.VsCodeShell.Abstractions;
namespace Fdw.VsCodeShell;

/// <summary>
/// Editor state the bootstrap captures from VS Code at command-invocation time and POSTs
/// to the host as the command body. Fields are populated according to the command's
/// <see cref="IVsCodeCommandDescriptor.ContextKind"/>; unused fields are <c>null</c>.
/// </summary>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record EditorContext(
    string? DocumentUri,
    string? LanguageId,
    int? CursorLine,
    int? CursorCharacter,
    string? SelectionText,
    string? WordUnderCursor);
