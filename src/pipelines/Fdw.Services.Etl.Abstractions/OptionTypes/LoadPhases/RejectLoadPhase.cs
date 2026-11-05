using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The row writes the records the pipeline's components rejected, after the load, with the reason and the component that rejected each.</summary>
[TypeOption(typeof(LoadPhases), "Reject")]
public sealed class RejectLoadPhase : LoadPhaseBase
{
    /// <summary>The column that receives the reason a component rejected the record.</summary>
    public const string ReasonColumn = "RejectReason";

    /// <summary>The column that receives the name of the component that rejected the record.</summary>
    public const string ComponentColumn = "RejectComponent";

    /// <summary>Initializes a new instance of the <see cref="RejectLoadPhase"/> class.</summary>
    public RejectLoadPhase() : base("Reject", "The row writes the records the pipeline's components rejected, after the load, with the reason and the component that rejected each.", readsRejectedRecords: true)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> ReservedColumns { get; } = [ReasonColumn, ComponentColumn];
}
