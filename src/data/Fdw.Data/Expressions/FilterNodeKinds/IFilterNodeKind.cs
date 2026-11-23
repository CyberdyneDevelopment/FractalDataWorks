using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>One kind of node a filter tree is made of, and what it means to walk, rewrite and evaluate a node of that kind.</summary>
public interface IFilterNodeKind : ITypeOption<int, FilterNodeKindBase>
{
    /// <summary>Tells whether a node is of this kind.</summary>
    /// <param name="node">The node.</param>
    /// <returns>True when this kind holds the node.</returns>
    bool Holds(IFilterNode node);

    /// <summary>Lists the conditions at the leaves of a node, in order.</summary>
    /// <param name="node">A node this kind <see cref="Holds"/>.</param>
    /// <returns>The conditions.</returns>
    IEnumerable<IFilterCondition> Conditions(IFilterNode node);

    /// <summary>Rebuilds a node with each of its conditions replaced.</summary>
    /// <param name="node">A node this kind <see cref="Holds"/>.</param>
    /// <param name="rewriteCondition">Gives the condition that takes the place of one condition.</param>
    /// <returns>The rebuilt node; a group keeps its operator.</returns>
    IFilterNode Rewrite(IFilterNode node, Func<IFilterCondition, IFilterCondition> rewriteCondition);

    /// <summary>Tells whether a node is satisfied.</summary>
    /// <param name="node">A node this kind <see cref="Holds"/>.</param>
    /// <param name="satisfies">Tells whether one condition is satisfied.</param>
    /// <returns>True when the node is satisfied: a group by its operator, a condition by <paramref name="satisfies"/>.</returns>
    bool Matches(IFilterNode node, Func<IFilterCondition, bool> satisfies);
}
