using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// When a Pipeline in a Step fails, continue executing remaining pipelines in the Stage.
/// The Stage itself records failure at completion but does not short-circuit sibling pipelines.
/// This is the less strict option (ContinueStage &lt; HaltStage).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StepFailurePolicies), "ContinueStage")]
public sealed class ContinueStagePolicy : StepFailurePolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="ContinueStagePolicy"/> class.</summary>
    public ContinueStagePolicy() : base(2, "ContinueStage")
    {
    }
}
