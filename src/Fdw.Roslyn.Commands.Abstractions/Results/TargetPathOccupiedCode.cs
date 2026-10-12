using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The path implied by the namespace is already occupied by another document.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetPathOccupied", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetPathOccupiedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetPathOccupiedCode"/> class.
    /// </summary>
    public TargetPathOccupiedCode()
        : base(31020, "TargetPathOccupied",
            ResultSeverities.ByName("Error"),
            "Target path already occupied: {TargetPath}",
            isRetryable: false)
    {
    }
}
