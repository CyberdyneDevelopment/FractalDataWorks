using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql.FilterNodes;

/// <summary>A group of conditions, joined by its logical operator and wrapped in parentheses for precedence.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SqlFilterNodeHandlers), "Group")]
public sealed class GroupSqlFilterNodeHandler : SqlFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupSqlFilterNodeHandler"/> class.</summary>
    public GroupSqlFilterNodeHandler()
        : base("Group")
    {
    }

    /// <inheritdoc/>
    public override string Build(IFilterNode node, SqlBuildContext context) =>
        node is FilterGroup group
            ? SqlWhereBuilder.BuildGroup(group, context)
            : throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}");
}
