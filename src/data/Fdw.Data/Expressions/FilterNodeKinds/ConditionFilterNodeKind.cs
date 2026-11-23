using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>A condition: one test of one field, the leaf of a filter tree.</summary>
[TypeOption(typeof(FilterNodeKinds), "Condition")]
[ExcludeFromCodeCoverage]
public sealed class ConditionFilterNodeKind : FilterNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionFilterNodeKind"/> class.</summary>
    public ConditionFilterNodeKind()
        : base("Condition")
    {
    }

    /// <inheritdoc />
    public override bool Holds(IFilterNode node) => node is IFilterCondition;

    /// <inheritdoc />
    public override IEnumerable<IFilterCondition> Conditions(IFilterNode node) => [(IFilterCondition)node];

    /// <inheritdoc />
    public override IFilterNode Rewrite(IFilterNode node, Func<IFilterCondition, IFilterCondition> rewriteCondition) =>
        (IFilterNode)rewriteCondition((IFilterCondition)node);

    /// <inheritdoc />
    public override bool Matches(IFilterNode node, Func<IFilterCondition, bool> satisfies) => satisfies((IFilterCondition)node);
}
