using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>Nothing stopped the flow: a record ends Completed or Failed by how the flow went.</summary>
[TypeOption(typeof(FlowStopKinds), "None")]
public sealed class NoStop : FlowStopKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NoStop"/> class.</summary>
    public NoStop()
        : base(1, "None")
    {
    }

    /// <inheritdoc />
    public override Task EndRecord(IFlowReporter reporter, Guid record, IGenericResult outcome, bool unfinished, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentNullException.ThrowIfNull(outcome);
        return reporter.StepFinished(record, unfinished ? outcome : GenericResult.Success(), cancellationToken);
    }
}
