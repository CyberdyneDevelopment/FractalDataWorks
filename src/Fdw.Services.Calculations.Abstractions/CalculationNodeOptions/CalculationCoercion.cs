using System;
using System.Linq.Expressions;
using System.Reflection;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The conversions between the three types a calculation expression has: decimal, a possibly empty decimal, and a
/// condition.
/// </summary>
/// <remarks>
/// Why only these three: every value in a tree is an exact number, a number that may be empty, or a true/false condition. A
/// number converts to a possibly empty number for free; a possibly empty number converts to a number only by failing with
/// EmptyOperand when it is empty, so an empty value is never silently read as zero. A condition never converts to a number.
/// </remarks>
public static class CalculationCoercion
{
    private static readonly MethodInfo UnwrapMethod = typeof(CalculationRuntime).GetMethod(nameof(CalculationRuntime.Unwrap))
        ?? throw new InvalidOperationException("CalculationRuntime.Unwrap was not found");

    /// <summary>Gets a value indicating whether the type is a number, empty allowed or not.</summary>
    /// <param name="type">The type.</param>
    /// <returns>True for decimal and decimal?.</returns>
    public static bool IsNumber(Type type) => type == typeof(decimal) || type == typeof(decimal?);

    /// <summary>Gets a value indicating whether a value of one type can be used where another is required.</summary>
    /// <param name="actual">The type the value has.</param>
    /// <param name="required">The type that is required.</param>
    /// <returns>True when they are the same, or both are numbers.</returns>
    public static bool CanCoerce(Type actual, Type required) =>
        actual == required || (IsNumber(actual) && IsNumber(required));

    /// <summary>Converts an expression to the required type.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="required">The required type, decimal or decimal?.</param>
    /// <param name="state">The run state parameter.</param>
    /// <param name="node">The index of the node that needs the value, for a failure.</param>
    /// <returns>The converted expression.</returns>
    public static Expression Coerce(Expression expression, Type required, ParameterExpression state, int node)
    {
        if (expression.Type == required)
        {
            return expression;
        }

        return required == typeof(decimal?)
            ? Expression.Convert(expression, typeof(decimal?))
            : Expression.Call(UnwrapMethod, expression, state, Expression.Constant(node));
    }
}
