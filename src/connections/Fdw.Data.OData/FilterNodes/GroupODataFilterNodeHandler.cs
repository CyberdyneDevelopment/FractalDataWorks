using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.OData.FilterNodes;

/// <summary>Writes a group of conditions, joined by its logical operator and wrapped in parentheses for precedence.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ODataFilterNodeHandlers), "Group")]
public sealed class GroupODataFilterNodeHandler : ODataFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupODataFilterNodeHandler"/> class.</summary>
    public GroupODataFilterNodeHandler()
        : base("Group")
    {
    }

    /// <inheritdoc/>
    public override string Build(IFilterNode node) =>
        node is FilterGroup group
            ? ODataCommandTranslatorBase.BuildODataGroup(group)
            : throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}");
}
