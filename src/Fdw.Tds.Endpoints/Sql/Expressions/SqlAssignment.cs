using System;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// Assigns a value to a declared variable, converted to the type it was declared with.
/// </summary>
public static class SqlAssignment
{
    /// <summary>Evaluates an expression and assigns it.</summary>
    /// <param name="variable">The variable as parsed.</param>
    /// <param name="expression">The expression whose value is assigned.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>Success, error 137 for a variable not declared, or 245 for a value that does not convert.</returns>
    public static IGenericResult Assign(VariableReference variable, ScalarExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(scope);
        var variables = scope.Context.Variables;
        if (!variables.TryGetType(variable.Name, out var type))
            return SqlRefusal.Refuse(SqlServerErrors.MustDeclareVariable(variable.Name, variable.StartLine), scope.Context.Logger);

        var value = SqlExpressions.Evaluate(expression, scope);
        if (value.IsFailure)
            return value;

        if (!type.TryConvert(value.Value, out var converted))
            return SqlRefusal.Refuse(SqlServerErrors.ConversionFailed(type.ToString(), expression.StartLine), scope.Context.Logger);

        variables.TrySet(variable.Name, converted);
        return GenericResult.Success();
    }
}
