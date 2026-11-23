using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols.FilterNodes;

/// <summary>A group the protocol cannot express: the refusal names the group's shape, since its own logical operator may well be expressible.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RestFilterNodeHandlers), "Group")]
public sealed class GroupRestFilterNodeHandler : RestFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupRestFilterNodeHandler"/> class.</summary>
    public GroupRestFilterNodeHandler()
        : base("Group")
    {
    }

    /// <inheritdoc/>
    public override ResultDetails Describe(IFilterNode node, string protocolName) =>
        node is FilterGroup group
            ? ResultDetails.Create(
                "Protocol", protocolName,
                // The group's own logical operator may well be expressible (OData supports "and" fine) -- what's actually unsupported is the
                // SHAPE (a nested group, or more children than this protocol can join into one expression), so the count goes in the detail
                // rather than implying the operator itself is the problem.
                "Operator", $"{group.Operator.Name} group of {group.Nodes.Count}",
                "Field", "(group)")
            : throw new InvalidOperationException($"Filter node '{node.GetType().Name}' is not a group.");
}
