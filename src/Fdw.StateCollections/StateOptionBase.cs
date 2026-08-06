using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.StateCollections;

/// <summary>
/// Base class for smart-state TypeOptions. Subclasses populate <see cref="CanProgressTo"/>
/// and override <see cref="OnEnter"/> / <see cref="OnExit"/>. To avoid construction-order
/// problems with cross-state references, derived types should resolve their outbound
/// transitions lazily — typically via the static collection's <c>ByName</c> lookup inside the
/// property getter rather than capturing references in the constructor.
/// </summary>
/// <typeparam name="TSelf">The concrete state interface (CRTP).</typeparam>
public abstract class StateOptionBase<TSelf> : TypeOptionBase<int, TSelf>, IStateOption<TSelf>
    where TSelf : IStateOption<TSelf>
{
    /// <summary>Initializes a smart-state option with id + name.</summary>
    protected StateOptionBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>Initializes a smart-state option with id + name + category.</summary>
    protected StateOptionBase(int id, string name, string? category) : base(id, name, category)
    {
    }

    /// <inheritdoc />
    public abstract IReadOnlyList<TSelf> CanProgressTo { get; }

    /// <inheritdoc />
    public virtual bool IsTerminal => CanProgressTo.Count == 0;

    /// <inheritdoc />
    public virtual Task<IGenericResult> OnEnter(IStateContext<TSelf> context, CancellationToken cancellationToken)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public virtual Task<IGenericResult> OnExit(IStateContext<TSelf> context, CancellationToken cancellationToken)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return Task.FromResult(GenericResult.Success());
    }
}
