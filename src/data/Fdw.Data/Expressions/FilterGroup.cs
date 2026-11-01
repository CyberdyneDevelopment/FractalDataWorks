using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Composite filter node that groups child nodes with a logical operator.
/// Enables nested filtering with proper precedence.
/// </summary>
/// <remarks>
/// <para>
/// FilterGroup allows unlimited nesting for complex filter expressions:
/// <list type="bullet">
/// <item>(A OR B) AND C</item>
/// <item>((A AND B) OR (C AND D)) AND E</item>
/// <item>A OR (B AND (C OR D))</item>
/// </list>
/// </para>
/// <para>
/// Translators wrap groups in parentheses to preserve precedence in SQL and OData.
/// </para>
/// </remarks>
public sealed record FilterGroup : IFilterNode
{
    /// <summary>
    /// Logical operator combining the child nodes (AND/OR).
    /// </summary>
    public required LogicalOperator Operator { get; init; }

    /// <summary>
    /// Child filter nodes - can be FilterCondition (leaf) or FilterGroup (nested composite).
    /// </summary>
    public required IReadOnlyList<IFilterNode> Nodes { get; init; }

    /// <inheritdoc/>
    public bool Equals(FilterGroup? other) =>
        other is not null && Operator == other.Operator && Nodes.SequenceEqual(other.Nodes);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Operator);
        foreach (var node in Nodes)
            hash.Add(node);
        return hash.ToHashCode();
    }
}
