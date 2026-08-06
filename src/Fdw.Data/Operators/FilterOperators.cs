using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// TypeCollection for filter operators.
/// Source generator will create static properties for each operator with [TypeOption] attribute.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides compile-time discovery of all filter operator types.
/// No switch statements needed - operators know their own SQL/OData representations!
/// </para>
/// <para>
/// Example generated properties:
/// <list type="bullet">
/// <item>FilterOperators.Equal - Equal operator (=, eq)</item>
/// <item>FilterOperators.NotEqual - Not equal operator (&lt;&gt;, ne)</item>
/// <item>FilterOperators.Contains - Contains operator (LIKE, contains)</item>
/// <item>FilterOperators.GreaterThan - Greater than operator (&gt;, gt)</item>
/// </list>
/// </para>
/// <para>
/// Usage eliminates switch statements:
/// <code>
/// var condition = new FilterCondition {
///     PropertyName = "Name",
///     Operator = FilterOperators.Contains,  // Type-safe!
///     Value = "Acme"
/// };
///
/// // No switch - just property access!
/// var sqlCondition = $"[{condition.PropertyName}] {condition.Operator.SqlOperator} {condition.Operator.FormatSqlParameter(condition.PropertyName)}";
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(FilterOperatorBase), typeof(IFilterOperator), typeof(FilterOperators))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterOperators : TypeCollectionBase<FilterOperatorBase, IFilterOperator>
{
    // Source generator will create:
    // - Static constructor
    // - Static properties for each [TypeOption] operator
    // - All() method
    // - ByName() method
    // - ById() method
    // - NotFound() method (returns a special "not found" instance)
}
