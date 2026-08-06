using System.Linq.Expressions;

namespace Fdw.Commands.Data;

/// <summary>
/// Extension methods for expression-based property extraction.
/// </summary>
internal static class ExpressionHelper
{
    /// <summary>
    /// Extracts the property name from a lambda expression.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="expression">The property selector expression.</param>
    /// <returns>The property name.</returns>
    /// <exception cref="ArgumentException">Thrown when expression is not a property access.</exception>
    internal static string ExtractPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        // Handle unary expressions (e.g., boxing conversions)
        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a simple property access (e.g., x => x.PropertyName)",
            nameof(expression));
    }
}