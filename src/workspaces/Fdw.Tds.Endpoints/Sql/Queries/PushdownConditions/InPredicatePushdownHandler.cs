using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates IN with a list of values.</summary>
[TypeOption(typeof(PushdownConditionHandlers), "InPredicate")]
[ExcludeFromCodeCoverage]
public sealed class InPredicatePushdownHandler : PushdownConditionHandlerBase<InPredicate>
{
    /// <summary>Initializes a new instance of the <see cref="InPredicatePushdownHandler"/> class.</summary>
    public InPredicatePushdownHandler()
        : base("InPredicate")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<QueryFilterNode> Handle(InPredicate condition, SqlTable table, string qualifier, SqlScope scope)
        => PushdownFilter.In(condition, table, qualifier, scope);
}
