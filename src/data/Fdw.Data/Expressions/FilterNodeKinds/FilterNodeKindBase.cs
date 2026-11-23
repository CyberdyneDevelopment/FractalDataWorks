using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>The base of every filter node kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class FilterNodeKindBase : TypeOptionBase<FilterNodeKindBase>, IFilterNodeKind
{
    /// <summary>Initializes a new instance of the <see cref="FilterNodeKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected FilterNodeKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(IFilterNode node);

    /// <inheritdoc />
    public abstract IEnumerable<IFilterCondition> Conditions(IFilterNode node);

    /// <inheritdoc />
    public abstract IFilterNode Rewrite(IFilterNode node, Func<IFilterCondition, IFilterCondition> rewriteCondition);

    /// <inheritdoc />
    public abstract bool Matches(IFilterNode node, Func<IFilterCondition, bool> satisfies);
}
