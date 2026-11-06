using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>A person cancelled the run, or its time limit passed: a record whose components did not finish ends Cancelled.</summary>
[TypeOption(typeof(FlowStopKinds), "Cancelled")]
public sealed class CancelledStop : FlowStopKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CancelledStop"/> class.</summary>
    public CancelledStop()
        : base(2, "Cancelled")
    {
    }

    /// <inheritdoc />
    public override Task EndRecord(IFlowReporter reporter, Guid record, IGenericResult outcome, bool unfinished, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        return unfinished
            ? reporter.StepCancelled(record, cancellationToken)
            : reporter.StepFinished(record, GenericResult.Success(), cancellationToken);
    }
}
