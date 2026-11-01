using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The provided symbol id is not a valid Roslyn DocumentationCommentId.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "InvalidSymbolId", RestrictToCurrentCompilation = true)]
public sealed class InvalidSymbolIdCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSymbolIdCode"/> class.
    /// </summary>
    public InvalidSymbolIdCode()
        : base(
            20001,
            "InvalidSymbolId",
            ResultSeverities.ByName("Error"),
            "Symbol id {symbolId} is not a valid Roslyn document/span identifier")
    {
    }
}
