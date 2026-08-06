namespace Fdw.Data.Abstractions;

/// <summary>
/// Marker interface for filter tree nodes in composite pattern.
/// Implemented by FilterCondition (leaf) and FilterGroup (composite).
/// </summary>
/// <remarks>
/// Enables building filter trees with proper precedence and grouping:
/// <code>
/// // (Name = 'Acme' OR Name = 'Corp') AND IsActive = true
/// new FilterGroup
/// {
///     Operator = LogicalOperator.And,
///     Nodes =
///     [
///         new FilterGroup
///         {
///             Operator = LogicalOperator.Or,
///             Nodes =
///             [
///                 new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Acme" },
///                 new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Corp" }
///             ]
///         },
///         new FilterCondition { PropertyName = "IsActive", Operator = new EqualOperator(), Value = true }
///     ]
/// }
/// </code>
/// </remarks>
public interface IFilterNode
{
}
