using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates a parenthesized condition.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "BooleanParenthesisExpression")]
[ExcludeFromCodeCoverage]
public sealed class BooleanParenthesisExpressionPushdownHandler : PushdownConditionHandlerBase<BooleanParenthesisExpression>
{
    /// <summary>Initializes a new instance of the <see cref="BooleanParenthesisExpressionPushdownHandler"/> class.</summary>
    public BooleanParenthesisExpressionPushdownHandler()
        : base("BooleanParenthesisExpression")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(BooleanParenthesisExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.Translate(condition.Expression, table, qualifier, scope);
}
