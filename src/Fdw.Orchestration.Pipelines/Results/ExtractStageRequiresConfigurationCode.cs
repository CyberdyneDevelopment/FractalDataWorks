using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Extract stage requires configuration.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "ExtractStageRequiresConfiguration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExtractStageRequiresConfigurationCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractStageRequiresConfigurationCode"/> class.
    /// </summary>
    public ExtractStageRequiresConfigurationCode()
        : base(60001, "ExtractStageRequiresConfiguration",
            ResultSeverities.ByName("Error"),
            "Extract stage requires configuration",
            isRetryable: false)
    {
    }
}
