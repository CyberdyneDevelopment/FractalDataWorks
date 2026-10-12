using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No containing type found at position.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoContainingTypeFoundAtPosition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoContainingTypeFoundAtPositionCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoContainingTypeFoundAtPositionCode"/> class.
    /// </summary>
    public NoContainingTypeFoundAtPositionCode()
        : base(31002, "NoContainingTypeFoundAtPosition",
            ResultSeverities.ByName("Error"),
            "No containing type found at position",
            isRetryable: false)
    {
    }
}
