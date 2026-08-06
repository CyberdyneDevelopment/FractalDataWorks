using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Could not find containing type.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CouldNotFindContainingType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CouldNotFindContainingTypeCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CouldNotFindContainingTypeCode"/> class.
    /// </summary>
    public CouldNotFindContainingTypeCode()
        : base(30000, "CouldNotFindContainingType",
            ResultSeverities.ByName("Error"),
            "Could not find containing type",
            isRetryable: false)
    {
    }
}
