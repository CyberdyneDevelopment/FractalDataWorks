using Fdw.VsCodeShell.Abstractions;
namespace Fdw.VsCodeShell;

/// <summary>Default record implementation of <see cref="IVsCodeCommandDescriptor"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record VsCodeCommandDescriptor(
    string Id,
    string Title,
    string? Category,
    string ContextKind) : IVsCodeCommandDescriptor;
