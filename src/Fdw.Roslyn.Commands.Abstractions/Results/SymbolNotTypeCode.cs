using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Symbol at position is not a type.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SymbolNotType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SymbolNotTypeCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolNotTypeCode"/> class.
    /// </summary>
    public SymbolNotTypeCode()
        : base(21021, "SymbolNotType",
            ResultSeverities.ByName("Error"),
            "Symbol at position is not a type",
            isRetryable: false)
    {
    }
}
