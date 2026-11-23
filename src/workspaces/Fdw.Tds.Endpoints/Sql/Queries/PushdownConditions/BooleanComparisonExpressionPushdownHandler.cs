using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates a column compared with a value.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "BooleanComparisonExpression")]
[ExcludeFromCodeCoverage]
public sealed class BooleanComparisonExpressionPushdownHandler : PushdownConditionHandlerBase<BooleanComparisonExpression>
{
    /// <summary>Initializes a new instance of the <see cref="BooleanComparisonExpressionPushdownHandler"/> class.</summary>
    public BooleanComparisonExpressionPushdownHandler()
        : base("BooleanComparisonExpression")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(BooleanComparisonExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.Comparison(condition, table, qualifier, scope);
}
