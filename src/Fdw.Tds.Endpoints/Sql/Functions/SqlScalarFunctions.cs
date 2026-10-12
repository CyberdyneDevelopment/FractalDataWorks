using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The T-SQL built-in scalar functions this endpoint evaluates, found by their T-SQL name.
/// </summary>
/// <remarks>
/// The set is open: a package adds a function by declaring an option. A name no option answers to is
/// error 195, and an aggregate or windowed call is refused here because it is not a scalar function —
/// the query that holds it decides whether it can be aggregated.
/// </remarks>
[TypeCollection(typeof(SqlScalarFunctionBase), typeof(ISqlScalarFunction), typeof(SqlScalarFunctions))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlScalarFunctions : TypeCollectionBase<SqlScalarFunctionBase, ISqlScalarFunction>
{
    private static readonly Lazy<Dictionary<string, ISqlScalarFunction>> BySqlName =
        new(static () => All().ToDictionary(f => f.SqlName, f => (ISqlScalarFunction)f, StringComparer.OrdinalIgnoreCase));

    /// <summary>Evaluates a function call.</summary>
    /// <param name="call">The call as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value (null for NULL), or the refusal.</returns>
    public static IGenericResult<object?> Call(FunctionCall call, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(scope);
        var name = call.FunctionName.Value;

        // An aggregate a grouped query already computed answers its own value here.
        if (scope.Aggregates.TryGetValue(call, out var aggregated))
            return GenericResult<object?>.Success(aggregated);

        if (call.CallTarget is not null || call.OverClause is not null || SqlAggregates.IsAggregate(name))
            return SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported(SqlText.Of(call), call.StartLine), scope.Context.Logger);

        return BySqlName.Value.TryGetValue(name, out var function)
            ? function.Evaluate(new SqlFunctionCall(name, call.Parameters, scope, call.StartLine))
            : SqlRefusal.Refuse<object?>(SqlServerErrors.UnknownFunction(name, call.StartLine), scope.Context.Logger);
    }
}
