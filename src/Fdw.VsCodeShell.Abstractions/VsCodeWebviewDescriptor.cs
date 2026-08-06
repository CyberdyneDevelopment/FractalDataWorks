using Fdw.VsCodeShell.Abstractions;
namespace Fdw.VsCodeShell;

/// <summary>Default record implementation of <see cref="IVsCodeWebviewDescriptor"/>.</summary>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record VsCodeWebviewDescriptor(
    string ViewType,
    string Title,
    string OpenCommandId,
    string Path,
    bool RetainContextWhenHidden) : IVsCodeWebviewDescriptor;
