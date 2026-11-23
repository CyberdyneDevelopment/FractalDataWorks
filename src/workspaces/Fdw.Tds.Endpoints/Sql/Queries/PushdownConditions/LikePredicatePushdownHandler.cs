using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates LIKE with % only at the start, the end or both.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "LikePredicate")]
[ExcludeFromCodeCoverage]
public sealed class LikePredicatePushdownHandler : PushdownConditionHandlerBase<LikePredicate>
{
    /// <summary>Initializes a new instance of the <see cref="LikePredicatePushdownHandler"/> class.</summary>
    public LikePredicatePushdownHandler()
        : base("LikePredicate")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(LikePredicate condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.Like(condition, table, qualifier, scope);
}
