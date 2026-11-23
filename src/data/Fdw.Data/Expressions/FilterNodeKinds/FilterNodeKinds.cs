using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>The kinds of node a filter tree is made of: a condition or a group.</summary>
/// <remarks>A node of a type no kind holds is refused with an <see cref="InvalidOperationException"/>, never skipped or passed through.</remarks>
[TypeCollection(typeof(FilterNodeKindBase), typeof(IFilterNodeKind), typeof(FilterNodeKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterNodeKinds : TypeCollectionBase<FilterNodeKindBase, IFilterNodeKind>
{
    /// <summary>Lists the conditions at the leaves of a node, in order.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The conditions.</returns>
    public static IEnumerable<IFilterCondition> Conditions(IFilterNode node) => Held(node).Conditions(node);

    /// <summary>Rebuilds a node with each of its conditions replaced.</summary>
    /// <param name="node">The node.</param>
    /// <param name="rewriteCondition">Gives the condition that takes the place of one condition.</param>
    /// <returns>The rebuilt node.</returns>
    public static IFilterNode Rewrite(IFilterNode node, Func<IFilterCondition, IFilterCondition> rewriteCondition) =>
        Held(node).Rewrite(node, rewriteCondition);

    /// <summary>Tells whether a node is satisfied.</summary>
    /// <param name="node">The node.</param>
    /// <param name="satisfies">Tells whether one condition is satisfied.</param>
    /// <returns>True when the node is satisfied.</returns>
    public static bool Matches(IFilterNode node, Func<IFilterCondition, bool> satisfies) =>
        Held(node).Matches(node, satisfies);

    /// <summary>Finds the kind of a filter node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The first kind that holds the node: Condition or Group; <c>NotFound</c> for a node of any other type.</returns>
    public static IFilterNodeKind Of(IFilterNode node)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(node))
            {
                return kind;
            }
        }

        return NotFound;
    }

    private static IFilterNodeKind Held(IFilterNode node)
    {
        var kind = Of(node);
        return ReferenceEquals(kind, NotFound)
            ? throw new InvalidOperationException($"A filter node of type '{node.GetType().Name}' is neither a condition nor a group; the query translator builds only those.")
            : kind;
    }
}
