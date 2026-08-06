using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// When a Pipeline in a Step fails, halt the entire Stage immediately.
/// All sibling pipelines in the same step are cancelled.
/// This is the stricter option (HaltStage &gt; ContinueStage).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StepFailurePolicies), "HaltStage")]
public sealed class HaltStagePolicy : StepFailurePolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="HaltStagePolicy"/> class.</summary>
    public HaltStagePolicy() : base(1, "HaltStage")
    {
    }
}
