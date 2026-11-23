using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The cap of a pipeline: how many of its runs may be claimed at once.</summary>
[TypeOption(typeof(ClaimKeyTypes), "PipelineCap")]
public sealed class PipelineCapClaimKeyType : ClaimKeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineCapClaimKeyType"/> class.</summary>
    public PipelineCapClaimKeyType()
        : base("PipelineCap", "The cap of a pipeline: how many of its runs may be claimed at once.", holdsRow: true, statesCap: true)
    {
    }
}
