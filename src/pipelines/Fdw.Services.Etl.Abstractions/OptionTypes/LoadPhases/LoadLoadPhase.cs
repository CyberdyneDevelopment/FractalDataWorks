using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The row writes the pipeline's transformed output rows: the pipeline's own load, or one of its load steps.</summary>
[TypeOption(typeof(LoadPhases), "Load")]
public sealed class LoadLoadPhase : LoadPhaseBase
{
    /// <summary>Initializes a new instance of the <see cref="LoadLoadPhase"/> class.</summary>
    public LoadLoadPhase() : base("Load", "The row writes the pipeline's transformed output rows: the pipeline's own load, or one of its load steps.", readsRejectedRecords: false)
    {
    }
}
