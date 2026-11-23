using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>A group: nodes combined by AND or OR, which may nest.</summary>
[TypeOption(typeof(FilterNodeKinds), "Group")]
[ExcludeFromCodeCoverage]
public sealed class GroupFilterNodeKind : FilterNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupFilterNodeKind"/> class.</summary>
    public GroupFilterNodeKind()
        : base("Group")
    {
    }

    /// <inheritdoc />
    public override bool Holds(IFilterNode node) => node is FilterGroup;

    /// <inheritdoc />
    public override IEnumerable<IFilterCondition> Conditions(IFilterNode node) =>
        ((FilterGroup)node).Nodes.SelectMany(FilterNodeKinds.Conditions);

    /// <inheritdoc />
    public override IFilterNode Rewrite(IFilterNode node, Func<IFilterCondition, IFilterCondition> rewriteCondition)
    {
        var group = (FilterGroup)node;
        return new FilterGroup
        {
            Operator = group.Operator,
            Nodes = [.. group.Nodes.Select(child => FilterNodeKinds.Rewrite(child, rewriteCondition))],
        };
    }

    /// <inheritdoc />
    public override bool Matches(IFilterNode node, Func<IFilterCondition, bool> satisfies)
    {
        var group = (FilterGroup)node;
        return ReferenceEquals(group.Operator, LogicalOperator.Or)
            ? group.Nodes.Any(child => FilterNodeKinds.Matches(child, satisfies))
            : group.Nodes.All(child => FilterNodeKinds.Matches(child, satisfies));
    }
}
