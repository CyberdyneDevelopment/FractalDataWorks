using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Load stage requires configuration.
/// </summary>
[TypeOption(typeof(PipelineResultCodes), "LoadStageRequiresConfiguration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LoadStageRequiresConfigurationCode : PipelineResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadStageRequiresConfigurationCode"/> class.
    /// </summary>
    public LoadStageRequiresConfigurationCode()
        : base(61000, "LoadStageRequiresConfiguration",
            ResultSeverities.ByName("Error"),
            "Load stage requires configuration",
            isRetryable: false)
    {
    }
}
