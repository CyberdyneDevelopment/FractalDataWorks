using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql.FilterNodes;

/// <summary>A single condition: the column, the operator and its parameters.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SqlFilterNodeHandlers), "Condition")]
public sealed class ConditionSqlFilterNodeHandler : SqlFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionSqlFilterNodeHandler"/> class.</summary>
    public ConditionSqlFilterNodeHandler()
        : base("Condition")
    {
    }

    /// <inheritdoc/>
    public override string Build(IFilterNode node, SqlBuildContext context) =>
        node is FilterCondition condition
            ? SqlWhereBuilder.BuildCondition(condition, context)
            : throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}");
}
