using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates BETWEEN and NOT BETWEEN.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "BooleanTernaryExpression")]
[ExcludeFromCodeCoverage]
public sealed class BooleanTernaryExpressionPushdownHandler : PushdownConditionHandlerBase<BooleanTernaryExpression>
{
    /// <summary>Initializes a new instance of the <see cref="BooleanTernaryExpressionPushdownHandler"/> class.</summary>
    public BooleanTernaryExpressionPushdownHandler()
        : base("BooleanTernaryExpression")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(BooleanTernaryExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.Between(condition, table, qualifier, scope);
}
