using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run's pipeline already has as many runs in flight as its limit allows.</summary>
[TypeOption(typeof(RunWaitReasons), "PipelineLimit")]
public sealed class PipelineLimitRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineLimitRunWaitReason"/> class.</summary>
    public PipelineLimitRunWaitReason() : base("PipelineLimit", "The run's pipeline already has as many runs in flight as its limit allows.")
    {
    }
}
