using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No symbol found at the specified position.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoSymbolFoundAtPosition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSymbolFoundAtPositionCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSymbolFoundAtPositionCode"/> class.
    /// </summary>
    public NoSymbolFoundAtPositionCode()
        : base(31012, "NoSymbolFoundAtPosition",
            ResultSeverities.ByName("Error"),
            "No symbol found at the specified position",
            isRetryable: false)
    {
    }
}
