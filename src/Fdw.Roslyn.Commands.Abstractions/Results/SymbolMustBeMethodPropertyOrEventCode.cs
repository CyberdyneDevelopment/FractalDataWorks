using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Symbol at position must be a method, property, or event.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SymbolMustBeMethodPropertyOrEvent", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SymbolMustBeMethodPropertyOrEventCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolMustBeMethodPropertyOrEventCode"/> class.
    /// </summary>
    public SymbolMustBeMethodPropertyOrEventCode()
        : base(21017, "SymbolMustBeMethodPropertyOrEvent",
            ResultSeverities.ByName("Error"),
            "Symbol at position must be a method, property, or event",
            isRetryable: false)
    {
    }
}
