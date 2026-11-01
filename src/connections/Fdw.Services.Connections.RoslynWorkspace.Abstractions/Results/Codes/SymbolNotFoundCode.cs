using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The requested symbol was not found in the workspace.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "SymbolNotFound", RestrictToCurrentCompilation = true)]
public sealed class SymbolNotFoundCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolNotFoundCode"/> class.
    /// </summary>
    public SymbolNotFoundCode()
        : base(
            31000,
            "SymbolNotFound",
            ResultSeverities.ByName("Error"),
            "Symbol {symbolId} not found in workspace {connection}")
    {
    }
}
