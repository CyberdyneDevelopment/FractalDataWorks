using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The row writes the records the pipeline's stages rejected, after the load, with the reason and the stage that rejected each.</summary>
[TypeOption(typeof(LoadPhases), "Reject")]
public sealed class RejectLoadPhase : LoadPhaseBase
{
    /// <summary>The column that receives the reason a stage rejected the record.</summary>
    public const string ReasonColumn = "RejectReason";

    /// <summary>The column that receives the name of the stage that rejected the record.</summary>
    public const string StageColumn = "RejectStage";

    /// <summary>Initializes a new instance of the <see cref="RejectLoadPhase"/> class.</summary>
    public RejectLoadPhase() : base("Reject", "The row writes the records the pipeline's stages rejected, after the load, with the reason and the stage that rejected each.", readsRejectedRecords: true)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> ReservedColumns { get; } = [ReasonColumn, StageColumn];
}
