using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Symbol at position is not a method.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SymbolNotMethod", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SymbolNotMethodCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolNotMethodCode"/> class.
    /// </summary>
    public SymbolNotMethodCode()
        : base(21020, "SymbolNotMethod",
            ResultSeverities.ByName("Error"),
            "Symbol at position is not a method",
            isRetryable: false)
    {
    }
}
