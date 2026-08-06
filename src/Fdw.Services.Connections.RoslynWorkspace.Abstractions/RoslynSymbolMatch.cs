namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Identifier-shaped description of a Roslyn symbol — the wire form for cross-boundary symbol
/// references. Domain-blind: no Microsoft.CodeAnalysis types leak through.
/// </summary>
/// <param name="DocumentationCommentId">Roslyn DocumentationCommentId (e.g. <c>M:Foo.Bar.Baz(System.Int32)</c>). Addressable across calls.</param>
/// <param name="DisplayName">Minimally qualified display label suitable for a UI/canvas.</param>
/// <param name="Kind">Symbol kind in lowercase invariant form: <c>method</c>, <c>class</c>, <c>interface</c>, <c>property</c>, <c>field</c>, <c>event</c>, <c>namespace</c>, …</param>
/// <param name="FilePath">Source file path when the symbol has an in-source location; <c>null</c> for metadata-only symbols.</param>
/// <param name="Line">1-based line number when <see cref="FilePath"/> is non-null.</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record RoslynSymbolMatch(
    string DocumentationCommentId,
    string DisplayName,
    string Kind,
    string? FilePath,
    int? Line);
