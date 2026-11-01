using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to get type symbol.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToGetTypeSymbol", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToGetTypeSymbolCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToGetTypeSymbolCode"/> class.
    /// </summary>
    public FailedToGetTypeSymbolCode()
        : base(91008, "FailedToGetTypeSymbol",
            ResultSeverities.ByName("Error"),
            "Failed to get type symbol",
            isRetryable: false)
    {
    }
}
