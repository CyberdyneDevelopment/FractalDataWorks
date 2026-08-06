namespace Fdw.Data.Abstractions;

/// <summary>
/// Raw text content produced by a text-mode source connector.
/// </summary>
/// <param name="Text">The full text content.</param>
/// <param name="Lines">
/// Optional line range applied before returning the text.
/// When present the <see cref="Text"/> value contains only the requested lines.
/// </param>
/// <remarks>
/// <para>
/// <see cref="RawText"/> is the canonical output type for text-mode workspace clients such as
/// <c>RoslynWorkspaceClient</c> and <c>SnapshotRoslynWorkspaceClient</c>.
/// </para>
/// <para>
/// Consumers that need structured types (e.g. parsed JSON) compose a mapper on top of
/// a text result rather than creating a separate client for each output format.
/// </para>
/// </remarks>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record RawText(string Text, RawTextLineRange? Lines = null);
