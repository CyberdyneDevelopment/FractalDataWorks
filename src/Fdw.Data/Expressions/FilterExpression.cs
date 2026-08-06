using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IFilterExpression for hierarchical WHERE clause representation.
/// </summary>
/// <remarks>
/// <para>
/// Use FilterCondition for simple single-condition filters.
/// Use FilterGroup for complex filters with grouping and precedence.
/// </para>
/// <para>
/// Examples:
/// <code>
/// // Simple: Name = 'Acme'
/// new FilterExpression
/// {
///     Root = new FilterCondition
///     {
///         PropertyName = "Name",
///         Operator = new EqualOperator(),
///         Value = "Acme"
///     }
/// };
///
/// // Complex: (Name = 'Acme' OR Name = 'Corp') AND IsActive = true
/// new FilterExpression
/// {
///     Root = new FilterGroup
///     {
///         Operator = LogicalOperator.And,
///         Nodes =
///         [
///             new FilterGroup
///             {
///                 Operator = LogicalOperator.Or,
///                 Nodes =
///                 [
///                     new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Acme" },
///                     new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Corp" }
///                 ]
///             },
///             new FilterCondition { PropertyName = "IsActive", Operator = new EqualOperator(), Value = true }
///         ]
///     }
/// };
/// </code>
/// </para>
/// </remarks>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class FilterExpression : IFilterExpression
{
    /// <summary>
    /// Gets or sets the root filter node.
    /// </summary>
    public IFilterNode? Root { get; init; }
}
