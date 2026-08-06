using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The pruned repair plan named by ApplyFromPath does not exist.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "RepairPlanNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RepairPlanNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepairPlanNotFoundCode"/> class.
    /// </summary>
    public RepairPlanNotFoundCode()
        : base(31027, "RepairPlanNotFound",
            ResultSeverities.ByName("Error"),
            "Repair plan not found: {PlanPath}; run a preview with PreviewPath first",
            isRetryable: false)
    {
    }
}
