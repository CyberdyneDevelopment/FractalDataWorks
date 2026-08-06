using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Target folder is the same as the current folder — no-op move.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetSameAsCurrent", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetSameAsCurrentCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetSameAsCurrentCode"/> class.
    /// </summary>
    public TargetSameAsCurrentCode()
        : base(41001, "TargetSameAsCurrent",
            ResultSeverities.ByName("Error"),
            "Target folder is the same as current folder for project: {ProjectName}",
            isRetryable: false)
    {
    }
}
