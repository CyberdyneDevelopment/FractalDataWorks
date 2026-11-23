using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates IS [NOT] NULL.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "BooleanIsNullExpression")]
[ExcludeFromCodeCoverage]
public sealed class BooleanIsNullExpressionPushdownHandler : PushdownConditionHandlerBase<BooleanIsNullExpression>
{
    /// <summary>Initializes a new instance of the <see cref="BooleanIsNullExpressionPushdownHandler"/> class.</summary>
    public BooleanIsNullExpressionPushdownHandler()
        : base("BooleanIsNullExpression")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(BooleanIsNullExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.IsNull(condition, table, qualifier, scope);
}
