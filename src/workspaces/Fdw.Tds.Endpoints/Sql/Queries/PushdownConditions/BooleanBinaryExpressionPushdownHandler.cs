using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates AND and OR.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "BooleanBinaryExpression")]
[ExcludeFromCodeCoverage]
public sealed class BooleanBinaryExpressionPushdownHandler : PushdownConditionHandlerBase<BooleanBinaryExpression>
{
    /// <summary>Initializes a new instance of the <see cref="BooleanBinaryExpressionPushdownHandler"/> class.</summary>
    public BooleanBinaryExpressionPushdownHandler()
        : base("BooleanBinaryExpression")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(BooleanBinaryExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.Logical(condition, table, qualifier, scope);
}
