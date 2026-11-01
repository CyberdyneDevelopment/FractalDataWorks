using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Globals;

/// <summary>
/// The T-SQL global variables this endpoint answers, found by their @@ name.
/// </summary>
/// <remarks>
/// A name no option answers to is error 137, which is what SQL Server says of a global variable it does
/// not define.
/// </remarks>
[TypeCollection(typeof(SqlGlobalVariableBase), typeof(ISqlGlobalVariable), typeof(SqlGlobalVariables))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlGlobalVariables : TypeCollectionBase<SqlGlobalVariableBase, ISqlGlobalVariable>
{
    private static readonly Lazy<Dictionary<string, ISqlGlobalVariable>> BySqlName =
        new(static () => All().ToDictionary(v => v.SqlName, v => (ISqlGlobalVariable)v, StringComparer.OrdinalIgnoreCase));

    /// <summary>Reads a global variable.</summary>
    /// <param name="expression">The variable as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value (null for NULL), or error 137.</returns>
    public static IGenericResult<object?> Evaluate(GlobalVariableExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(scope);
        return BySqlName.Value.TryGetValue(expression.Name, out var variable)
            ? GenericResult<object?>.Success(variable.Read(scope.Context))
            : SqlRefusal.Refuse<object?>(SqlServerErrors.MustDeclareVariable(expression.Name, expression.StartLine), scope.Context.Logger);
    }
}
