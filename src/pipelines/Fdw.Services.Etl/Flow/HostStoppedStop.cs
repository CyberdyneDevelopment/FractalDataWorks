using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>The host is stopping: the flow writes what it did and leaves the record open for the host to close Interrupted.</summary>
[TypeOption(typeof(FlowStopKinds), "HostStopped")]
public sealed class HostStoppedStop : FlowStopKindBase
{
    /// <summary>Initializes a new instance of the <see cref="HostStoppedStop"/> class.</summary>
    public HostStoppedStop()
        : base(3, "HostStopped")
    {
    }

    /// <inheritdoc />
    public override Task EndRecord(IFlowReporter reporter, Guid record, IGenericResult outcome, bool unfinished, CancellationToken cancellationToken = default)
    {
        return unfinished
            ? Task.CompletedTask
            : reporter.StepFinished(record, GenericResult.Success(), cancellationToken);
    }
}
