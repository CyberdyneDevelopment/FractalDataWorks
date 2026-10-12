using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Symbol at position is not a field.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SymbolNotField", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SymbolNotFieldCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolNotFieldCode"/> class.
    /// </summary>
    public SymbolNotFieldCode()
        : base(21018, "SymbolNotField",
            ResultSeverities.ByName("Error"),
            "Symbol at position is not a field",
            isRetryable: false)
    {
    }
}
