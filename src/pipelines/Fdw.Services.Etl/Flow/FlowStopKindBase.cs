using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>Base of the <see cref="FlowStopKinds"/> options.</summary>
public abstract class FlowStopKindBase : TypeOptionBase<FlowStopKindBase>, IFlowStopKind
{
    /// <summary>Initializes a new instance of the <see cref="FlowStopKindBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The option's name.</param>
    protected FlowStopKindBase(int id, string name)
        : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract Task EndRecord(IFlowReporter reporter, Guid record, IGenericResult outcome, bool unfinished, CancellationToken cancellationToken = default);
}
